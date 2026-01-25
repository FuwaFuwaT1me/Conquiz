using System;

namespace Conquiz.Quiz
{
    /// <summary>
    /// Interface for providing answers to quiz questions.
    /// Implementations can be human input (via UI) or bot AI.
    /// </summary>
    public interface IAnswerProvider
    {
        /// <summary>
        /// The player ID this provider answers for.
        /// </summary>
        int PlayerId { get; }

        /// <summary>
        /// Requests an MCQ answer from this provider.
        /// </summary>
        /// <param name="question">The MCQ question to answer</param>
        /// <param name="timeLimit">Time limit in seconds</param>
        /// <param name="onAnswerReady">Callback with the answer result</param>
        void RequestMcqAnswer(
            McqQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady);

        /// <summary>
        /// Requests a Numeric answer from this provider.
        /// </summary>
        /// <param name="question">The Numeric question to answer</param>
        /// <param name="timeLimit">Time limit in seconds</param>
        /// <param name="onAnswerReady">Callback with the answer result</param>
        void RequestNumericAnswer(
            NumericQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady);

        /// <summary>
        /// Cancels any pending answer request.
        /// </summary>
        void CancelPendingRequest();
    }
}
