#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Conquiz.Quiz;
using Conquiz.Bots;

namespace Conquiz.UI
{
    /// <summary>
    /// Bootstraps the entire Quiz scene UI programmatically.
    /// Creates all necessary Canvas, panels, buttons, and wires them together.
    /// </summary>
    public sealed class QuizSceneSetup : MonoBehaviour
    {
        [Header("Test Questions")]
        [SerializeField] private McqQuestionData[]? mcqQuestions;
        [SerializeField] private NumericQuestionData[]? numericQuestions;

        [Header("Created References (Auto-populated)")]
        [SerializeField] private QuizUIController? quizUI;
        [SerializeField] private SessionDebugPanel? debugPanel;
        [SerializeField] private QuizSessionController? sessionController;
        [SerializeField] private HumanAnswerProvider? humanProvider;
        [SerializeField] private BotAnswerProvider? botProvider;

        private const int QuizCanvasSortOrder = 10;
        private const int DebugCanvasSortOrder = 100;

        private void Start()
        {
            SetupScene();
        }
        
        private void Update()
        {
            // Handle F1 toggle globally (works even when debug panel is hidden)
            SessionDebugPanel.CheckGlobalToggle();
        }

        [ContextMenu("Setup Scene")]
        public void SetupScene()
        {
            Debug.Log("[QuizSceneSetup] Building Quiz Scene...");

            LoadQuestionsIfNeeded();
            CreateQuizCanvas();
            CreateDebugCanvas();
            CreateManagers();
            WireReferences();

            Debug.Log("[QuizSceneSetup] Scene setup complete!");
            Debug.Log("[QuizSceneSetup] Press F1 to toggle debug panel");
        }

        private void LoadQuestionsIfNeeded()
        {
            if (mcqQuestions == null || mcqQuestions.Length == 0)
            {
                mcqQuestions = Resources.LoadAll<McqQuestionData>("");
                if (mcqQuestions.Length == 0)
                {
                    mcqQuestions = CreateDefaultMcqQuestions();
                }
                Debug.Log($"[QuizSceneSetup] Loaded {mcqQuestions.Length} MCQ questions");
            }

            if (numericQuestions == null || numericQuestions.Length == 0)
            {
                numericQuestions = Resources.LoadAll<NumericQuestionData>("");
                if (numericQuestions.Length == 0)
                {
                    numericQuestions = CreateDefaultNumericQuestions();
                }
                Debug.Log($"[QuizSceneSetup] Loaded {numericQuestions.Length} Numeric questions");
            }
        }

        private McqQuestionData[] CreateDefaultMcqQuestions()
        {
            var q1 = ScriptableObject.CreateInstance<McqQuestionData>();
            SetQuestionFields(q1, "What is the capital of France?", "Geography",
                new[] { "Paris", "London", "Berlin", "Madrid" }, 0);

            var q2 = ScriptableObject.CreateInstance<McqQuestionData>();
            SetQuestionFields(q2, "Which planet is known as the Red Planet?", "Science",
                new[] { "Venus", "Mars", "Jupiter", "Saturn" }, 1);

            var q3 = ScriptableObject.CreateInstance<McqQuestionData>();
            SetQuestionFields(q3, "Who painted the Mona Lisa?", "Art",
                new[] { "Van Gogh", "Picasso", "Da Vinci", "Michelangelo" }, 2);

            return new[] { q1, q2, q3 };
        }

        private NumericQuestionData[] CreateDefaultNumericQuestions()
        {
            var q1 = ScriptableObject.CreateInstance<NumericQuestionData>();
            SetNumericQuestionFields(q1, "How many countries are in the European Union (2024)?", "Geography",
                27f, "", 1f, 100f, 0);

            var q2 = ScriptableObject.CreateInstance<NumericQuestionData>();
            SetNumericQuestionFields(q2, "What is the boiling point of water in Celsius?", "Science",
                100f, "°C", 0f, 500f, 0);

            var q3 = ScriptableObject.CreateInstance<NumericQuestionData>();
            SetNumericQuestionFields(q3, "In what year did World War II end?", "History",
                1945f, "", 1900f, 2000f, 0);

            return new[] { q1, q2, q3 };
        }

        private void SetQuestionFields(McqQuestionData q, string question, string category, string[] choices, int correctIndex)
        {
            var type = typeof(McqQuestionData);
            var baseType = typeof(QuestionData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            baseType.GetField("questionText", flags)?.SetValue(q, question);
            baseType.GetField("category", flags)?.SetValue(q, category);
            type.GetField("choices", flags)?.SetValue(q, choices);
            type.GetField("correctIndex", flags)?.SetValue(q, correctIndex);
        }

        private void SetNumericQuestionFields(NumericQuestionData q, string question, string category,
            float correctValue, string unit, float min, float max, int decimals)
        {
            var type = typeof(NumericQuestionData);
            var baseType = typeof(QuestionData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            baseType.GetField("questionText", flags)?.SetValue(q, question);
            baseType.GetField("category", flags)?.SetValue(q, category);
            type.GetField("correctValue", flags)?.SetValue(q, correctValue);
            type.GetField("unit", flags)?.SetValue(q, unit);
            type.GetField("allowedRangeMin", flags)?.SetValue(q, min);
            type.GetField("allowedRangeMax", flags)?.SetValue(q, max);
            type.GetField("decimalPlaces", flags)?.SetValue(q, decimals);
        }

        private void CreateQuizCanvas()
        {
            var canvasGo = new GameObject("QuizCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = QuizCanvasSortOrder;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            quizUI = canvasGo.AddComponent<QuizUIController>();

            CreateQuizPanelUI(canvasGo.transform);
        }

        private void CreateQuizPanelUI(Transform canvasTransform)
        {
            // ===== CENTERED QUIZ WINDOW =====
            // Create a centered panel (not fullscreen) - approximately 60% width, 70% height
            var quizPanelGo = CreatePanel(canvasTransform, "QuizPanel", new Color(0.10f, 0.12f, 0.18f, 0.96f));
            var quizPanelRect = quizPanelGo.GetComponent<RectTransform>();
            
            // Center the panel perfectly
            quizPanelRect.anchorMin = new Vector2(0.20f, 0.15f);   // 20% from left, 15% from bottom
            quizPanelRect.anchorMax = new Vector2(0.80f, 0.85f);   // 80% from left, 85% from bottom
            quizPanelRect.offsetMin = Vector2.zero;
            quizPanelRect.offsetMax = Vector2.zero;
            quizPanelRect.pivot = new Vector2(0.5f, 0.5f);
            
            // No outline - was causing visual artifacts when panel is hidden

            var mainLayout = quizPanelGo.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(25, 25, 15, 20);
            mainLayout.spacing = 12;
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;
            mainLayout.childControlHeight = true;

            // Header section with player badges and timer
            CreateHeaderSection(quizPanelGo.transform);

            // Question section (~40-45% of content)
            CreateQuestionSection(quizPanelGo.transform);

            // MCQ Panel (~55-60% of content)
            CreateMcqPanel(quizPanelGo.transform);

            // Numeric Panel
            CreateNumericPanel(quizPanelGo.transform);

            // Results Panel
            CreateResultsPanel(quizPanelGo.transform);

            // Assign to QuizUIController via reflection
            AssignQuizUIReferences(quizPanelGo);
        }

        private void CreateHeaderSection(Transform parent)
        {
            // Header: [PlayerLeft] --- [Timer] --- [PlayerRight]
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(parent, false);
            var headerRect = headerGo.AddComponent<RectTransform>();
            var headerLayout = headerGo.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 0;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            var headerLE = headerGo.AddComponent<LayoutElement>();
            headerLE.minHeight = 70;
            headerLE.preferredHeight = 70;

            // Left badge (YOU) - compact design
            CreateCompactPlayerBadge(headerGo.transform, "PlayerBadgeLeft", "YOU", 
                new Color(0.15f, 0.75f, 0.65f), true);

            // Flexible spacer
            CreateFlexibleSpacer(headerGo.transform, 0.5f);

            // Timer in center - smaller, cleaner design
            CreateCompactTimer(headerGo.transform);

            // Flexible spacer
            CreateFlexibleSpacer(headerGo.transform, 0.5f);

            // Right badge (OPPONENT) - compact design
            CreateCompactPlayerBadge(headerGo.transform, "PlayerBadgeRight", "OPPONENT", 
                new Color(0.95f, 0.5f, 0.2f), false);
        }

        private void CreateCompactPlayerBadge(Transform parent, string name, string label, Color accentColor, bool isLeft)
        {
            var badgeGo = new GameObject(name);
            badgeGo.transform.SetParent(parent, false);
            var badgeRect = badgeGo.AddComponent<RectTransform>();
            
            var badgeLE = badgeGo.AddComponent<LayoutElement>();
            badgeLE.minWidth = 100;
            badgeLE.preferredWidth = 120;
            badgeLE.minHeight = 60;

            var badgeImage = badgeGo.AddComponent<Image>();
            badgeImage.color = new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f, 0.6f);

            var badgeLayout = badgeGo.AddComponent<VerticalLayoutGroup>();
            badgeLayout.padding = new RectOffset(8, 8, 4, 4);
            badgeLayout.spacing = 2;
            badgeLayout.childAlignment = TextAnchor.MiddleCenter;
            badgeLayout.childForceExpandWidth = true;
            badgeLayout.childForceExpandHeight = false;

            // Player name label
            var labelText = CreateText(badgeGo.transform, "Label", label, 12, FontStyles.Bold, accentColor);
            labelText.alignment = TextAlignmentOptions.Center;
            var labelLE = labelText.gameObject.AddComponent<LayoutElement>();
            labelLE.minHeight = 18;

            // Status text (Thinking... / Answered!)
            var statusText = CreateText(badgeGo.transform, "Status", "Thinking...", 10, FontStyles.Italic, new Color(0.7f, 0.7f, 0.8f));
            statusText.alignment = TextAlignmentOptions.Center;
            var statusLE = statusText.gameObject.AddComponent<LayoutElement>();
            statusLE.minHeight = 14;

            // Add PlayerStatusBadge component
            var badge = badgeGo.AddComponent<PlayerStatusBadge>();
        }

        private void CreateFlexibleSpacer(Transform parent, float flex)
        {
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>();
            var spacerLE = spacer.AddComponent<LayoutElement>();
            spacerLE.flexibleWidth = flex;
        }

        private void CreateCompactTimer(Transform parent)
        {
            var timerGo = new GameObject("Timer");
            timerGo.transform.SetParent(parent, false);
            var timerRect = timerGo.AddComponent<RectTransform>();
            
            var timerLE = timerGo.AddComponent<LayoutElement>();
            timerLE.minWidth = 60;
            timerLE.preferredWidth = 60;
            timerLE.minHeight = 60;
            timerLE.preferredHeight = 60;

            // Timer background (circle/ring)
            var bgRing = new GameObject("BgRing");
            bgRing.transform.SetParent(timerGo.transform, false);
            var bgRingRect = bgRing.AddComponent<RectTransform>();
            bgRingRect.anchorMin = Vector2.zero;
            bgRingRect.anchorMax = Vector2.one;
            bgRingRect.offsetMin = Vector2.zero;
            bgRingRect.offsetMax = Vector2.zero;
            var bgRingImage = bgRing.AddComponent<Image>();
            bgRingImage.color = new Color(0.15f, 0.18f, 0.25f, 0.8f);

            // Timer fill ring (countdown visualization)
            var fillRing = new GameObject("FillRing");
            fillRing.transform.SetParent(timerGo.transform, false);
            var fillRingRect = fillRing.AddComponent<RectTransform>();
            fillRingRect.anchorMin = Vector2.zero;
            fillRingRect.anchorMax = Vector2.one;
            fillRingRect.offsetMin = new Vector2(3, 3);
            fillRingRect.offsetMax = new Vector2(-3, -3);
            var fillRingImage = fillRing.AddComponent<Image>();
            fillRingImage.color = new Color(0.3f, 0.7f, 1f);
            fillRingImage.type = Image.Type.Filled;
            fillRingImage.fillMethod = Image.FillMethod.Radial360;
            fillRingImage.fillOrigin = (int)Image.Origin360.Top;
            fillRingImage.fillClockwise = false;

            // Timer number text
            var timerText = CreateText(timerGo.transform, "TimerText", "15", 22, FontStyles.Bold, Color.white);
            var timerTextRect = timerText.GetComponent<RectTransform>();
            timerTextRect.anchorMin = Vector2.zero;
            timerTextRect.anchorMax = Vector2.one;
            timerTextRect.offsetMin = Vector2.zero;
            timerTextRect.offsetMax = Vector2.zero;
            timerText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateQuestionSection(Transform parent)
        {
            // Question section takes ~45-50% of content area
            var questionSection = new GameObject("QuestionSection");
            questionSection.transform.SetParent(parent, false);
            var sectionLayout = questionSection.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 6;
            sectionLayout.childAlignment = TextAnchor.MiddleCenter;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;
            sectionLayout.padding = new RectOffset(20, 20, 10, 15);
            
            var sectionLE = questionSection.AddComponent<LayoutElement>();
            sectionLE.flexibleHeight = 0.45f;  // 45% of flexible space
            sectionLE.minHeight = 120;

            // Round indicator (small, subtle)
            var roundText = CreateText(questionSection.transform, "RoundIndicator", "ROUND 1: MULTIPLE CHOICE", 12, FontStyles.Bold, new Color(0.5f, 0.7f, 1f));
            roundText.alignment = TextAlignmentOptions.Center;
            var roundLE = roundText.gameObject.AddComponent<LayoutElement>();
            roundLE.minHeight = 22;

            // Category text (small)
            var categoryText = CreateText(questionSection.transform, "CategoryText", "GEOGRAPHY", 11, FontStyles.Normal, new Color(0.6f, 0.65f, 0.75f));
            categoryText.alignment = TextAlignmentOptions.Center;
            var catLE = categoryText.gameObject.AddComponent<LayoutElement>();
            catLE.minHeight = 18;

            // Separator line
            CreateSeparatorLine(questionSection.transform, new Color(0.35f, 0.4f, 0.5f, 0.4f));

            // Question text (main focus, larger)
            var questionText = CreateText(questionSection.transform, "QuestionText", "What is the capital of France?", 22, FontStyles.Normal, Color.white);
            questionText.alignment = TextAlignmentOptions.Center;
            questionText.enableWordWrapping = true;
            var qLE = questionText.gameObject.AddComponent<LayoutElement>();
            qLE.minHeight = 60;
            qLE.flexibleHeight = 1;
        }
        
        private void CreateSeparatorLine(Transform parent, Color color)
        {
            var lineGo = new GameObject("Separator");
            lineGo.transform.SetParent(parent, false);
            var lineRect = lineGo.AddComponent<RectTransform>();
            var lineImage = lineGo.AddComponent<Image>();
            lineImage.color = color;
            var lineLE = lineGo.AddComponent<LayoutElement>();
            lineLE.minHeight = 1;
            lineLE.preferredHeight = 1;
            lineLE.flexibleWidth = 1;
        }

        private void CreateMcqPanel(Transform parent)
        {
            // MCQ section takes ~50-55% of content area
            var mcqPanelGo = new GameObject("MCQPanel");
            mcqPanelGo.transform.SetParent(parent, false);
            
            // Using VerticalLayoutGroup with nested rows for better control
            var mcqLayout = mcqPanelGo.AddComponent<VerticalLayoutGroup>();
            mcqLayout.spacing = 8;
            mcqLayout.childAlignment = TextAnchor.MiddleCenter;
            mcqLayout.childForceExpandWidth = true;
            mcqLayout.childForceExpandHeight = true;
            mcqLayout.padding = new RectOffset(20, 20, 5, 10);

            var mcqLE = mcqPanelGo.AddComponent<LayoutElement>();
            mcqLE.flexibleHeight = 0.55f;  // 55% of flexible space
            mcqLE.minHeight = 140;

            // Create 2 rows of 2 buttons each
            var row1 = CreateMcqRow(mcqPanelGo.transform, "Row1");
            CreateMcqButton(row1.transform, "McqButton0", "A) Paris");
            CreateMcqButton(row1.transform, "McqButton1", "B) London");

            var row2 = CreateMcqRow(mcqPanelGo.transform, "Row2");
            CreateMcqButton(row2.transform, "McqButton2", "C) Berlin");
            CreateMcqButton(row2.transform, "McqButton3", "D) Madrid");
        }

        private GameObject CreateMcqRow(Transform parent, string name)
        {
            var rowGo = new GameObject(name);
            rowGo.transform.SetParent(parent, false);
            
            var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            var rowLE = rowGo.AddComponent<LayoutElement>();
            rowLE.flexibleHeight = 1;
            rowLE.minHeight = 48;

            return rowGo;
        }

        private Button CreateMcqButton(Transform parent, string name, string text)
        {
            var buttonGo = new GameObject(name);
            buttonGo.transform.SetParent(parent, false);

            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.15f, 0.18f, 0.26f);

            var button = buttonGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.15f, 0.18f, 0.26f);
            colors.highlightedColor = new Color(0.22f, 0.28f, 0.40f);
            colors.pressedColor = new Color(0.12f, 0.15f, 0.22f);
            colors.selectedColor = new Color(0.18f, 0.22f, 0.32f);
            button.colors = colors;

            var buttonLE = buttonGo.AddComponent<LayoutElement>();
            buttonLE.flexibleWidth = 1;
            buttonLE.minHeight = 45;

            var outline = buttonGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.4f, 0.55f, 0.25f);
            outline.effectDistance = new Vector2(1, -1);

            var buttonText = CreateText(buttonGo.transform, "Text", text, 15, FontStyles.Normal, Color.white);
            var textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 4);
            textRect.offsetMax = new Vector2(-10, -4);
            buttonText.alignment = TextAlignmentOptions.Center;

            return button;
        }

        private void CreateNumericPanel(Transform parent)
        {
            var numericPanelGo = new GameObject("NumericPanel");
            numericPanelGo.transform.SetParent(parent, false);
            numericPanelGo.SetActive(false);

            var numericLayout = numericPanelGo.AddComponent<VerticalLayoutGroup>();
            numericLayout.spacing = 15;
            numericLayout.childAlignment = TextAnchor.UpperCenter;
            numericLayout.childForceExpandWidth = true;
            numericLayout.childForceExpandHeight = false;
            numericLayout.padding = new RectOffset(100, 100, 0, 0);

            var numericLE = numericPanelGo.AddComponent<LayoutElement>();
            numericLE.minHeight = 150;
            numericLE.preferredHeight = 150;

            // Range hint
            var rangeHint = CreateText(numericPanelGo.transform, "RangeHint", "Range: 0 - 1,000,000", 14, FontStyles.Italic, new Color(0.7f, 0.7f, 0.8f));
            var rangeLE = rangeHint.gameObject.AddComponent<LayoutElement>();
            rangeLE.minHeight = 25;

            // Input row
            var inputRow = new GameObject("InputRow");
            inputRow.transform.SetParent(numericPanelGo.transform, false);
            var inputRowLayout = inputRow.AddComponent<HorizontalLayoutGroup>();
            inputRowLayout.spacing = 15;
            inputRowLayout.childAlignment = TextAnchor.MiddleCenter;
            inputRowLayout.childForceExpandWidth = false;
            inputRowLayout.childForceExpandHeight = false;
            var inputRowLE = inputRow.AddComponent<LayoutElement>();
            inputRowLE.minHeight = 50;
            inputRowLE.preferredHeight = 50;

            // Input field
            CreateNumericInputField(inputRow.transform);

            // Unit text
            var unitText = CreateText(inputRow.transform, "UnitText", "km", 18, FontStyles.Normal, Color.white);
            var unitLE = unitText.gameObject.AddComponent<LayoutElement>();
            unitLE.minWidth = 60;

            // Submit button
            CreateSubmitButton(inputRow.transform);
        }

        private void CreateNumericInputField(Transform parent)
        {
            var inputGo = new GameObject("NumericInput");
            inputGo.transform.SetParent(parent, false);

            var inputImage = inputGo.AddComponent<Image>();
            inputImage.color = new Color(0.2f, 0.2f, 0.25f);

            var inputLE = inputGo.AddComponent<LayoutElement>();
            inputLE.minWidth = 200;
            inputLE.preferredWidth = 250;
            inputLE.minHeight = 50;

            // Text viewport
            var textAreaGo = new GameObject("Text Area");
            textAreaGo.transform.SetParent(inputGo.transform, false);
            var textAreaRect = textAreaGo.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(15, 5);
            textAreaRect.offsetMax = new Vector2(-15, -5);

            // Placeholder
            var placeholderText = CreateText(textAreaGo.transform, "Placeholder", "Enter value...", 18, FontStyles.Italic, new Color(0.5f, 0.5f, 0.5f));
            var phRect = placeholderText.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;

            // Input text
            var inputText = CreateText(textAreaGo.transform, "Text", "", 18, FontStyles.Normal, Color.white);
            var inputTextRect = inputText.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;
            inputText.alignment = TextAlignmentOptions.MidlineLeft;

            var inputField = inputGo.AddComponent<TMP_InputField>();
            inputField.textViewport = textAreaRect;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            inputField.characterLimit = 15;
        }

        private void CreateSubmitButton(Transform parent)
        {
            var buttonGo = new GameObject("SubmitButton");
            buttonGo.transform.SetParent(parent, false);

            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.3f);

            var button = buttonGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 0.3f);
            colors.highlightedColor = new Color(0.3f, 0.7f, 0.4f);
            colors.pressedColor = new Color(0.15f, 0.5f, 0.25f);
            button.colors = colors;

            var buttonLE = buttonGo.AddComponent<LayoutElement>();
            buttonLE.minWidth = 120;
            buttonLE.minHeight = 50;

            var buttonText = CreateText(buttonGo.transform, "Text", "SUBMIT", 16, FontStyles.Bold, Color.white);
            var textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            buttonText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateResultsPanel(Transform parent)
        {
            var resultsPanelGo = new GameObject("ResultsPanel");
            resultsPanelGo.transform.SetParent(parent, false);
            resultsPanelGo.SetActive(false);

            var resultsLayout = resultsPanelGo.AddComponent<VerticalLayoutGroup>();
            resultsLayout.spacing = 15;
            resultsLayout.childAlignment = TextAnchor.UpperCenter;
            resultsLayout.childForceExpandWidth = true;
            resultsLayout.childForceExpandHeight = false;
            resultsLayout.padding = new RectOffset(50, 50, 20, 20);

            var resultsLE = resultsPanelGo.AddComponent<LayoutElement>();
            resultsLE.minHeight = 250;
            resultsLE.preferredHeight = 300;

            var resultsBg = resultsPanelGo.AddComponent<Image>();
            resultsBg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            // Player result
            var playerResult = CreateText(resultsPanelGo.transform, "PlayerResultText", "<b>YOU:</b> Paris", 20, FontStyles.Normal, new Color(0.3f, 0.85f, 0.4f));
            var playerLE = playerResult.gameObject.AddComponent<LayoutElement>();
            playerLE.minHeight = 35;

            // Opponent result
            var opponentResult = CreateText(resultsPanelGo.transform, "OpponentResultText", "<b>OPPONENT:</b> London", 20, FontStyles.Normal, new Color(1f, 0.4f, 0.4f));
            var oppLE = opponentResult.gameObject.AddComponent<LayoutElement>();
            oppLE.minHeight = 35;

            // Correct answer
            var correctAnswer = CreateText(resultsPanelGo.transform, "CorrectAnswerText", "Correct: Paris", 18, FontStyles.Bold, new Color(0.4f, 0.8f, 0.4f));
            var correctLE = correctAnswer.gameObject.AddComponent<LayoutElement>();
            correctLE.minHeight = 30;

            // Outcome
            var outcome = CreateText(resultsPanelGo.transform, "RoundOutcomeText", "<size=130%><b>YOU WIN!</b></size>\nCorrect answer beats wrong answer", 22, FontStyles.Normal, Color.white);
            outcome.alignment = TextAlignmentOptions.Center;
            var outcomeLE = outcome.gameObject.AddComponent<LayoutElement>();
            outcomeLE.minHeight = 80;

            // Continue button
            var continueBtn = CreateContinueButton(resultsPanelGo.transform);
        }

        private Button CreateContinueButton(Transform parent)
        {
            var buttonGo = new GameObject("ContinueButton");
            buttonGo.transform.SetParent(parent, false);

            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.5f, 0.9f);

            var button = buttonGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.5f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.6f, 1f);
            colors.pressedColor = new Color(0.15f, 0.4f, 0.8f);
            button.colors = colors;

            var buttonLE = buttonGo.AddComponent<LayoutElement>();
            buttonLE.minWidth = 200;
            buttonLE.minHeight = 50;
            buttonLE.preferredHeight = 50;

            var buttonText = CreateText(buttonGo.transform, "Text", "CONTINUE", 18, FontStyles.Bold, Color.white);
            var textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            buttonText.alignment = TextAlignmentOptions.Center;

            return button;
        }

        private void CreateDebugCanvas()
        {
            var canvasGo = new GameObject("DebugCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = DebugCanvasSortOrder;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            CreateDebugPanelUI(canvasGo.transform);
        }

        private void CreateDebugPanelUI(Transform canvasTransform)
        {
            var panelGo = CreatePanel(canvasTransform, "SessionDebugPanel", new Color(0.1f, 0.1f, 0.15f, 0.95f));
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(340, 520);

            var panelLayout = panelGo.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(12, 12, 12, 12);
            panelLayout.spacing = 10;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;

            // Title
            var title = CreateText(panelGo.transform, "Title", "SESSION DEBUG (F1)", 18, FontStyles.Bold, Color.white);
            var titleLE = title.gameObject.AddComponent<LayoutElement>();
            titleLE.minHeight = 30;

            // Session buttons
            var hvbButton = CreateDebugButton(panelGo.transform, "HumanVsBotButton", "Human vs Bot", new Color(0.2f, 0.5f, 0.9f));
            var bvbButton = CreateDebugButton(panelGo.transform, "BotVsBotButton", "Bot vs Bot", new Color(0.4f, 0.4f, 0.8f));

            // Divider
            CreateDivider(panelGo.transform);

            // Mode section
            var modeLabel = CreateText(panelGo.transform, "ModeLabel", "BOT MODE:", 14, FontStyles.Bold, Color.white);
            var modeLabelLE = modeLabel.gameObject.AddComponent<LayoutElement>();
            modeLabelLE.minHeight = 25;

            // Mode buttons row
            var modeRow = new GameObject("ModeRow");
            modeRow.transform.SetParent(panelGo.transform, false);
            var modeRowLayout = modeRow.AddComponent<HorizontalLayoutGroup>();
            modeRowLayout.spacing = 10;
            modeRowLayout.childForceExpandWidth = true;
            modeRowLayout.childForceExpandHeight = false;
            var modeRowLE = modeRow.AddComponent<LayoutElement>();
            modeRowLE.minHeight = 40;

            var autoButton = CreateDebugButton(modeRow.transform, "AutomaticModeButton", "Auto", new Color(0.2f, 0.6f, 0.3f));
            var manualButton = CreateDebugButton(modeRow.transform, "ManualModeButton", "Manual", new Color(0.5f, 0.5f, 0.5f));

            // Mode status
            var modeStatus = CreateText(panelGo.transform, "ModeStatusText", "BOT: Auto (Correct)", 12, FontStyles.Italic, new Color(0.2f, 0.6f, 0.3f));
            var modeStatusLE = modeStatus.gameObject.AddComponent<LayoutElement>();
            modeStatusLE.minHeight = 20;

            // MCQ Override group
            var mcqGroup = CreateOverrideGroup(panelGo.transform, "McqOverrideGroup", "MCQ (click to make bot answer):");
            var mcqBtnRow = new GameObject("McqButtons");
            mcqBtnRow.transform.SetParent(mcqGroup.transform, false);
            var mcqBtnLayout = mcqBtnRow.AddComponent<HorizontalLayoutGroup>();
            mcqBtnLayout.spacing = 10;
            mcqBtnLayout.childForceExpandWidth = true;
            var mcqBtnLE = mcqBtnRow.AddComponent<LayoutElement>();
            mcqBtnLE.minHeight = 40;

            var correctBtn = CreateDebugButton(mcqBtnRow.transform, "BotCorrectButton", "Correct", new Color(0.2f, 0.7f, 0.3f));
            var wrongBtn = CreateDebugButton(mcqBtnRow.transform, "BotWrongButton", "Wrong", new Color(0.8f, 0.3f, 0.3f));
            mcqGroup.SetActive(false);

            // Numeric Override group
            var numericGroup = CreateOverrideGroup(panelGo.transform, "NumericOverrideGroup", "Numeric (click to make bot answer):");
            var numericInputRow = new GameObject("NumericInputRow");
            numericInputRow.transform.SetParent(numericGroup.transform, false);
            var numInputLayout = numericInputRow.AddComponent<HorizontalLayoutGroup>();
            numInputLayout.spacing = 10;
            numInputLayout.childForceExpandWidth = false;
            var numInputLE = numericInputRow.AddComponent<LayoutElement>();
            numInputLE.minHeight = 40;

            CreateDebugNumericInput(numericInputRow.transform);
            var sendBtn = CreateDebugButton(numericInputRow.transform, "SetNumericButton", "Send", new Color(0.5f, 0.5f, 0.8f));
            var sendLE = sendBtn.GetComponent<LayoutElement>();
            if (sendLE == null) sendLE = sendBtn.gameObject.AddComponent<LayoutElement>();
            sendLE.flexibleWidth = 0;
            sendLE.minWidth = 80;

            var numericStatus = CreateText(numericGroup.transform, "NumericOverrideStatusText", "", 11, FontStyles.Normal, new Color(0.3f, 0.85f, 0.4f));
            numericGroup.SetActive(false);

            // Divider
            CreateDivider(panelGo.transform);

            // Status text
            var statusText = CreateText(panelGo.transform, "StatusText", "Ready. Press a button to start.", 12, FontStyles.Normal, Color.white);
            var statusLE = statusText.gameObject.AddComponent<LayoutElement>();
            statusLE.minHeight = 40;
            statusLE.flexibleHeight = 1;

            // Result text
            var resultText = CreateText(panelGo.transform, "ResultText", "", 10, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));
            resultText.alignment = TextAlignmentOptions.TopLeft;
            var resultLE = resultText.gameObject.AddComponent<LayoutElement>();
            resultLE.minHeight = 60;
            resultLE.flexibleHeight = 2;

            // Add SessionDebugPanel component
            debugPanel = panelGo.AddComponent<SessionDebugPanel>();
        }

        private GameObject CreateOverrideGroup(Transform parent, string name, string label)
        {
            var groupGo = new GameObject(name);
            groupGo.transform.SetParent(parent, false);
            var groupLayout = groupGo.AddComponent<VerticalLayoutGroup>();
            groupLayout.spacing = 8;
            groupLayout.childForceExpandWidth = true;
            groupLayout.childForceExpandHeight = false;

            var labelText = CreateText(groupGo.transform, "Label", label, 11, FontStyles.Bold, Color.white);
            var labelLE = labelText.gameObject.AddComponent<LayoutElement>();
            labelLE.minHeight = 20;

            return groupGo;
        }

        private void CreateDebugNumericInput(Transform parent)
        {
            var inputGo = new GameObject("NumericOverrideInput");
            inputGo.transform.SetParent(parent, false);

            var inputImage = inputGo.AddComponent<Image>();
            inputImage.color = new Color(0.2f, 0.2f, 0.25f);

            var inputLE = inputGo.AddComponent<LayoutElement>();
            inputLE.minWidth = 150;
            inputLE.flexibleWidth = 1;
            inputLE.minHeight = 35;

            var textAreaGo = new GameObject("Text Area");
            textAreaGo.transform.SetParent(inputGo.transform, false);
            var textAreaRect = textAreaGo.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 5);
            textAreaRect.offsetMax = new Vector2(-10, -5);

            var placeholderText = CreateText(textAreaGo.transform, "Placeholder", "Value...", 12, FontStyles.Italic, new Color(0.5f, 0.5f, 0.5f));
            var phRect = placeholderText.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;

            var inputText = CreateText(textAreaGo.transform, "Text", "", 12, FontStyles.Normal, Color.white);
            var inputTextRect = inputText.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;
            inputText.alignment = TextAlignmentOptions.MidlineLeft;

            var inputField = inputGo.AddComponent<TMP_InputField>();
            inputField.textViewport = textAreaRect;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
        }

        private Button CreateDebugButton(Transform parent, string name, string label, Color bgColor)
        {
            var buttonGo = new GameObject(name);
            buttonGo.transform.SetParent(parent, false);

            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = bgColor;

            var button = buttonGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.2f;
            colors.pressedColor = bgColor * 0.8f;
            button.colors = colors;

            var buttonLE = buttonGo.AddComponent<LayoutElement>();
            buttonLE.minHeight = 35;
            buttonLE.flexibleWidth = 1;

            var buttonText = CreateText(buttonGo.transform, "Text", label, 14, FontStyles.Bold, Color.white);
            var textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            buttonText.alignment = TextAlignmentOptions.Center;

            return button;
        }

        private void CreateDivider(Transform parent)
        {
            var divider = CreateText(parent, "Divider", "─────────────────────────", 10, FontStyles.Normal, new Color(0.4f, 0.4f, 0.4f));
            var dividerLE = divider.gameObject.AddComponent<LayoutElement>();
            dividerLE.minHeight = 15;
        }

        private void CreateManagers()
        {
            var managerGo = GameObject.Find("QuizManager");
            if (managerGo == null)
            {
                managerGo = new GameObject("QuizManager");
            }

            sessionController = managerGo.AddComponent<QuizSessionController>();
            humanProvider = managerGo.AddComponent<HumanAnswerProvider>();

            var botGo = new GameObject("BotProvider");
            botGo.transform.SetParent(managerGo.transform);
            botProvider = botGo.AddComponent<BotAnswerProvider>();
        }

        private void WireReferences()
        {
            if (humanProvider != null && quizUI != null)
            {
                humanProvider.SetQuizUI(quizUI);
                humanProvider.SetPlayerId(0);
            }

            if (botProvider != null)
            {
                botProvider.SetPlayerId(1);
            }

            AssignDebugPanelReferences();
            AssignSessionControllerReferences();
            
            // IMPORTANT: Re-initialize listeners after buttons are assigned via reflection
            if (quizUI != null)
            {
                quizUI.ReinitializeListeners();
                
                // Hide quiz UI until session starts
                quizUI.HideQuiz();
            }
        }

        private void AssignQuizUIReferences(GameObject quizPanelGo)
        {
            if (quizUI == null) return;

            var type = typeof(QuizUIController);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            SetField(type, quizUI, "quizPanel", quizPanelGo, flags);
            SetField(type, quizUI, "mcqPanel", FindChild(quizPanelGo.transform, "MCQPanel"), flags);
            SetField(type, quizUI, "numericPanel", FindChild(quizPanelGo.transform, "NumericPanel"), flags);
            SetField(type, quizUI, "resultsPanel", FindChild(quizPanelGo.transform, "ResultsPanel"), flags);

            var header = FindChild(quizPanelGo.transform, "Header");
            if (header != null)
            {
                var badgeLeft = FindComponent<PlayerStatusBadge>(header.transform, "PlayerBadgeLeft");
                var badgeRight = FindComponent<PlayerStatusBadge>(header.transform, "PlayerBadgeRight");
                Debug.Log($"[QuizSceneSetup] Found badges: Left={badgeLeft != null}, Right={badgeRight != null}");
                SetField(type, quizUI, "playerBadgeLeft", badgeLeft, flags);
                SetField(type, quizUI, "playerBadgeRight", badgeRight, flags);

                var timer = FindChild(header.transform, "Timer");
                if (timer != null)
                {
                    SetField(type, quizUI, "timerText", FindComponent<TextMeshProUGUI>(timer.transform, "TimerText"), flags);
                    SetField(type, quizUI, "timerFillImage", FindComponent<Image>(timer.transform, "FillRing"), flags);
                }
            }

            var questionSection = FindChild(quizPanelGo.transform, "QuestionSection");
            if (questionSection != null)
            {
                SetField(type, quizUI, "questionText", FindComponent<TextMeshProUGUI>(questionSection.transform, "QuestionText"), flags);
                SetField(type, quizUI, "categoryText", FindComponent<TextMeshProUGUI>(questionSection.transform, "CategoryText"), flags);
                SetField(type, quizUI, "roundIndicatorText", FindComponent<TextMeshProUGUI>(questionSection.transform, "RoundIndicator"), flags);
            }

            // MCQ buttons
            var mcqPanel = FindChild(quizPanelGo.transform, "MCQPanel");
            if (mcqPanel != null)
            {
                var buttons = new Button[4];
                var buttonTexts = new TextMeshProUGUI[4];
                var buttonImages = new Image[4];

                for (int i = 0; i < 4; i++)
                {
                    var btn = FindChild(mcqPanel.transform, $"McqButton{i}");
                    if (btn != null)
                    {
                        buttons[i] = btn.GetComponent<Button>();
                        buttonImages[i] = btn.GetComponent<Image>();
                        buttonTexts[i] = FindComponent<TextMeshProUGUI>(btn.transform, "Text");
                    }
                }

                SetField(type, quizUI, "mcqButtons", buttons, flags);
                SetField(type, quizUI, "mcqButtonTexts", buttonTexts, flags);
                SetField(type, quizUI, "mcqButtonImages", buttonImages, flags);
            }

            // Numeric panel
            var numericPanel = FindChild(quizPanelGo.transform, "NumericPanel");
            if (numericPanel != null)
            {
                var inputRow = FindChild(numericPanel.transform, "InputRow");
                if (inputRow != null)
                {
                    SetField(type, quizUI, "numericInputField", FindComponent<TMP_InputField>(inputRow.transform, "NumericInput"), flags);
                    SetField(type, quizUI, "numericSubmitButton", FindComponent<Button>(inputRow.transform, "SubmitButton"), flags);
                    SetField(type, quizUI, "unitText", FindComponent<TextMeshProUGUI>(inputRow.transform, "UnitText"), flags);
                }
                SetField(type, quizUI, "rangeHintText", FindComponent<TextMeshProUGUI>(numericPanel.transform, "RangeHint"), flags);
            }

            // Results panel
            var resultsPanel = FindChild(quizPanelGo.transform, "ResultsPanel");
            if (resultsPanel != null)
            {
                SetField(type, quizUI, "playerResultText", FindComponent<TextMeshProUGUI>(resultsPanel.transform, "PlayerResultText"), flags);
                SetField(type, quizUI, "opponentResultText", FindComponent<TextMeshProUGUI>(resultsPanel.transform, "OpponentResultText"), flags);
                SetField(type, quizUI, "correctAnswerText", FindComponent<TextMeshProUGUI>(resultsPanel.transform, "CorrectAnswerText"), flags);
                SetField(type, quizUI, "roundOutcomeText", FindComponent<TextMeshProUGUI>(resultsPanel.transform, "RoundOutcomeText"), flags);
                SetField(type, quizUI, "continueButton", FindComponent<Button>(resultsPanel.transform, "ContinueButton"), flags);
            }
        }

        private void AssignDebugPanelReferences()
        {
            if (debugPanel == null) return;

            var type = typeof(SessionDebugPanel);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            SetField(type, debugPanel, "sessionController", sessionController, flags);
            SetField(type, debugPanel, "quizUI", quizUI, flags);
            SetField(type, debugPanel, "humanProvider", humanProvider, flags);
            SetField(type, debugPanel, "botProvider", botProvider, flags);

            // Questions
            SetField(type, debugPanel, "mcqQuestions", mcqQuestions, flags);
            SetField(type, debugPanel, "numericQuestions", numericQuestions, flags);

            // UI elements
            var panel = debugPanel.gameObject;
            SetField(type, debugPanel, "debugPanel", panel, flags);

            SetField(type, debugPanel, "humanVsBotButton", FindComponent<Button>(panel.transform, "HumanVsBotButton"), flags);
            SetField(type, debugPanel, "botVsBotButton", FindComponent<Button>(panel.transform, "BotVsBotButton"), flags);
            SetField(type, debugPanel, "statusText", FindComponent<TextMeshProUGUI>(panel.transform, "StatusText"), flags);
            SetField(type, debugPanel, "resultText", FindComponent<TextMeshProUGUI>(panel.transform, "ResultText"), flags);

            SetField(type, debugPanel, "automaticModeButton", FindComponent<Button>(panel.transform, "AutomaticModeButton"), flags);
            SetField(type, debugPanel, "manualModeButton", FindComponent<Button>(panel.transform, "ManualModeButton"), flags);
            SetField(type, debugPanel, "modeStatusText", FindComponent<TextMeshProUGUI>(panel.transform, "ModeStatusText"), flags);

            var mcqGroup = FindChild(panel.transform, "McqOverrideGroup");
            SetField(type, debugPanel, "mcqOverrideGroup", mcqGroup, flags);
            if (mcqGroup != null)
            {
                SetField(type, debugPanel, "botCorrectButton", FindComponent<Button>(mcqGroup.transform, "BotCorrectButton"), flags);
                SetField(type, debugPanel, "botWrongButton", FindComponent<Button>(mcqGroup.transform, "BotWrongButton"), flags);
            }

            var numericGroup = FindChild(panel.transform, "NumericOverrideGroup");
            SetField(type, debugPanel, "numericOverrideGroup", numericGroup, flags);
            if (numericGroup != null)
            {
                SetField(type, debugPanel, "numericOverrideInput", FindComponent<TMP_InputField>(numericGroup.transform, "NumericOverrideInput"), flags);
                SetField(type, debugPanel, "setNumericButton", FindComponent<Button>(numericGroup.transform, "SetNumericButton"), flags);
                SetField(type, debugPanel, "numericOverrideStatusText", FindComponent<TextMeshProUGUI>(numericGroup.transform, "NumericOverrideStatusText"), flags);
            }
        }

        private void AssignSessionControllerReferences()
        {
            if (sessionController == null || quizUI == null) return;

            var type = typeof(QuizSessionController);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            SetField(type, sessionController, "quizUI", quizUI, flags);
        }

        // =====================================================================
        // HELPER METHODS
        // =====================================================================

        private GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            var panelGo = new GameObject(name);
            panelGo.transform.SetParent(parent, false);

            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = bgColor;

            panelGo.AddComponent<RectTransform>();

            return panelGo;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string content, int fontSize, FontStyles style, Color color)
        {
            var textGo = new GameObject(name);
            textGo.transform.SetParent(parent, false);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;

            return text;
        }

        private static GameObject? FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child.gameObject;
                var found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static T? FindComponent<T>(Transform parent, string name) where T : Component
        {
            var go = FindChild(parent, name);
            return go != null ? go.GetComponent<T>() : null;
        }

        private static void SetField(System.Type type, object target, string fieldName, object? value, System.Reflection.BindingFlags flags)
        {
            var field = type.GetField(fieldName, flags);
            if (field != null && value != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
