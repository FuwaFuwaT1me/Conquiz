using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Conquiz.Quiz;
using Conquiz.Bots;

namespace Conquiz.UI
{
    /// <summary>
    /// Debug panel for testing quiz sessions.
    /// Provides buttons to start Human vs Bot or Bot vs Bot sessions.
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

        [Header("UI Elements")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private Button humanVsBotButton;
        [SerializeField] private Button botVsBotButton;
        [SerializeField] private Button togglePanelButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        private int mcqIndex;
        private int numericIndex;
        private bool panelVisible = true;

        void Start()
        {
            SetupComponents();
            SetupButtons();
            UpdateStatus("Ready. Press a button to start.");

            Debug.Log("=== SESSION DEBUG PANEL ===");
            Debug.Log($"Press {toggleKey} to toggle debug panel");
            Debug.Log("===========================");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                TogglePanel();
            }
        }

        private void SetupComponents()
        {
            // Auto-create session controller if needed
            if (sessionController == null)
            {
                sessionController = GetComponent<QuizSessionController>();
                if (sessionController == null)
                {
                    sessionController = gameObject.AddComponent<QuizSessionController>();
                }
            }

            // Auto-find QuizUIController
            if (quizUI == null)
            {
                quizUI = FindObjectOfType<QuizUIController>();
            }

            // Auto-create human provider if needed
            if (humanProvider == null)
            {
                humanProvider = GetComponent<HumanAnswerProvider>();
                if (humanProvider == null)
                {
                    humanProvider = gameObject.AddComponent<HumanAnswerProvider>();
                }
            }
            humanProvider.SetPlayerId(0);
            humanProvider.SetQuizUI(quizUI);

            // Auto-create bot provider if needed
            if (botProvider == null)
            {
                var botGo = new GameObject("DebugBot");
                botGo.transform.SetParent(transform);
                botProvider = botGo.AddComponent<BotAnswerProvider>();
            }
            botProvider.SetPlayerId(1);
            botProvider.SetDifficulty(0.6f, 40f, 0.2f);

            // Subscribe to session events
            sessionController.OnSessionComplete += HandleSessionComplete;
            // TODO: Re-enable after modernization
            // sessionController.OnMcqQuestionStarted += HandleQuestionStarted;
            // sessionController.OnNumericQuestionStarted += HandleNumericStarted;
        }

        private void SetupButtons()
        {
            if (humanVsBotButton != null)
            {
                humanVsBotButton.onClick.AddListener(StartHumanVsBot);
            }

            if (botVsBotButton != null)
            {
                botVsBotButton.onClick.AddListener(StartBotVsBot);
            }

            if (togglePanelButton != null)
            {
                togglePanelButton.onClick.AddListener(TogglePanel);
            }
        }

        void OnDestroy()
        {
            if (sessionController != null)
            {
                sessionController.OnSessionComplete -= HandleSessionComplete;
                // sessionController.OnMcqQuestionStarted -= HandleQuestionStarted;
                // sessionController.OnNumericQuestionStarted -= HandleNumericStarted;
            }
        }

        private void TogglePanel()
        {
            panelVisible = !panelVisible;
            if (debugPanel != null)
            {
                debugPanel.SetActive(panelVisible);
            }
        }

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

            // Hide debug panel during quiz
            if (debugPanel != null)
            {
                debugPanel.SetActive(false);
            }

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

            // Create second bot
            var bot2Go = new GameObject("DebugBot2");
            bot2Go.transform.SetParent(transform);
            var bot2 = bot2Go.AddComponent<BotAnswerProvider>();
            bot2.SetPlayerId(2);
            bot2.SetDifficulty(0.5f, 60f, 0.1f);

            UpdateStatus("Bot vs Bot in progress...");
            SetResultText("");

            sessionController.StartSession(botProvider, bot2, mcq, numeric, (result) =>
            {
                // Cleanup second bot
                Destroy(bot2Go);
            });
        }

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

        private void HandleQuestionStarted(int playerId, McqQuestionData question)
        {
            string playerName = playerId == humanProvider.PlayerId ? "YOU" : "BOT";
            UpdateStatus($"[MCQ] {playerName}: {question.QuestionText}");
        }

        private void HandleNumericStarted(int playerId, NumericQuestionData question)
        {
            string playerName = playerId == humanProvider.PlayerId ? "YOU" : "BOT";
            UpdateStatus($"[NUMERIC] {playerName}: {question.QuestionText}");
        }

        private void HandleSessionComplete(SessionResult result)
        {
            // Show debug panel again
            if (debugPanel != null)
            {
                debugPanel.SetActive(true);
                panelVisible = true;
            }

            // Hide quiz UI after a delay
            if (quizUI != null)
            {
                StartCoroutine(HideQuizAfterDelay(2f));
            }

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

        private System.Collections.IEnumerator HideQuizAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (quizUI != null)
            {
                quizUI.HideQuiz();
            }
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
            // Find or create Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("DebugCanvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // Create panel
            debugPanel = new GameObject("DebugPanel");
            debugPanel.transform.SetParent(canvas.transform, false);

            var panelRect = debugPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(300, 200);

            var panelImage = debugPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);

            var layout = debugPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Title
            CreateText(debugPanel.transform, "SESSION DEBUG (F1 to toggle)", 16, FontStyles.Bold);

            // Buttons
            humanVsBotButton = CreateButton(debugPanel.transform, "Human vs Bot");
            botVsBotButton = CreateButton(debugPanel.transform, "Bot vs Bot");

            // Status
            statusText = CreateText(debugPanel.transform, "Ready", 12, FontStyles.Normal);

            // Result (scrollable would be better, but keeping simple)
            resultText = CreateText(debugPanel.transform, "", 10, FontStyles.Normal);
            resultText.alignment = TextAlignmentOptions.TopLeft;

            SetupButtons();
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

        private Button CreateButton(Transform parent, string label)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.4f, 0.8f);

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.9f);
            colors.pressedColor = new Color(0.1f, 0.3f, 0.7f);
            button.colors = colors;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 35;

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
    }
}
