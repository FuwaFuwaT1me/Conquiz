using System;
using System.Collections;
using UnityEngine;
using Conquiz.Quiz;

namespace Conquiz.Bots
{
    /// <summary>
    /// Bot implementation of IAnswerProvider.
    /// Supports two modes controlled by DebugBotSettings:
    /// - Automatic: Bot answers correctly after random delay (1s to timeLimit).
    /// - Manual: Bot waits for user to click Correct/Wrong button in debug panel.
    /// </summary>
    public class BotAnswerProvider : MonoBehaviour, IAnswerProvider
    {
        [Header("Bot Identity")]
        [SerializeField] private int playerId = 1;

        [Header("Response Time (Automatic Mode)")]
        [Tooltip("Minimum thinking time in seconds")]
        [Min(1f)]
        [SerializeField] private float minResponseTime = 1f;

        private Coroutine pendingAnswerCoroutine;
        private Action<QuizAnswerResult> pendingMcqCallback;
        private Action<QuizAnswerResult> pendingNumericCallback;
        private McqQuestionData currentMcqQuestion;
        private NumericQuestionData currentNumericQuestion;
        private float currentTimeLimit;
        private float questionStartTime;

        public int PlayerId => playerId;

        void OnEnable()
        {
            DebugBotSettings.Instance.OnManualAnswerRequested += HandleManualAnswerRequest;
        }

        void OnDisable()
        {
            CancelPendingRequest();
            if (DebugBotSettings.Instance != null)
            {
                DebugBotSettings.Instance.OnManualAnswerRequested -= HandleManualAnswerRequest;
            }
        }

        /// <summary>
        /// Sets the bot's player ID at runtime.
        /// </summary>
        public void SetPlayerId(int id)
        {
            playerId = id;
        }

        /// <summary>
        /// Configures bot response time range.
        /// </summary>
        public void SetResponseTimeRange(float minTime, float maxTime)
        {
            minResponseTime = Mathf.Max(1f, minTime);
        }

        /// <summary>
        /// Legacy method for compatibility. Now uses DebugBotSettings.
        /// </summary>
        [Obsolete("Use DebugBotSettings.Instance instead")]
        public void SetDifficulty(float mcqAcc, float numericError, float exactChance)
        {
            // No-op: difficulty is now controlled by DebugBotSettings mode
        }

        /// <summary>
        /// Legacy method for compatibility. Now uses DebugBotSettings.
        /// </summary>
        [Obsolete("Use DebugBotSettings.Instance.SetMcqOverride() instead")]
        public void SetNextAnswerOverride(BotAnswerOverrideMode overrideMode)
        {
            if (overrideMode == BotAnswerOverrideMode.ForceCorrect)
                DebugBotSettings.Instance.SetMcqOverride(true);
            else if (overrideMode == BotAnswerOverrideMode.ForceWrong)
                DebugBotSettings.Instance.SetMcqOverride(false);
        }

        public void RequestMcqAnswer(
            McqQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady)
        {
            CancelPendingRequest();
            DebugBotSettings.Instance.ResetQuestionOverrides();
            
            currentMcqQuestion = question;
            currentTimeLimit = timeLimit;
            pendingMcqCallback = onAnswerReady;
            questionStartTime = Time.time;

            var settings = DebugBotSettings.Instance;
            
            if (settings.CurrentMode == BotAnswerMode.Automatic)
            {
                // Automatic mode: answer after random delay
                pendingAnswerCoroutine = StartCoroutine(
                    GenerateMcqAnswerCoroutine(question, timeLimit, onAnswerReady));
            }
            else
            {
                // Manual mode: wait for button click
                settings.IsWaitingForManualInput = true;
                settings.WaitingQuestionType = CurrentQuestionType.MCQ;
                Debug.Log($"[Bot {playerId}] Waiting for manual MCQ input...");
            }
        }

        public void RequestNumericAnswer(
            NumericQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady)
        {
            CancelPendingRequest();
            DebugBotSettings.Instance.ResetQuestionOverrides();
            
            currentNumericQuestion = question;
            currentTimeLimit = timeLimit;
            pendingNumericCallback = onAnswerReady;
            questionStartTime = Time.time;

            var settings = DebugBotSettings.Instance;
            
            if (settings.CurrentMode == BotAnswerMode.Automatic)
            {
                // Automatic mode: answer after random delay
                pendingAnswerCoroutine = StartCoroutine(
                    GenerateNumericAnswerCoroutine(question, timeLimit, onAnswerReady));
            }
            else
            {
                // Manual mode: wait for button click
                settings.IsWaitingForManualInput = true;
                settings.WaitingQuestionType = CurrentQuestionType.Numeric;
                settings.CurrentNumericCorrectValue = question.CorrectValue;
                Debug.Log($"[Bot {playerId}] Waiting for manual Numeric input (correct={question.CorrectValue})...");
            }
        }

        public void CancelPendingRequest()
        {
            if (pendingAnswerCoroutine != null)
            {
                StopCoroutine(pendingAnswerCoroutine);
                pendingAnswerCoroutine = null;
            }
            
            pendingMcqCallback = null;
            pendingNumericCallback = null;
            currentMcqQuestion = null;
            currentNumericQuestion = null;
            
            if (DebugBotSettings.Instance != null)
            {
                DebugBotSettings.Instance.IsWaitingForManualInput = false;
            }
        }

        /// <summary>
        /// Called when user clicks Correct/Wrong or SetNumeric in Manual mode.
        /// </summary>
        private void HandleManualAnswerRequest(bool isMcq)
        {
            var settings = DebugBotSettings.Instance;
            if (!settings.IsWaitingForManualInput)
                return;

            settings.IsWaitingForManualInput = false;
            float responseTime = Time.time - questionStartTime;

            if (isMcq && pendingMcqCallback != null && currentMcqQuestion != null)
            {
                // Generate MCQ answer based on settings
                bool answerCorrectly = settings.McqAnswerCorrect;
                int chosenIndex;
                
                if (answerCorrectly)
                {
                    chosenIndex = currentMcqQuestion.CorrectIndex;
                }
                else
                {
                    // Pick a random wrong answer
                    do
                    {
                        chosenIndex = UnityEngine.Random.Range(0, 4);
                    } while (chosenIndex == currentMcqQuestion.CorrectIndex);
                }

                bool isCorrect = currentMcqQuestion.IsCorrect(chosenIndex);

                var result = QuizAnswerResult.CreateMcqAnswer(
                    playerId,
                    chosenIndex,
                    responseTime,
                    isCorrect);

                Debug.Log($"[Bot {playerId}] Manual MCQ Answer: index={chosenIndex}, correct={isCorrect}, time={responseTime:F1}s");

                var callback = pendingMcqCallback;
                pendingMcqCallback = null;
                currentMcqQuestion = null;
                callback?.Invoke(result);
            }
            else if (!isMcq && pendingNumericCallback != null && currentNumericQuestion != null)
            {
                // Generate Numeric answer based on settings
                float botAnswer = settings.HasNumericOverride 
                    ? settings.NumericAnswerValue 
                    : currentNumericQuestion.CorrectValue;

                // Clamp to allowed range
                botAnswer = currentNumericQuestion.ClampToAllowedRange(botAnswer);

                float distance = currentNumericQuestion.GetDistance(botAnswer);
                bool isExact = currentNumericQuestion.IsExact(botAnswer);

                var result = QuizAnswerResult.CreateNumericAnswer(
                    playerId,
                    botAnswer,
                    responseTime,
                    distance,
                    isExact);

                Debug.Log($"[Bot {playerId}] Manual Numeric Answer: {botAnswer}, distance={distance:F2}, time={responseTime:F1}s");

                var callback = pendingNumericCallback;
                pendingNumericCallback = null;
                currentNumericQuestion = null;
                callback?.Invoke(result);
            }
        }

        private IEnumerator GenerateMcqAnswerCoroutine(
            McqQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady)
        {
            // Random thinking time: 1s to (timeLimit - 0.5s)
            float maxThinkTime = Mathf.Max(minResponseTime + 0.5f, timeLimit - 0.5f);
            float thinkTime = UnityEngine.Random.Range(minResponseTime, maxThinkTime);

            yield return new WaitForSeconds(thinkTime);

            // In Automatic mode, always answer correctly
            int chosenIndex = question.CorrectIndex;
            bool isCorrect = true;

            var result = QuizAnswerResult.CreateMcqAnswer(
                playerId,
                chosenIndex,
                thinkTime,
                isCorrect);

            Debug.Log($"[Bot {playerId}] Auto MCQ Answer: index={chosenIndex}, correct={isCorrect}, time={thinkTime:F1}s");

            pendingAnswerCoroutine = null;
            pendingMcqCallback = null;
            currentMcqQuestion = null;
            onAnswerReady?.Invoke(result);
        }

        private IEnumerator GenerateNumericAnswerCoroutine(
            NumericQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady)
        {
            // Random thinking time: 1s to (timeLimit - 0.5s)
            float maxThinkTime = Mathf.Max(minResponseTime + 0.5f, timeLimit - 0.5f);
            float thinkTime = UnityEngine.Random.Range(minResponseTime, maxThinkTime);

            yield return new WaitForSeconds(thinkTime);

            // In Automatic mode, always answer exactly correct
            float botAnswer = question.CorrectValue;
            float distance = 0f;
            bool isExact = true;

            var result = QuizAnswerResult.CreateNumericAnswer(
                playerId,
                botAnswer,
                thinkTime,
                distance,
                isExact);

            Debug.Log($"[Bot {playerId}] Auto Numeric Answer: {botAnswer}, exact={isExact}, time={thinkTime:F1}s");

            pendingAnswerCoroutine = null;
            pendingNumericCallback = null;
            currentNumericQuestion = null;
            onAnswerReady?.Invoke(result);
        }
    }

    /// <summary>
    /// Legacy enum for backward compatibility.
    /// </summary>
    public enum BotAnswerOverrideMode
    {
        None = 0,
        ForceCorrect = 1,
        ForceWrong = 2
    }
}
