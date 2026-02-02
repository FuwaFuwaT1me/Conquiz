using System;
using System.Collections;
using UnityEngine;
using Conquiz.UI;

namespace Conquiz.Quiz
{
    /// <summary>
    /// Human player implementation of IAnswerProvider.
    /// Connects to QuizUIController to display questions and capture answers.
    /// </summary>
    public class HumanAnswerProvider : MonoBehaviour, IAnswerProvider
    {
        [Header("References")]
        [SerializeField] private QuizUIController quizUI;

        [Header("Player Identity")]
        [SerializeField] private int playerId = 0;

        [Header("Feedback Settings")]
        [Tooltip("Time to show feedback before continuing to next question")]
        [SerializeField] private float feedbackDelay = 1.5f;

        private Action<QuizAnswerResult> pendingCallback;
        private QuestionData currentQuestion;
        private bool waitingForAnswer;
        private bool isSubscribed;

        public int PlayerId => playerId;

        /// <summary>
        /// Sets the player ID at runtime.
        /// </summary>
        public void SetPlayerId(int id)
        {
            playerId = id;
        }

        /// <summary>
        /// Sets the QuizUIController reference at runtime.
        /// </summary>
        public void SetQuizUI(QuizUIController ui)
        {
            // Unsubscribe from old
            UnsubscribeFromEvents();

            quizUI = ui;

            // Subscribe to new
            SubscribeToEvents();
        }

        void Awake()
        {
            // Auto-find QuizUIController if not assigned
            if (quizUI == null)
            {
                quizUI = FindObjectOfType<QuizUIController>();
            }
        }

        void Start()
        {
            // Only subscribe if not already subscribed via SetQuizUI
            if (!isSubscribed)
            {
                SubscribeToEvents();
            }
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (quizUI != null && !isSubscribed)
            {
                quizUI.OnMcqAnswerSubmitted += HandleMcqAnswer;
                quizUI.OnNumericAnswerSubmitted += HandleNumericAnswer;
                quizUI.OnTimerExpired += HandleTimeout;
                isSubscribed = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (quizUI != null && isSubscribed)
            {
                quizUI.OnMcqAnswerSubmitted -= HandleMcqAnswer;
                quizUI.OnNumericAnswerSubmitted -= HandleNumericAnswer;
                quizUI.OnTimerExpired -= HandleTimeout;
                isSubscribed = false;
            }
        }

        public void RequestMcqAnswer(
            McqQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady)
        {
            if (quizUI == null)
            {
                Debug.LogError("HumanAnswerProvider: QuizUIController not assigned!");
                onAnswerReady?.Invoke(QuizAnswerResult.CreateTimedOut(playerId, QuestionType.MultipleChoice));
                return;
            }

            CancelPendingRequest();

            currentQuestion = question;
            pendingCallback = onAnswerReady;
            waitingForAnswer = true;

            quizUI.ShowMcqQuestion(question, timeLimit);
        }

        public void RequestNumericAnswer(
            NumericQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady)
        {
            if (quizUI == null)
            {
                Debug.LogError("HumanAnswerProvider: QuizUIController not assigned!");
                onAnswerReady?.Invoke(QuizAnswerResult.CreateTimedOut(playerId, QuestionType.Numeric));
                return;
            }

            CancelPendingRequest();

            currentQuestion = question;
            pendingCallback = onAnswerReady;
            waitingForAnswer = true;

            quizUI.ShowNumericQuestion(question, timeLimit);
        }

        public void CancelPendingRequest()
        {
            StopAllCoroutines();

            if (waitingForAnswer && quizUI != null)
            {
                quizUI.HideQuiz();
            }

            waitingForAnswer = false;
            pendingCallback = null;
            currentQuestion = null;
        }

        private void HandleMcqAnswer(int choiceIndex, float responseTime)
        {
            if (!waitingForAnswer || currentQuestion == null)
                return;

            var mcq = currentQuestion as McqQuestionData;
            if (mcq == null)
                return;

            waitingForAnswer = false;

            bool isCorrect = mcq.IsCorrect(choiceIndex);

            // Show visual feedback
            quizUI.ShowMcqResult(mcq.CorrectIndex, choiceIndex);
            quizUI.ShowFeedback(
                isCorrect ? "Correct!" : $"Wrong! Answer: {mcq.CorrectAnswer}",
                isCorrect);

            var result = QuizAnswerResult.CreateMcqAnswer(
                playerId,
                choiceIndex,
                responseTime,
                isCorrect);

            // Delay callback to show feedback
            StartCoroutine(DelayedCallback(result));
        }

        private void HandleNumericAnswer(float value, float responseTime)
        {
            if (!waitingForAnswer || currentQuestion == null)
                return;

            var numeric = currentQuestion as NumericQuestionData;
            if (numeric == null)
                return;

            waitingForAnswer = false;

            float distance = numeric.GetDistance(value);
            bool isExact = numeric.IsExact(value);

            // Show visual feedback
            quizUI.ShowFeedback(
                isExact ? "Exact!" : $"Distance: {distance:F1} (Answer: {numeric.CorrectValue})",
                isExact);

            var result = QuizAnswerResult.CreateNumericAnswer(
                playerId,
                value,
                responseTime,
                distance,
                isExact);

            // Delay callback to show feedback
            StartCoroutine(DelayedCallback(result));
        }

        private void HandleTimeout()
        {
            if (!waitingForAnswer || currentQuestion == null)
                return;

            waitingForAnswer = false;

            // Show feedback
            if (currentQuestion is McqQuestionData mcq)
            {
                quizUI.ShowMcqResult(mcq.CorrectIndex, -1);
                quizUI.ShowFeedback($"Time's up! Answer: {mcq.CorrectAnswer}", false);
            }
            else if (currentQuestion is NumericQuestionData numeric)
            {
                quizUI.ShowFeedback($"Time's up! Answer: {numeric.CorrectValue}", false);
            }

            var result = QuizAnswerResult.CreateTimedOut(playerId, currentQuestion.Type);

            // Delay callback to show feedback
            StartCoroutine(DelayedCallback(result));
        }

        private IEnumerator DelayedCallback(QuizAnswerResult result)
        {
            // Wait to show feedback
            yield return new WaitForSeconds(feedbackDelay);

            // Small frame delay to let UI settle
            yield return null;

            var callback = pendingCallback;
            pendingCallback = null;
            currentQuestion = null;

            callback?.Invoke(result);
        }
    }
}
