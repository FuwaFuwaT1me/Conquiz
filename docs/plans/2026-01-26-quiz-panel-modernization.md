# QuizPanel Modernization Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Modernize QuizPanel with beautiful gradient glassmorphism UI, simultaneous player display, real-time answer tracking, and seamless round transitions.

**Architecture:** Refactor QuizUIController to support 2x2 button grid, create reusable PlayerStatusBadge component for real-time status, implement reveal system with player labels, add smooth animated transitions between rounds using Unity's Animation system.

**Tech Stack:** Unity 2022.3 LTS, TextMeshPro, Unity UI, C# events, Coroutines/Animation curves

---

## Task 1: Create PlayerStatusBadge Component

**Files:**
- Create: `Assets/_Project/Scripts/UI/PlayerStatusBadge.cs`

**Step 1: Write PlayerStatusBadge script**

Create the reusable status badge component that shows player name, timer ring, and status text.

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Conquiz.UI
{
    public enum BadgeState
    {
        Hidden,
        Thinking,
        Answered,
        TimedOut,
        ResultCorrect,
        ResultWrong
    }

    /// <summary>
    /// Displays real-time status for a player during quiz sessions.
    /// Shows player label, circular timer, and status text.
    /// </summary>
    public class PlayerStatusBadge : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerLabel;
        [SerializeField] private Image timerRing;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image background;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Colors")]
        [SerializeField] private Color playerColor = Color.cyan;
        [SerializeField] private Color timerColorFull = new Color(0.23f, 0.51f, 0.96f); // Blue
        [SerializeField] private Color timerColorMid = new Color(0.96f, 0.62f, 0.11f); // Yellow
        [SerializeField] private Color timerColorLow = new Color(0.94f, 0.27f, 0.27f); // Red
        [SerializeField] private Color correctColor = new Color(0.06f, 0.72f, 0.51f);
        [SerializeField] private Color wrongColor = new Color(0.94f, 0.27f, 0.27f);

        private BadgeState currentState = BadgeState.Hidden;

        void Awake()
        {
            if (timerRing != null)
            {
                timerRing.type = Image.Type.Filled;
                timerRing.fillMethod = Image.FillMethod.Radial360;
                timerRing.fillOrigin = (int)Image.Origin360.Top;
                timerRing.fillClockwise = false;
            }

            SetState(BadgeState.Hidden);
        }

        public void Initialize(string label, Color color)
        {
            if (playerLabel != null)
                playerLabel.text = label;

            playerColor = color;
        }

        public void SetState(BadgeState state)
        {
            currentState = state;

            switch (state)
            {
                case BadgeState.Hidden:
                    if (canvasGroup != null)
                        canvasGroup.alpha = 0f;
                    break;

                case BadgeState.Thinking:
                    if (canvasGroup != null)
                        canvasGroup.alpha = 1f;
                    if (statusText != null)
                        statusText.text = "Thinking...";
                    if (background != null)
                        background.color = new Color(1f, 1f, 1f, 0.1f);
                    break;

                case BadgeState.Answered:
                    if (statusText != null)
                        statusText.text = "Answered! ✓";
                    break;

                case BadgeState.TimedOut:
                    if (statusText != null)
                        statusText.text = "Time's up!";
                    if (timerRing != null)
                        timerRing.color = timerColorLow;
                    break;

                case BadgeState.ResultCorrect:
                    if (statusText != null)
                        statusText.text = "✓";
                    if (background != null)
                        background.color = correctColor;
                    break;

                case BadgeState.ResultWrong:
                    if (statusText != null)
                        statusText.text = "✗";
                    if (background != null)
                        background.color = wrongColor;
                    break;
            }
        }

        public void UpdateTimer(float normalizedTime)
        {
            if (timerRing != null)
            {
                timerRing.fillAmount = normalizedTime;

                // Color gradient based on time remaining
                if (normalizedTime > 0.5f)
                    timerRing.color = timerColorFull;
                else if (normalizedTime > 0.25f)
                    timerRing.color = Color.Lerp(timerColorMid, timerColorFull, (normalizedTime - 0.25f) / 0.25f);
                else
                    timerRing.color = Color.Lerp(timerColorLow, timerColorMid, normalizedTime / 0.25f);
            }
        }

        public void Reset()
        {
            SetState(BadgeState.Thinking);
            UpdateTimer(1f);
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Scripts/UI/PlayerStatusBadge.cs
git commit -m "feat: add PlayerStatusBadge component for real-time player status

- Displays player label, timer ring, status text
- Supports states: Hidden, Thinking, Answered, TimedOut, Result
- Color-coded timer with gradient (blue -> yellow -> red)
- Reusable for both players"
```

---

## Task 2: Update QuizUIController - Add Status Badges and 2x2 Grid

**Files:**
- Modify: `Assets/_Project/Scripts/UI/QuizUIController.cs`

**Step 1: Add status badge references and update serialized fields**

Add references for the two player status badges and update button layout for 2x2 grid.

```csharp
// Add after line 32 (after resultsPanel):
[Header("Player Status Badges")]
[SerializeField] private PlayerStatusBadge playerBadgeLeft;
[SerializeField] private PlayerStatusBadge playerBadgeRight;

[Header("Reveal Elements")]
[SerializeField] private GameObject playerLabelPrefab; // Small chip prefab for "YOU"/"OPPONENT"
```

**Step 2: Initialize status badges in Awake**

```csharp
// Add at end of Awake() method (before HideQuiz() call):
if (playerBadgeLeft != null)
    playerBadgeLeft.Initialize("YOU", new Color(0.08f, 0.72f, 0.65f)); // Teal

if (playerBadgeRight != null)
    playerBadgeRight.Initialize("OPPONENT", new Color(0.98f, 0.45f, 0.09f)); // Orange
```

**Step 3: Update ShowMcqQuestion to show status badges**

Replace the ShowMcqQuestion method:

```csharp
public void ShowMcqQuestion(McqQuestionData question, float customTimeLimit = 0f, string roundText = "Round 1: Multiple Choice")
{
    if (question == null) return;

    currentQuestion = question;
    hasAnswered = false;

    if (questionText != null)
        questionText.text = question.QuestionText;

    if (categoryText != null)
        categoryText.text = question.Category ?? "";

    if (roundIndicatorText != null)
        roundIndicatorText.text = roundText;

    // Setup buttons
    for (int i = 0; i < mcqButtons.Length && i < question.Choices.Length; i++)
    {
        if (mcqButtonTexts[i] != null)
            mcqButtonTexts[i].text = question.Choices[i];

        if (mcqButtons[i] != null)
        {
            mcqButtons[i].interactable = true;
            SetButtonStyle(mcqButtons[i], mcqButtonImages[i], neutralColor, Color.white);
        }
    }

    // Show status badges
    if (playerBadgeLeft != null)
        playerBadgeLeft.SetState(BadgeState.Thinking);

    if (playerBadgeRight != null)
        playerBadgeRight.SetState(BadgeState.Thinking);

    ShowPanel(mcqPanel, true);
    ShowPanel(numericPanel, false);
    ShowPanel(resultsPanel, false);
    ShowPanel(quizPanel, true);

    StartTimer(customTimeLimit > 0f ? customTimeLimit : defaultTimeLimit);
}
```

**Step 4: Update ShowNumericQuestion similarly**

Replace ShowNumericQuestion to include status badge resets:

```csharp
public void ShowNumericQuestion(NumericQuestionData question, float customTimeLimit = 0f, string roundText = "Round 2: Numeric")
{
    if (question == null) return;

    currentQuestion = question;
    hasAnswered = false;

    if (questionText != null)
        questionText.text = question.QuestionText;

    if (categoryText != null)
        categoryText.text = question.Category ?? "";

    if (roundIndicatorText != null)
        roundIndicatorText.text = roundText;

    if (unitText != null)
        unitText.text = question.Unit ?? "";

    if (rangeHintText != null)
        rangeHintText.text = $"Range: {question.AllowedRangeMin:N0} - {question.AllowedRangeMax:N0}";

    if (numericInputField != null)
    {
        numericInputField.text = "";
        numericInputField.interactable = true;
        numericInputField.contentType = question.DecimalPlaces > 0
            ? TMP_InputField.ContentType.DecimalNumber
            : TMP_InputField.ContentType.IntegerNumber;
        numericInputField.Select();
        numericInputField.ActivateInputField();
    }

    if (numericSubmitButton != null)
        numericSubmitButton.interactable = true;

    // Reset status badges
    if (playerBadgeLeft != null)
        playerBadgeLeft.SetState(BadgeState.Thinking);

    if (playerBadgeRight != null)
        playerBadgeRight.SetState(BadgeState.Thinking);

    ShowPanel(mcqPanel, false);
    ShowPanel(numericPanel, true);
    ShowPanel(resultsPanel, false);
    ShowPanel(quizPanel, true);

    StartTimer(customTimeLimit > 0f ? customTimeLimit : defaultTimeLimit);
}
```

**Step 5: Update timer to update badge timers**

Modify UpdateTimerDisplay method:

```csharp
private void UpdateTimerDisplay()
{
    float normalizedTime = timeRemaining / timeLimit;

    if (timerText != null)
    {
        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = seconds.ToString();
        timerText.color = timeRemaining <= 5f ? warningColor : Color.white;
    }

    if (timerFillImage != null)
    {
        timerFillImage.fillAmount = normalizedTime;
        timerFillImage.color = timeRemaining <= 5f ? warningColor : primaryColor;
    }

    // Update badge timers
    if (playerBadgeLeft != null && !hasAnswered)
        playerBadgeLeft.UpdateTimer(normalizedTime);

    if (playerBadgeRight != null)
        playerBadgeRight.UpdateTimer(normalizedTime);
}
```

**Step 6: Commit**

```bash
git add Assets/_Project/Scripts/UI/QuizUIController.cs
git commit -m "feat: integrate PlayerStatusBadge into QuizUIController

- Add left/right player status badges
- Initialize with player names and colors (YOU=teal, OPPONENT=orange)
- Update badges when showing questions
- Sync badge timers with question timer"
```

---

## Task 3: Add Player Answer Events and Badge Updates

**Files:**
- Modify: `Assets/_Project/Scripts/UI/QuizUIController.cs`

**Step 1: Add new event for opponent answers**

Add after existing events (around line 22):

```csharp
public event Action<int, float> OnMcqAnswerSubmitted;
public event Action<float, float> OnNumericAnswerSubmitted;
public event Action OnTimerExpired;
// ADD THESE:
public event Action OnPlayerAnswered; // Fired when local player answers
public event Action OnOpponentAnswered; // Fired when opponent answers (for testing)
```

**Step 2: Add public method to mark player as answered**

Add this public method to QuizUIController:

```csharp
/// <summary>
/// Updates the player badge to "Answered" state.
/// Called when player submits answer.
/// </summary>
public void MarkPlayerAnswered()
{
    if (playerBadgeLeft != null)
        playerBadgeLeft.SetState(BadgeState.Answered);
}

/// <summary>
/// Updates the opponent badge to "Answered" state.
/// Called when opponent submits answer.
/// </summary>
public void MarkOpponentAnswered()
{
    if (playerBadgeRight != null)
        playerBadgeRight.SetState(BadgeState.Answered);
}
```

**Step 3: Call MarkPlayerAnswered in OnMcqButtonClicked**

Modify OnMcqButtonClicked (around line 332):

```csharp
private void OnMcqButtonClicked(int index)
{
    if (hasAnswered || !isTimerRunning) return;

    hasAnswered = true;
    isTimerRunning = false;

    float responseTime = GetCurrentResponseTime();

    foreach (var btn in mcqButtons)
    {
        if (btn != null)
            btn.interactable = false;
    }

    // ADD THIS:
    MarkPlayerAnswered();

    OnMcqAnswerSubmitted?.Invoke(index, responseTime);
}
```

**Step 4: Call MarkPlayerAnswered in OnNumericSubmitClicked**

Modify OnNumericSubmitClicked (around line 350):

```csharp
private void OnNumericSubmitClicked()
{
    if (hasAnswered || !isTimerRunning) return;

    if (numericInputField == null || string.IsNullOrWhiteSpace(numericInputField.text))
        return;

    if (!float.TryParse(numericInputField.text, out float value))
        return;

    if (currentQuestion is NumericQuestionData numericQ)
    {
        value = numericQ.ClampToAllowedRange(value);
    }

    hasAnswered = true;
    isTimerRunning = false;

    float responseTime = GetCurrentResponseTime();

    if (numericInputField != null)
        numericInputField.interactable = false;
    if (numericSubmitButton != null)
        numericSubmitButton.interactable = false;

    // ADD THIS:
    MarkPlayerAnswered();

    OnNumericAnswerSubmitted?.Invoke(value, responseTime);
}
```

**Step 5: Commit**

```bash
git add Assets/_Project/Scripts/UI/QuizUIController.cs
git commit -m "feat: add badge updates when players answer

- Add MarkPlayerAnswered/MarkOpponentAnswered methods
- Update player badge to 'Answered' state on submit
- Provides visual feedback that answer was recorded"
```

---

## Task 4: Implement MCQ Answer Reveal System

**Files:**
- Modify: `Assets/_Project/Scripts/UI/QuizUIController.cs`
- Create: `Assets/_Project/Prefabs/UI/PlayerLabelChip.prefab` (will be created in Unity Editor)

**Step 1: Add reveal method to QuizUIController**

Add this new public method to show the MCQ reveal:

```csharp
/// <summary>
/// Shows MCQ reveal with player labels on buttons.
/// Highlights correct answer and shows who picked what.
/// </summary>
public IEnumerator ShowMcqRevealCoroutine(
    int correctIndex,
    int playerChoiceIndex,
    int opponentChoiceIndex,
    bool playerCorrect,
    bool opponentCorrect)
{
    // Step 1: Brief pause (0.3s)
    yield return new WaitForSeconds(0.3f);

    // Step 2: Highlight answers (0.7s)
    for (int i = 0; i < mcqButtons.Length; i++)
    {
        if (mcqButtons[i] == null) continue;

        mcqButtons[i].interactable = false;

        // Highlight correct answer
        if (i == correctIndex)
        {
            SetButtonStyle(mcqButtons[i], mcqButtonImages[i], correctColor, Color.white);
        }
        // Show player's wrong answer
        else if (i == playerChoiceIndex && !playerCorrect)
        {
            SetButtonBorder(mcqButtons[i], incorrectColor, 3f);
            AddPlayerLabel(mcqButtons[i].transform, "YOU", incorrectColor);
        }
        // Show opponent's wrong answer
        else if (i == opponentChoiceIndex && !opponentCorrect)
        {
            SetButtonBorder(mcqButtons[i], warningColor, 3f);
            AddPlayerLabel(mcqButtons[i].transform, "OPPONENT", warningColor);
        }
    }

    yield return new WaitForSeconds(0.5f);

    // Step 3: Update status badges (0.3s)
    if (playerBadgeLeft != null)
        playerBadgeLeft.SetState(playerCorrect ? BadgeState.ResultCorrect : BadgeState.ResultWrong);

    if (playerBadgeRight != null)
        playerBadgeRight.SetState(opponentCorrect ? BadgeState.ResultCorrect : BadgeState.ResultWrong);

    yield return new WaitForSeconds(0.5f);

    // Total time: ~1.5s
}

private void SetButtonBorder(Button button, Color borderColor, float borderWidth)
{
    // For MVP, we'll use the button's Outline component
    // In Unity Editor, ensure buttons have an Outline component
    var outline = button.GetComponent<Outline>();
    if (outline != null)
    {
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(borderWidth, borderWidth);
        outline.enabled = true;
    }
}

private void AddPlayerLabel(Transform buttonTransform, string labelText, Color backgroundColor)
{
    if (playerLabelPrefab == null) return;

    GameObject label = Instantiate(playerLabelPrefab, buttonTransform);
    label.name = $"Label_{labelText}";

    // Position in top-right corner
    RectTransform rectTransform = label.GetComponent<RectTransform>();
    if (rectTransform != null)
    {
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-10f, -10f);
    }

    // Set label text and color
    TextMeshProUGUI labelTextComponent = label.GetComponentInChildren<TextMeshProUGUI>();
    if (labelTextComponent != null)
    {
        labelTextComponent.text = labelText;
    }

    Image labelBackground = label.GetComponent<Image>();
    if (labelBackground != null)
    {
        labelBackground.color = backgroundColor;
    }
}
```

**Step 2: Add method to clear player labels**

```csharp
private void ClearPlayerLabels()
{
    foreach (var button in mcqButtons)
    {
        if (button == null) continue;

        // Find and destroy any player labels
        Transform[] children = button.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.name.StartsWith("Label_"))
            {
                Destroy(child.gameObject);
            }
        }

        // Disable outline
        var outline = button.GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;
    }
}
```

**Step 3: Call ClearPlayerLabels at start of ShowMcqQuestion**

Add at the beginning of ShowMcqQuestion method:

```csharp
public void ShowMcqQuestion(McqQuestionData question, float customTimeLimit = 0f, string roundText = "Round 1: Multiple Choice")
{
    if (question == null) return;

    // ADD THIS:
    ClearPlayerLabels();

    currentQuestion = question;
    hasAnswered = false;
    // ... rest of method
}
```

**Step 4: Commit**

```bash
git add Assets/_Project/Scripts/UI/QuizUIController.cs
git commit -m "feat: implement MCQ answer reveal system

- Add ShowMcqRevealCoroutine with timed sequence
- Highlight correct answer in green
- Show player labels (YOU/OPPONENT) on wrong answers
- Update status badges with correct/wrong states
- Clear labels when starting new question"
```

**Note:** The PlayerLabelChip prefab will need to be created in Unity Editor. It should be a small UI element with:
- Image component (rounded rect background)
- TextMeshProUGUI child (bold, uppercase text)
- Size: ~60x24px

---

## Task 5: Implement Round Transition Animation

**Files:**
- Modify: `Assets/_Project/Scripts/UI/QuizUIController.cs`

**Step 1: Add transition coroutine**

Add this new method to handle animated transitions:

```csharp
/// <summary>
/// Animates transition from Round 1 (MCQ) to Round 2 (Numeric).
/// Total duration: ~1.2-1.5 seconds.
/// </summary>
public IEnumerator TransitionToRound2Coroutine(NumericQuestionData numericQuestion, float customTimeLimit = 0f)
{
    if (numericQuestion == null) yield break;

    // Phase 1: Fade Out (0.4s)
    float fadeOutDuration = 0.4f;
    float elapsed = 0f;

    CanvasGroup mcqCanvasGroup = mcqPanel.GetComponent<CanvasGroup>();
    if (mcqCanvasGroup == null)
        mcqCanvasGroup = mcqPanel.AddComponent<CanvasGroup>();

    CanvasGroup questionCanvasGroup = questionText.GetComponent<CanvasGroup>();
    if (questionCanvasGroup == null)
        questionCanvasGroup = questionText.gameObject.AddComponent<CanvasGroup>();

    while (elapsed < fadeOutDuration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / fadeOutDuration;
        float easedT = 1f - Mathf.Pow(1f - t, 3f); // ease-out-cubic

        mcqCanvasGroup.alpha = 1f - easedT;
        questionCanvasGroup.alpha = 1f - easedT;

        // Scale down slightly
        mcqPanel.transform.localScale = Vector3.one * (1f - easedT * 0.05f);

        yield return null;
    }

    mcqCanvasGroup.alpha = 0f;
    questionCanvasGroup.alpha = 0f;

    // Phase 2: Slide Transition (0.5s)
    // Hide MCQ panel
    ShowPanel(mcqPanel, false);
    ClearPlayerLabels();

    // Prepare numeric panel (off-screen right)
    ShowPanel(numericPanel, true);
    CanvasGroup numericCanvasGroup = numericPanel.GetComponent<CanvasGroup>();
    if (numericCanvasGroup == null)
        numericCanvasGroup = numericPanel.AddComponent<CanvasGroup>();

    RectTransform numericRect = numericPanel.GetComponent<RectTransform>();
    Vector2 startPos = new Vector2(Screen.width, 0f);
    Vector2 endPos = Vector2.zero;

    float slideDuration = 0.5f;
    elapsed = 0f;

    while (elapsed < slideDuration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / slideDuration;
        // ease-in-out-cubic
        float easedT = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        if (numericRect != null)
            numericRect.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);

        yield return null;
    }

    if (numericRect != null)
        numericRect.anchoredPosition = endPos;

    // Phase 3: Fade In (0.3s)
    // Setup numeric question
    currentQuestion = numericQuestion;
    hasAnswered = false;

    if (questionText != null)
        questionText.text = numericQuestion.QuestionText;

    if (categoryText != null)
        categoryText.text = numericQuestion.Category ?? "";

    if (roundIndicatorText != null)
        roundIndicatorText.text = "Round 2: Numeric";

    if (unitText != null)
        unitText.text = numericQuestion.Unit ?? "";

    if (rangeHintText != null)
        rangeHintText.text = $"Range: {numericQuestion.AllowedRangeMin:N0} - {numericQuestion.AllowedRangeMax:N0}";

    if (numericInputField != null)
    {
        numericInputField.text = "";
        numericInputField.interactable = true;
        numericInputField.contentType = numericQuestion.DecimalPlaces > 0
            ? TMP_InputField.ContentType.DecimalNumber
            : TMP_InputField.ContentType.IntegerNumber;
    }

    if (numericSubmitButton != null)
        numericSubmitButton.interactable = true;

    // Reset status badges
    if (playerBadgeLeft != null)
        playerBadgeLeft.Reset();

    if (playerBadgeRight != null)
        playerBadgeRight.Reset();

    float fadeInDuration = 0.3f;
    elapsed = 0f;

    while (elapsed < fadeInDuration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / fadeInDuration;
        float easedT = 1f - Mathf.Pow(1f - t, 3f); // ease-out-cubic

        numericCanvasGroup.alpha = easedT;
        questionCanvasGroup.alpha = easedT;

        // Scale up slightly
        numericPanel.transform.localScale = Vector3.one * (0.95f + easedT * 0.05f);

        yield return null;
    }

    numericCanvasGroup.alpha = 1f;
    questionCanvasGroup.alpha = 1f;
    numericPanel.transform.localScale = Vector3.one;

    // Start numeric question timer
    StartTimer(customTimeLimit > 0f ? customTimeLimit : defaultTimeLimit);

    if (numericInputField != null)
    {
        numericInputField.Select();
        numericInputField.ActivateInputField();
    }
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Scripts/UI/QuizUIController.cs
git commit -m "feat: implement round transition animation

- Add TransitionToRound2Coroutine with 3 phases
- Phase 1: Fade out MCQ panel (0.4s)
- Phase 2: Slide numeric panel from right (0.5s)
- Phase 3: Fade in numeric content (0.3s)
- Smooth easing curves for natural feel
- Reset status badges for Round 2"
```

---

## Task 6: Update QuizSessionController to Use New UI Features

**Files:**
- Modify: `Assets/_Project/Scripts/Quiz/QuizSessionController.cs`

**Step 1: Update RunSessionCoroutine to call reveal and transition**

Replace the existing RunSessionCoroutine method with this updated version:

```csharp
private IEnumerator RunSessionCoroutine(
    McqQuestionData mcqQuestion,
    NumericQuestionData numericQuestion)
{
    sessionInProgress = true;

    Debug.Log("=== QUIZ SESSION START ===");

    // --- ROUND 1: MCQ ---
    Debug.Log("--- Round 1: MCQ ---");

    QuizAnswerResult p1McqAnswer = null;
    QuizAnswerResult p2McqAnswer = null;

    // Player 1 (Human) answers MCQ
    bool p1Done = false;
    player1Provider.RequestMcqAnswer(mcqQuestion, mcqTimeLimit, (result) =>
    {
        p1McqAnswer = result;
        p1Done = true;
    });

    // Wait for player 1
    while (!p1Done)
    {
        yield return null;
    }

    Debug.Log($"P1 MCQ: {p1McqAnswer}");

    // Player 2 (Bot) answers MCQ (silently, in parallel)
    bool p2Done = false;
    player2Provider.RequestMcqAnswer(mcqQuestion, mcqTimeLimit, (result) =>
    {
        p2McqAnswer = result;
        p2Done = true;

        // Mark opponent as answered in UI
        if (quizUI != null)
            quizUI.MarkOpponentAnswered();
    });

    while (!p2Done)
    {
        yield return null;
    }

    Debug.Log($"P2 MCQ: {p2McqAnswer}");

    // Show reveal animation
    if (quizUI != null)
    {
        yield return quizUI.ShowMcqRevealCoroutine(
            mcqQuestion.CorrectIndex,
            p1McqAnswer.TimedOut ? -1 : p1McqAnswer.McqChoiceIndex,
            p2McqAnswer.TimedOut ? -1 : p2McqAnswer.McqChoiceIndex,
            p1McqAnswer.IsCorrect,
            p2McqAnswer.IsCorrect
        );
    }

    // Evaluate Round 1
    var round1Result = EvaluateMcqRound(p1McqAnswer, p2McqAnswer);
    if (round1Result != null)
    {
        CompleteSession(round1Result);
        yield break;
    }

    // Tie in Round 1 -> proceed to Round 2
    Debug.Log("MCQ Tie -> Proceeding to Round 2");

    if (numericQuestion == null)
    {
        Debug.LogWarning("No numeric question provided, cannot break tie!");
        var noWinnerResult = SessionResult.CreateNoWinner(p1McqAnswer, p2McqAnswer);
        CompleteSession(noWinnerResult);
        yield break;
    }

    // --- TRANSITION TO ROUND 2 ---
    if (quizUI != null)
    {
        yield return quizUI.TransitionToRound2Coroutine(numericQuestion, numericTimeLimit);
    }

    // --- ROUND 2: NUMERIC ---
    Debug.Log("--- Round 2: Numeric ---");

    QuizAnswerResult p1NumericAnswer = null;
    QuizAnswerResult p2NumericAnswer = null;

    // Player 1 answers Numeric
    bool p1NumericDone = false;
    player1Provider.RequestNumericAnswer(numericQuestion, numericTimeLimit, (result) =>
    {
        p1NumericAnswer = result;
        p1NumericDone = true;
    });

    while (!p1NumericDone)
    {
        yield return null;
    }

    Debug.Log($"P1 Numeric: {p1NumericAnswer}");

    // Player 2 answers Numeric (silently)
    bool p2NumericDone = false;
    player2Provider.RequestNumericAnswer(numericQuestion, numericTimeLimit, (result) =>
    {
        p2NumericAnswer = result;
        p2NumericDone = true;

        // Mark opponent as answered
        if (quizUI != null)
            quizUI.MarkOpponentAnswered();
    });

    while (!p2NumericDone)
    {
        yield return null;
    }

    Debug.Log($"P2 Numeric: {p2NumericAnswer}");

    // Show both results (keep existing ShowRoundResults for now)
    string p1NumText = FormatNumericAnswer(p1NumericAnswer, numericQuestion);
    string p2NumText = FormatNumericAnswer(p2NumericAnswer, numericQuestion);
    string numOutcome = GetNumericOutcomeText(p1NumericAnswer, p2NumericAnswer, numericQuestion);

    if (quizUI != null)
    {
        quizUI.ShowRoundResults(
            p1NumText, p1NumericAnswer.IsCorrect,
            p2NumText, p2NumericAnswer.IsCorrect,
            numOutcome);
    }

    yield return new WaitForSeconds(resultDisplayTime);

    // Evaluate Round 2
    var round2Result = EvaluateNumericRound(
        p1McqAnswer, p2McqAnswer,
        p1NumericAnswer, p2NumericAnswer,
        numericQuestion);

    CompleteSession(round2Result);
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Quiz/QuizSessionController.cs
git commit -m "feat: integrate reveal and transition into session flow

- Call ShowMcqRevealCoroutine after both players answer MCQ
- Call TransitionToRound2Coroutine when proceeding to numeric
- Mark opponent as answered when bot submits
- Smooth animated flow between rounds"
```

---

## Task 7: Create UI Prefab Structure in Unity Editor

**Manual Unity Editor Steps:**

This task must be done in the Unity Editor. Create the following structure:

### Step 1: Create PlayerLabelChip Prefab

1. Create new UI → Image in Canvas
2. Name it "PlayerLabelChip"
3. Configure Image:
   - Color: White (will be set by script)
   - Sprite: UI/Skin
   - Image Type: Sliced
   - Width: 60, Height: 24
4. Add TextMeshProUGUI child:
   - Name: "Label"
   - Text: "YOU"
   - Font: Bold
   - Size: 10-12
   - Color: White
   - Alignment: Center
   - Auto Size: Off
5. Add RectTransform:
   - Anchors: Center
   - Pivot: (0.5, 0.5)
6. Drag to `Assets/_Project/Prefabs/UI/PlayerLabelChip.prefab`

### Step 2: Update QuizPanel Prefab

1. Open existing QuizPanel prefab
2. Add two PlayerStatusBadge GameObjects:
   - **Left Badge (YOU)**:
     - Position: Top-left (anchored)
     - Add PlayerStatusBadge component
     - Create child Image "TimerRing" (radial filled, white)
     - Create child TextMeshProUGUI "StatusText"
     - Create child Image "Background" (semi-transparent)
     - Add CanvasGroup component

   - **Right Badge (OPPONENT)**:
     - Position: Top-right (anchored)
     - Same structure as left badge

3. Update MCQ Button Grid:
   - Add GridLayoutGroup to mcqPanel
   - Cell Size: (width/2 - spacing, height appropriate)
   - Spacing: 10-20px
   - Constraint: Fixed Column Count = 2
   - Add Outline component to each button (disabled by default)

4. Assign References in QuizUIController:
   - Drag PlayerStatusBadge objects to playerBadgeLeft/Right
   - Drag PlayerLabelChip prefab to playerLabelPrefab

### Step 3: Apply Glassmorphism Styling

For each UI element:
- Background gradients: Use UI/Default material with gradient textures
- Rounded corners: Sliced sprites with rounded edges
- Shadows: Add Shadow components with offset (0, -4) and soft color

**Commit after Unity Editor work:**

```bash
git add Assets/_Project/Prefabs/UI/PlayerLabelChip.prefab
git add Assets/_Project/Scenes/QuizTestScene.unity  # Or wherever QuizPanel is
git commit -m "feat: create UI prefabs and update QuizPanel structure

- Add PlayerLabelChip prefab (60x24, rounded)
- Add PlayerStatusBadge instances to QuizPanel
- Update MCQ button grid to 2x2 layout with GridLayoutGroup
- Add Outline components to buttons for borders
- Wire up all references in QuizUIController"
```

---

## Task 8: Manual Testing and Polish

**Files:**
- Test in: Play Mode (QuizTestScene or equivalent)

**Step 1: Test MCQ flow**

1. Enter Play Mode
2. Start a quiz session
3. Verify:
   - Both status badges show "Thinking..."
   - Timer rings deplete in sync
   - Clicking answer marks you as "Answered! ✓"
   - Bot answer triggers opponent badge "Answered! ✓"
   - Reveal highlights correct answer in green
   - Wrong answers show correct player labels
   - Status badges turn green/red

**Step 2: Test round transition**

1. Trigger a tie (both correct or both wrong)
2. Verify:
   - Smooth fade out of MCQ panel
   - Slide-in animation of numeric panel
   - Status badges reset to "Thinking..."
   - Timers reset
   - Input field is focused

**Step 3: Test numeric flow**

1. Submit numeric answer
2. Verify:
   - Badge updates to "Answered! ✓"
   - Opponent answers (simulated)
   - Results display correctly

**Step 4: Polish pass**

Adjust timings, colors, sizes as needed:
- Reveal timing (currently 1.5s total)
- Transition timing (currently 1.2s total)
- Badge sizes and positions
- Button spacing in 2x2 grid
- Timer ring stroke width

**Step 5: Commit any polish changes**

```bash
git add -A
git commit -m "polish: adjust UI timings and styling

- Fine-tune reveal sequence timing
- Adjust transition animation speed
- Update badge sizes for mobile
- Polish button grid spacing"
```

---

## Task 9: Add Numeric Reveal Animation (Optional Enhancement)

**Files:**
- Modify: `Assets/_Project/Scripts/UI/QuizUIController.cs`

**Step 1: Add numeric reveal coroutine**

```csharp
/// <summary>
/// Shows numeric answer reveal with result cards.
/// Future: Replace with dart-board visualization.
/// </summary>
public IEnumerator ShowNumericRevealCoroutine(
    QuizAnswerResult playerAnswer,
    QuizAnswerResult opponentAnswer,
    float correctValue)
{
    // Phase 1: Lock inputs (0.3s)
    if (numericInputField != null)
        numericInputField.interactable = false;
    if (numericSubmitButton != null)
        numericSubmitButton.interactable = false;

    yield return new WaitForSeconds(0.3f);

    // Phase 2: Show result cards (1.0s)
    // For MVP, use existing ShowRoundResults
    string p1Text = $"{playerAnswer.NumericValue:F1} (off by {playerAnswer.NumericDistance:F1})";
    string p2Text = $"{opponentAnswer.NumericValue:F1} (off by {opponentAnswer.NumericDistance:F1})";
    string outcome = DetermineNumericOutcome(playerAnswer, opponentAnswer);

    ShowRoundResults(p1Text, playerAnswer.IsCorrect, p2Text, opponentAnswer.IsCorrect, outcome);

    yield return new WaitForSeconds(1.0f);

    // Phase 3: Update status badges
    if (playerBadgeLeft != null)
    {
        bool playerWon = playerAnswer.NumericDistance < opponentAnswer.NumericDistance;
        playerBadgeLeft.SetState(playerWon ? BadgeState.ResultCorrect : BadgeState.ResultWrong);
    }

    if (playerBadgeRight != null)
    {
        bool opponentWon = opponentAnswer.NumericDistance < playerAnswer.NumericDistance;
        playerBadgeRight.SetState(opponentWon ? BadgeState.ResultCorrect : BadgeState.ResultWrong);
    }

    yield return new WaitForSeconds(0.5f);
}

private string DetermineNumericOutcome(QuizAnswerResult player, QuizAnswerResult opponent)
{
    if (player.NumericDistance < opponent.NumericDistance)
        return "YOU WIN! Closer answer!";
    else if (opponent.NumericDistance < player.NumericDistance)
        return "OPPONENT WINS! Closer answer!";
    else if (player.ResponseTimeSeconds < opponent.ResponseTimeSeconds)
        return "YOU WIN! Same distance, faster time!";
    else
        return "OPPONENT WINS! Same distance, faster time!";
}
```

**Step 2: Call from QuizSessionController**

Update the numeric reveal section in RunSessionCoroutine:

```csharp
// Replace ShowRoundResults call with:
if (quizUI != null)
{
    yield return quizUI.ShowNumericRevealCoroutine(
        p1NumericAnswer,
        p2NumericAnswer,
        numericQuestion.CorrectValue
    );
}
```

**Step 3: Commit**

```bash
git add Assets/_Project/Scripts/UI/QuizUIController.cs
git add Assets/_Project/Scripts/Quiz/QuizSessionController.cs
git commit -m "feat: add numeric reveal animation

- Add ShowNumericRevealCoroutine for numeric results
- Lock inputs and show result cards
- Update status badges based on winner
- Placeholder for future dart-board visualization"
```

---

## Task 10: Final Integration Testing

**Manual Testing Checklist:**

### Full Session Flow
- [ ] Start quiz session
- [ ] Both badges show "Thinking..." with full timers
- [ ] Answer MCQ question
- [ ] Player badge updates to "Answered! ✓"
- [ ] Bot answers (opponent badge updates)
- [ ] Reveal shows correct answer highlighted
- [ ] Player labels appear on wrong answers
- [ ] Status badges turn green/red
- [ ] If tie: Smooth transition to Round 2
- [ ] Round 2 shows numeric question
- [ ] Badges reset to "Thinking..."
- [ ] Answer numeric question
- [ ] Results display correctly
- [ ] Winner determined and shown

### Edge Cases
- [ ] Timeout on MCQ (badge shows "Time's up!")
- [ ] Timeout on numeric
- [ ] Both players pick same wrong answer (both labels show)
- [ ] Both players correct in MCQ (tie → Round 2)
- [ ] Both players wrong in MCQ (tie → Round 2)
- [ ] Exact numeric answer (both players)
- [ ] Identical numeric answers (time tiebreaker)

### Visual Polish
- [ ] Timers deplete smoothly with color gradient
- [ ] Animations feel smooth (no stuttering)
- [ ] Button grid looks balanced
- [ ] Status badges are readable
- [ ] Labels don't overlap on buttons
- [ ] Transitions feel fast-paced but not rushed

### Mobile Testing (if available)
- [ ] Buttons are touch-friendly
- [ ] Text is readable on small screen
- [ ] Animations perform well
- [ ] Input field keyboard works properly

**Final commit:**

```bash
git add -A
git commit -m "test: verify full quiz session flow

- Tested MCQ reveal with all scenarios
- Tested round transition animation
- Tested numeric reveal
- Verified edge cases (timeouts, ties)
- Confirmed mobile-friendly layout"
```

---

## Post-Implementation Tasks

### Documentation
- Update README with new UI features
- Add screenshots/GIFs of new UI
- Document any editor setup steps

### Future Enhancements (Post-MVP)
- Dart-board visualization for numeric rounds
- Particle effects on correct answers
- Sound effects for state changes
- Haptic feedback on mobile
- Player avatars in badges
- Custom gradient shader for glass effect
- Animation clip assets instead of code-based tweening

### Performance Optimization
- Profile animation performance on low-end devices
- Consider DOTween library for more efficient tweening
- Pool player label chips instead of instantiate/destroy

---

## Success Criteria

✅ Beautiful gradient glassmorphism UI implemented
✅ Status badges show real-time player progress
✅ 2x2 button grid layout for MCQ
✅ Answer reveal clearly shows who picked what
✅ Smooth animated transition between rounds
✅ Numeric results display with comparison
✅ All animations feel polished and fast-paced
✅ Code is clean, commented, and maintainable
✅ Ready for future dart-board visualization enhancement

---

## Execution Notes

- Each task is designed to be completed and committed independently
- Follow TDD where applicable (though Unity UI makes this challenging)
- Test frequently in Play Mode
- Commit after each task completes
- Use meaningful commit messages following conventional commits format

**Total estimated implementation time:** 3-5 hours (excluding Unity Editor prefab work)
