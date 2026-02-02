using System;
using UnityEngine;

namespace Conquiz.Bots
{
    /// <summary>
    /// Bot answer modes for debug control.
    /// </summary>
    public enum BotAnswerMode
    {
        /// <summary>Bot answers correctly automatically with random delay.</summary>
        Automatic,
        /// <summary>Bot waits for debug panel button click to answer.</summary>
        Manual
    }

    /// <summary>
    /// Type of question currently being answered.
    /// </summary>
    public enum CurrentQuestionType
    {
        None,
        MCQ,
        Numeric
    }

    /// <summary>
    /// Centralized settings for controlling bot behavior during debug/testing.
    /// Singleton pattern - access via DebugBotSettings.Instance.
    /// </summary>
    public class DebugBotSettings : MonoBehaviour
    {
        private static DebugBotSettings _instance;
        public static DebugBotSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DebugBotSettings>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DebugBotSettings");
                        _instance = go.AddComponent<DebugBotSettings>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Bot Mode")]
        [SerializeField] private BotAnswerMode currentMode = BotAnswerMode.Automatic;

        [Header("Manual Mode Overrides")]
        [Tooltip("For MCQ: true = correct, false = incorrect")]
        [SerializeField] private bool mcqAnswerCorrect = true;

        [Tooltip("For Numeric: the exact value the bot will answer")]
        [SerializeField] private float numericAnswerValue = 0f;

        [Tooltip("Has the numeric value been set for this question?")]
        [SerializeField] private bool numericValueSet = false;

        // Events for UI updates
        public event Action<BotAnswerMode> OnModeChanged;
        public event Action<bool> OnMcqOverrideChanged;
        public event Action<float> OnNumericOverrideChanged;
        
        /// <summary>
        /// Event fired when bot should answer immediately (Manual mode).
        /// Parameter: true = MCQ, false = Numeric
        /// </summary>
        public event Action<bool> OnManualAnswerRequested;

        /// <summary>
        /// Is the bot currently waiting for manual input?
        /// </summary>
        public bool IsWaitingForManualInput { get; set; }

        /// <summary>
        /// Type of question currently being answered by the bot.
        /// </summary>
        public CurrentQuestionType WaitingQuestionType { get; set; } = CurrentQuestionType.None;

        /// <summary>
        /// The correct value for the current numeric question (used for Correct button).
        /// </summary>
        public float CurrentNumericCorrectValue { get; set; }

        /// <summary>
        /// Current bot answer mode (Automatic or Manual).
        /// </summary>
        public BotAnswerMode CurrentMode
        {
            get => currentMode;
            set
            {
                if (currentMode != value)
                {
                    currentMode = value;
                    Debug.Log($"[DebugBotSettings] Mode changed to: {value}");
                    OnModeChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// In Manual mode: whether the bot should answer MCQ correctly.
        /// </summary>
        public bool McqAnswerCorrect
        {
            get => mcqAnswerCorrect;
            set
            {
                mcqAnswerCorrect = value;
                Debug.Log($"[DebugBotSettings] MCQ override set to: {(value ? "Correct" : "Incorrect")}");
                OnMcqOverrideChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// In Manual mode: the numeric value the bot will answer.
        /// </summary>
        public float NumericAnswerValue
        {
            get => numericAnswerValue;
            set
            {
                numericAnswerValue = value;
                numericValueSet = true;
                Debug.Log($"[DebugBotSettings] Numeric override set to: {value}");
                OnNumericOverrideChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// Whether a numeric override value has been set for this question.
        /// </summary>
        public bool HasNumericOverride => numericValueSet;

        /// <summary>
        /// Resets per-question overrides. Call at start of each question.
        /// </summary>
        public void ResetQuestionOverrides()
        {
            numericValueSet = false;
            IsWaitingForManualInput = false;
            WaitingQuestionType = CurrentQuestionType.None;
        }

        /// <summary>
        /// Sets both mode and MCQ override in one call.
        /// </summary>
        public void SetMcqOverride(bool answerCorrectly)
        {
            mcqAnswerCorrect = answerCorrectly;
            Debug.Log($"[DebugBotSettings] MCQ override: {(answerCorrectly ? "CORRECT" : "INCORRECT")}");
            OnMcqOverrideChanged?.Invoke(answerCorrectly);
        }

        /// <summary>
        /// Sets the numeric answer override value.
        /// </summary>
        public void SetNumericOverride(float value)
        {
            numericAnswerValue = value;
            numericValueSet = true;
            Debug.Log($"[DebugBotSettings] Numeric override: {value}");
            OnNumericOverrideChanged?.Invoke(value);
        }

        /// <summary>
        /// Triggers the bot to answer immediately (Manual mode).
        /// </summary>
        public void TriggerManualMcqAnswer(bool answerCorrectly)
        {
            mcqAnswerCorrect = answerCorrectly;
            Debug.Log($"[DebugBotSettings] Manual MCQ answer triggered: {(answerCorrectly ? "CORRECT" : "INCORRECT")}");
            OnManualAnswerRequested?.Invoke(true); // true = MCQ
        }

        /// <summary>
        /// Triggers the bot to answer numeric question with current override value.
        /// </summary>
        public void TriggerManualNumericAnswer()
        {
            Debug.Log($"[DebugBotSettings] Manual Numeric answer triggered: {numericAnswerValue}");
            OnManualAnswerRequested?.Invoke(false); // false = Numeric
        }

        /// <summary>
        /// Triggers correct answer for any question type (MCQ or Numeric).
        /// </summary>
        public void TriggerCorrectAnswer()
        {
            if (WaitingQuestionType == CurrentQuestionType.MCQ)
            {
                TriggerManualMcqAnswer(true);
            }
            else if (WaitingQuestionType == CurrentQuestionType.Numeric)
            {
                numericAnswerValue = CurrentNumericCorrectValue;
                numericValueSet = true;
                TriggerManualNumericAnswer();
            }
            else
            {
                Debug.LogWarning("[DebugBotSettings] No question pending!");
            }
        }

        /// <summary>
        /// Triggers wrong answer for any question type (MCQ or Numeric).
        /// </summary>
        public void TriggerWrongAnswer()
        {
            if (WaitingQuestionType == CurrentQuestionType.MCQ)
            {
                TriggerManualMcqAnswer(false);
            }
            else if (WaitingQuestionType == CurrentQuestionType.Numeric)
            {
                // Answer with a very wrong value (far from correct)
                float wrongValue = CurrentNumericCorrectValue * 10f; // Way off
                if (Mathf.Abs(wrongValue) < 1f) wrongValue = 999f;
                numericAnswerValue = wrongValue;
                numericValueSet = true;
                TriggerManualNumericAnswer();
            }
            else
            {
                Debug.LogWarning("[DebugBotSettings] No question pending!");
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
