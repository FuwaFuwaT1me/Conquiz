using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Conquiz.Quiz;
using Conquiz.Bots;

namespace Conquiz.UI
{
    /// <summary>
    /// Debug panel for testing quiz sessions.
    /// Provides bot mode toggle and controls for overriding bot answers.
    /// IMPORTANT: This panel remains visible and clickable during quiz sessions.
    /// Uses a dedicated Canvas with high sorting order to stay on top.
    /// </summary>
    public class SessionDebugPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QuizSessionController sessionController;
        [SerializeField] private QuizUIController quizUI;
        [SerializeField] private HumanAnswerProvider humanProvider;
        [SerializeField] private BotAnswerProvider botProvider;

        [Header("Test Questions")]
        [SerializeField] private McqQuestionData[] mcqQuestions;
        [SerializeField] private NumericQuestionData[] numericQuestions;

        [Header("UI Elements - Main Panel")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private Button humanVsBotButton;
        [SerializeField] private Button botVsBotButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("UI Elements - Bot Mode (Buttons)")]
        [SerializeField] private Button automaticModeButton;
        [SerializeField] private Button manualModeButton;
        [SerializeField] private TextMeshProUGUI modeStatusText;

        [Header("UI Elements - MCQ Override (Manual Mode)")]
        [SerializeField] private GameObject mcqOverrideGroup;
        [SerializeField] private Button botCorrectButton;
        [SerializeField] private Button botWrongButton;

        [Header("UI Elements - Numeric Override (Manual Mode)")]
        [SerializeField] private GameObject numericOverrideGroup;
        [SerializeField] private TMP_InputField numericOverrideInput;
        [SerializeField] private Button setNumericButton;
        [SerializeField] private TextMeshProUGUI numericOverrideStatusText;

        [Header("Canvas Settings")]
        [Tooltip("Sorting order for the debug panel's Canvas (must be higher than quiz UI)")]
        [SerializeField] private int debugCanvasSortingOrder = 100;

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private Color activeColor = new Color(0.3f, 0.85f, 0.4f);
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color autoModeColor = new Color(0.2f, 0.6f, 0.3f);
        [SerializeField] private Color manualModeColor = new Color(0.8f, 0.6f, 0.2f);

        private int mcqIndex;
        private int numericIndex;
        private bool panelVisible = true;
        private Canvas debugCanvas;
        private BotAnswerProvider bot2Provider;
        
        // Static toggle handler - works even when panel is disabled
        private static SessionDebugPanel activeInstance;

        void Start()
        {
            activeInstance = this;
            EnsureDebugCanvasOnTop();
            SetupComponents();
            SetupButtons();
            UpdateModeUI();
            UpdateStatus("Ready. Press a button to start.");

            Debug.Log("=== SESSION DEBUG PANEL ===");
            Debug.Log($"Press {toggleKey} to toggle debug panel");
            Debug.Log("Panel stays visible during quiz!");
            Debug.Log("===========================");
        }

        /// <summary>
        /// Ensures the debug panel has its own Canvas with high sorting order.
        /// This guarantees it stays visible and clickable during quiz sessions.
        /// </summary>
        private void EnsureDebugCanvasOnTop()
        {
            if (debugPanel == null) return;

            // Check if debugPanel already has a Canvas
            debugCanvas = debugPanel.GetComponent<Canvas>();
            if (debugCanvas == null)
            {
                debugCanvas = debugPanel.AddComponent<Canvas>();
            }

            // Set to overlay with high sorting order
            debugCanvas.overrideSorting = true;
            debugCanvas.sortingOrder = debugCanvasSortingOrder;

            // Ensure it has a GraphicRaycaster for input
            var raycaster = debugPanel.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                debugPanel.AddComponent<GraphicRaycaster>();
            }

            Debug.Log($"[SessionDebugPanel] Canvas sorting order set to {debugCanvasSortingOrder}");
        }

        void Update()
        {
            // F1 toggle is now handled by GlobalInputHandler
        }
        
        /// <summary>
        /// Called by GlobalInputHandler to toggle panel visibility.
        /// This method works even when the panel GameObject is disabled.
        /// </summary>
        public void TogglePanelFromGlobal()
        {
            TogglePanel();
        }
        
        /// <summary>
        /// Static method to check for F1 key globally.
        /// Call this from a persistent MonoBehaviour.
        /// </summary>
        public static void CheckGlobalToggle()
        {
            if (activeInstance != null && Input.GetKeyDown(activeInstance.toggleKey))
            {
                activeInstance.TogglePanel();
            }
        }

        private void SetupComponents()
        {
            // Auto-find QuizUIController FIRST (needed for other components)
            if (quizUI == null)
            {
                quizUI = FindObjectOfType<QuizUIController>();
                if (quizUI == null)
                {
                    Debug.LogWarning("[SessionDebugPanel] QuizUIController not found in scene!");
                }
            }

            // Auto-find or create session controller
            // Look for existing one in scene first to avoid duplicates
            if (sessionController == null)
            {
                sessionController = FindObjectOfType<QuizSessionController>();
                if (sessionController == null)
                {
                    sessionController = gameObject.AddComponent<QuizSessionController>();
                    Debug.Log("[SessionDebugPanel] Created QuizSessionController");
                }
            }

            // Auto-find or create human provider
            // IMPORTANT: Look for existing one in scene first to avoid duplicates!
            if (humanProvider == null)
            {
                humanProvider = FindObjectOfType<HumanAnswerProvider>();
                if (humanProvider == null)
                {
                    humanProvider = gameObject.AddComponent<HumanAnswerProvider>();
                    Debug.Log("[SessionDebugPanel] Created HumanAnswerProvider");
                }
            }
            humanProvider.SetPlayerId(0);
            if (quizUI != null)
            {
                humanProvider.SetQuizUI(quizUI);
            }

            // Auto-find or create bot provider
            if (botProvider == null)
            {
                botProvider = FindObjectOfType<BotAnswerProvider>();
                if (botProvider == null)
                {
                    var botGo = new GameObject("DebugBot");
                    botGo.transform.SetParent(transform);
                    botProvider = botGo.AddComponent<BotAnswerProvider>();
                    Debug.Log("[SessionDebugPanel] Created BotAnswerProvider");
                }
            }
            botProvider.SetPlayerId(1);

            // Subscribe to session events
            if (sessionController != null)
            {
                sessionController.OnSessionComplete += HandleSessionComplete;
            }

            // Subscribe to DebugBotSettings events
            DebugBotSettings.Instance.OnModeChanged += OnBotModeChanged;
            
            Debug.Log($"[SessionDebugPanel] Setup complete - UI:{quizUI != null}, Session:{sessionController != null}, Human:{humanProvider != null}, Bot:{botProvider != null}");
        }

        private void SetupButtons()
        {
            // Session start buttons
            if (humanVsBotButton != null)
            {
                humanVsBotButton.onClick.AddListener(StartHumanVsBot);
            }

            if (botVsBotButton != null)
            {
                botVsBotButton.onClick.AddListener(StartBotVsBot);
            }

            // Mode buttons (NOT toggles)
            if (automaticModeButton != null)
            {
                automaticModeButton.onClick.AddListener(SetAutomaticMode);
            }

            if (manualModeButton != null)
            {
                manualModeButton.onClick.AddListener(SetManualMode);
            }

            // MCQ override buttons - trigger immediate bot answer
            if (botCorrectButton != null)
            {
                botCorrectButton.onClick.AddListener(TriggerBotCorrect);
            }

            if (botWrongButton != null)
            {
                botWrongButton.onClick.AddListener(TriggerBotWrong);
            }

            // Numeric override
            if (setNumericButton != null)
            {
                setNumericButton.onClick.AddListener(TriggerBotNumeric);
            }

            if (numericOverrideInput != null)
            {
                numericOverrideInput.onSubmit.AddListener(_ => TriggerBotNumeric());
            }
        }

        void OnDestroy()
        {
            if (sessionController != null)
            {
                sessionController.OnSessionComplete -= HandleSessionComplete;
            }

            if (DebugBotSettings.Instance != null)
            {
                DebugBotSettings.Instance.OnModeChanged -= OnBotModeChanged;
            }

            CleanupSecondBot();
        }

        private void TogglePanel()
        {
            panelVisible = !panelVisible;
            
            // If debugPanel is THIS gameObject, use CanvasGroup to hide/show
            // instead of SetActive (which would disable the Update() method)
            if (debugPanel != null)
            {
                if (debugPanel == gameObject)
                {
                    // Use CanvasGroup for visibility toggle
                    var canvasGroup = debugPanel.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = debugPanel.AddComponent<CanvasGroup>();
                    }
                    
                    canvasGroup.alpha = panelVisible ? 1f : 0f;
                    canvasGroup.interactable = panelVisible;
                    canvasGroup.blocksRaycasts = panelVisible;
                }
                else
                {
                    debugPanel.SetActive(panelVisible);
                }
            }
            
            Debug.Log($"[SessionDebugPanel] Panel {(panelVisible ? "SHOWN" : "HIDDEN")}");
        }

        // =====================================================================
        // SESSION START
        // =====================================================================

        private void StartHumanVsBot()
        {
            if (sessionController.IsSessionInProgress)
            {
                UpdateStatus("Session in progress...");
                return;
            }

            var mcq = GetNextMcq();
            var numeric = GetNextNumeric();

            if (mcq == null)
            {
                UpdateStatus("ERROR: No MCQ questions!");
                return;
            }

            UpdateStatus($"YOUR TURN: {mcq.QuestionText}");
            SetResultText("");

            // DO NOT hide debug panel - keep it visible!
            sessionController.StartSession(humanProvider, botProvider, mcq, numeric);
        }

        private void StartBotVsBot()
        {
            if (sessionController.IsSessionInProgress)
            {
                UpdateStatus("Session in progress...");
                return;
            }

            var mcq = GetNextMcq();
            var numeric = GetNextNumeric();

            if (mcq == null)
            {
                UpdateStatus("ERROR: No MCQ questions!");
                return;
            }

            // Cleanup any existing second bot
            CleanupSecondBot();

            // Create second bot
            var bot2Go = new GameObject("DebugBot2");
            bot2Go.transform.SetParent(transform);
            bot2Provider = bot2Go.AddComponent<BotAnswerProvider>();
            bot2Provider.SetPlayerId(2);

            UpdateStatus("Bot vs Bot in progress...");
            SetResultText("");

            sessionController.StartSession(botProvider, bot2Provider, mcq, numeric, (result) =>
            {
                // Cleanup second bot after session
                CleanupSecondBot();
            });
        }

        private void CleanupSecondBot()
        {
            if (bot2Provider != null)
            {
                Destroy(bot2Provider.gameObject);
                bot2Provider = null;
            }
        }

        // =====================================================================
        // BOT MODE CONTROL
        // =====================================================================

        private void SetAutomaticMode()
        {
            DebugBotSettings.Instance.CurrentMode = BotAnswerMode.Automatic;
            UpdateStatus("Bot Mode: AUTOMATIC (always correct)");
        }

        private void SetManualMode()
        {
            DebugBotSettings.Instance.CurrentMode = BotAnswerMode.Manual;
            UpdateStatus("Bot Mode: MANUAL (click Correct/Wrong)");
        }

        private void OnBotModeChanged(BotAnswerMode mode)
        {
            UpdateModeUI();
        }

        private void UpdateModeUI()
        {
            var settings = DebugBotSettings.Instance;
            bool isManual = settings.CurrentMode == BotAnswerMode.Manual;

            // Update mode status text
            if (modeStatusText != null)
            {
                if (isManual)
                {
                    modeStatusText.text = "BOT: Manual Control";
                    modeStatusText.color = manualModeColor;
                }
                else
                {
                    modeStatusText.text = "BOT: Auto (Correct)";
                    modeStatusText.color = autoModeColor;
                }
            }

            // Update button colors
            if (automaticModeButton != null)
            {
                var colors = automaticModeButton.colors;
                colors.normalColor = isManual ? inactiveColor : autoModeColor;
                automaticModeButton.colors = colors;
                
                var img = automaticModeButton.GetComponent<Image>();
                if (img != null) img.color = isManual ? inactiveColor : autoModeColor;
            }

            if (manualModeButton != null)
            {
                var colors = manualModeButton.colors;
                colors.normalColor = isManual ? manualModeColor : inactiveColor;
                manualModeButton.colors = colors;
                
                var img = manualModeButton.GetComponent<Image>();
                if (img != null) img.color = isManual ? manualModeColor : inactiveColor;
            }

            // SHOW/HIDE manual override controls based on mode
            if (mcqOverrideGroup != null)
            {
                mcqOverrideGroup.SetActive(isManual);
            }
            else
            {
                // If no group, control individual buttons
                if (botCorrectButton != null)
                    botCorrectButton.gameObject.SetActive(isManual);
                if (botWrongButton != null)
                    botWrongButton.gameObject.SetActive(isManual);
            }

            if (numericOverrideGroup != null)
            {
                numericOverrideGroup.SetActive(isManual);
            }
            else
            {
                // If no group, control individual elements
                if (numericOverrideInput != null)
                    numericOverrideInput.gameObject.SetActive(isManual);
                if (setNumericButton != null)
                    setNumericButton.gameObject.SetActive(isManual);
            }
        }

        // =====================================================================
        // CORRECT/WRONG - TRIGGERS IMMEDIATE BOT ANSWER (MCQ OR NUMERIC)
        // =====================================================================

        private void TriggerBotCorrect()
        {
            var settings = DebugBotSettings.Instance;
            
            if (!settings.IsWaitingForManualInput)
            {
                UpdateStatus("Bot not waiting for input");
                return;
            }

            string qType = settings.WaitingQuestionType == CurrentQuestionType.MCQ ? "MCQ" : "Numeric";
            settings.TriggerCorrectAnswer();
            UpdateStatus($"Bot answered {qType}: CORRECT");
        }

        private void TriggerBotWrong()
        {
            var settings = DebugBotSettings.Instance;
            
            if (!settings.IsWaitingForManualInput)
            {
                UpdateStatus("Bot not waiting for input");
                return;
            }

            string qType = settings.WaitingQuestionType == CurrentQuestionType.MCQ ? "MCQ" : "Numeric";
            settings.TriggerWrongAnswer();
            UpdateStatus($"Bot answered {qType}: WRONG");
        }

        // =====================================================================
        // NUMERIC OVERRIDE - TRIGGERS IMMEDIATE ANSWER
        // =====================================================================

        private void TriggerBotNumeric()
        {
            var settings = DebugBotSettings.Instance;
            
            if (!settings.IsWaitingForManualInput)
            {
                UpdateStatus("Bot not waiting for input");
                return;
            }

            if (numericOverrideInput == null) return;

            string inputText = numericOverrideInput.text;
            if (string.IsNullOrWhiteSpace(inputText))
            {
                UpdateStatus("Enter a numeric value first!");
                return;
            }

            if (!float.TryParse(inputText, out float value))
            {
                UpdateStatus("Invalid number!");
                return;
            }

            settings.SetNumericOverride(value);
            settings.TriggerManualNumericAnswer();
            
            UpdateStatus($"Bot answered: {value}");

            if (numericOverrideStatusText != null)
            {
                numericOverrideStatusText.text = $"Sent: {value}";
                numericOverrideStatusText.color = activeColor;
            }
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private McqQuestionData GetNextMcq()
        {
            if (mcqQuestions == null || mcqQuestions.Length == 0)
                return null;

            var q = mcqQuestions[mcqIndex % mcqQuestions.Length];
            mcqIndex++;
            return q;
        }

        private NumericQuestionData GetNextNumeric()
        {
            if (numericQuestions == null || numericQuestions.Length == 0)
                return null;

            var q = numericQuestions[numericIndex % numericQuestions.Length];
            numericIndex++;
            return q;
        }

        private void HandleSessionComplete(SessionResult result)
        {
            // Update status
            string winnerName;
            if (!result.HasWinner)
            {
                winnerName = "NO WINNER";
            }
            else if (result.WinnerPlayerId == humanProvider.PlayerId)
            {
                winnerName = "YOU WIN!";
            }
            else
            {
                winnerName = "BOT WINS!";
            }

            UpdateStatus($"SESSION COMPLETE: {winnerName}");
            SetResultText(result.GetDebugBreakdown());

            Debug.Log(result.GetDebugBreakdown());
        }

        private void UpdateStatus(string text)
        {
            if (statusText != null)
            {
                statusText.text = text;
            }
            Debug.Log($"[SessionDebug] {text}");
        }

        private void SetResultText(string text)
        {
            if (resultText != null)
            {
                resultText.text = text;
            }
        }

        /// <summary>
        /// Creates the debug UI programmatically if no Canvas exists.
        /// Call this from another script or use the prefab approach.
        /// </summary>
        [ContextMenu("Create Debug UI")]
        public void CreateDebugUI()
        {
            // Create dedicated debug Canvas (always create new one with high sorting)
            var canvasGo = new GameObject("DebugCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = debugCanvasSortingOrder; // High sorting order to stay on top
            
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGo.AddComponent<GraphicRaycaster>();
            debugCanvas = canvas;

            // Create panel
            debugPanel = new GameObject("DebugPanel");
            debugPanel.transform.SetParent(canvas.transform, false);

            var panelRect = debugPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(320, 400);

            var panelImage = debugPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            var layout = debugPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Title
            CreateText(debugPanel.transform, "SESSION DEBUG (F1)", 16, FontStyles.Bold);

            // Session buttons
            humanVsBotButton = CreateButton(debugPanel.transform, "Human vs Bot", new Color(0.2f, 0.5f, 0.9f));
            botVsBotButton = CreateButton(debugPanel.transform, "Bot vs Bot", new Color(0.4f, 0.4f, 0.8f));

            // Divider
            CreateText(debugPanel.transform, "─────────────────", 12, FontStyles.Normal);

            // Mode section
            CreateText(debugPanel.transform, "BOT MODE:", 14, FontStyles.Bold);

            // Mode buttons (horizontal group)
            var modeGroupGo = new GameObject("ModeGroup");
            modeGroupGo.transform.SetParent(debugPanel.transform, false);
            var modeLayout = modeGroupGo.AddComponent<HorizontalLayoutGroup>();
            modeLayout.spacing = 10;
            var modeLE = modeGroupGo.AddComponent<LayoutElement>();
            modeLE.minHeight = 35;

            automaticModeButton = CreateButton(modeGroupGo.transform, "Auto", autoModeColor);
            manualModeButton = CreateButton(modeGroupGo.transform, "Manual", inactiveColor);

            modeStatusText = CreateText(debugPanel.transform, "BOT: Auto (Correct)", 12, FontStyles.Italic);
            modeStatusText.color = autoModeColor;

            // MCQ Override section (in a group for easy hiding)
            mcqOverrideGroup = new GameObject("McqOverrideGroup");
            mcqOverrideGroup.transform.SetParent(debugPanel.transform, false);
            var mcqLayout = mcqOverrideGroup.AddComponent<VerticalLayoutGroup>();
            mcqLayout.spacing = 5;
            mcqOverrideGroup.SetActive(false); // Hidden by default (Auto mode)

            CreateText(mcqOverrideGroup.transform, "MCQ (click to make bot answer):", 11, FontStyles.Bold);

            var mcqBtnGroup = new GameObject("McqButtons");
            mcqBtnGroup.transform.SetParent(mcqOverrideGroup.transform, false);
            var mcqBtnLayout = mcqBtnGroup.AddComponent<HorizontalLayoutGroup>();
            mcqBtnLayout.spacing = 10;
            var mcqBtnLE = mcqBtnGroup.AddComponent<LayoutElement>();
            mcqBtnLE.minHeight = 35;

            botCorrectButton = CreateButton(mcqBtnGroup.transform, "Correct", new Color(0.2f, 0.7f, 0.3f));
            botWrongButton = CreateButton(mcqBtnGroup.transform, "Wrong", new Color(0.8f, 0.3f, 0.3f));

            // Numeric Override section (in a group for easy hiding)
            numericOverrideGroup = new GameObject("NumericOverrideGroup");
            numericOverrideGroup.transform.SetParent(debugPanel.transform, false);
            var numLayout = numericOverrideGroup.AddComponent<VerticalLayoutGroup>();
            numLayout.spacing = 5;
            numericOverrideGroup.SetActive(false); // Hidden by default (Auto mode)

            CreateText(numericOverrideGroup.transform, "Numeric (click to make bot answer):", 11, FontStyles.Bold);

            var numInputGroup = new GameObject("NumericInputGroup");
            numInputGroup.transform.SetParent(numericOverrideGroup.transform, false);
            var numInputLayout = numInputGroup.AddComponent<HorizontalLayoutGroup>();
            numInputLayout.spacing = 10;
            var numInputLE = numInputGroup.AddComponent<LayoutElement>();
            numInputLE.minHeight = 35;

            numericOverrideInput = CreateInputField(numInputGroup.transform, "Value...");
            setNumericButton = CreateButton(numInputGroup.transform, "Send", new Color(0.5f, 0.5f, 0.8f));

            numericOverrideStatusText = CreateText(numericOverrideGroup.transform, "", 10, FontStyles.Normal);

            // Divider
            CreateText(debugPanel.transform, "─────────────────", 12, FontStyles.Normal);

            // Status
            statusText = CreateText(debugPanel.transform, "Ready", 12, FontStyles.Normal);

            // Result (scrollable would be better, but keeping simple)
            resultText = CreateText(debugPanel.transform, "", 10, FontStyles.Normal);
            resultText.alignment = TextAlignmentOptions.TopLeft;

            SetupButtons();
            UpdateModeUI();
            Debug.Log("Debug UI created!");
        }

        private TextMeshProUGUI CreateText(Transform parent, string content, int fontSize, FontStyles style)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = fontSize + 8;

            return text;
        }

        private Button CreateButton(Transform parent, string label, Color bgColor)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = bgColor;

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.2f;
            colors.pressedColor = bgColor * 0.8f;
            button.colors = colors;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 35;
            le.flexibleWidth = 1;

            // Button text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private TMP_InputField CreateInputField(Transform parent, string placeholder)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f);

            var inputField = go.AddComponent<TMP_InputField>();
            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 35;
            le.flexibleWidth = 2;

            // Text area
            var textAreaGo = new GameObject("Text Area");
            textAreaGo.transform.SetParent(go.transform, false);
            var textAreaRect = textAreaGo.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 5);
            textAreaRect.offsetMax = new Vector2(-10, -5);

            // Placeholder
            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(textAreaGo.transform, false);
            var placeholderText = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 12;
            placeholderText.color = new Color(0.6f, 0.6f, 0.6f);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            var phRect = placeholderGo.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(textAreaGo.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            inputField.textViewport = textAreaRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;

            return inputField;
        }
    }
}
