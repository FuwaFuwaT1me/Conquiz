using System;

namespace Conquiz.Quiz
{
    /// <summary>
    /// Represents a player's answer to a quiz question.
    /// Captures answer data, response time, and correctness.
    /// </summary>
    [Serializable]
    public class QuizAnswerResult
    {
        /// <summary>
        /// Player ID who submitted this answer.
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// Type of question answered.
        /// </summary>
        public QuestionType QuestionType { get; set; }

        /// <summary>
        /// For MCQ: the index chosen (0-3). For Numeric: -1.
        /// </summary>
        public int McqChoiceIndex { get; set; } = -1;

        /// <summary>
        /// For Numeric: the value entered. For MCQ: 0.
        /// </summary>
        public float NumericValue { get; set; }

        /// <summary>
        /// Time in seconds from question display to answer submission.
        /// </summary>
        public float ResponseTimeSeconds { get; set; }

        /// <summary>
        /// Whether the answer was correct.
        /// </summary>
        public bool IsCorrect { get; set; }

        /// <summary>
        /// Whether the timer expired before answering.
        /// </summary>
        public bool TimedOut { get; set; }

        /// <summary>
        /// For Numeric: distance from correct answer.
        /// </summary>
        public float NumericDistance { get; set; }

        // --- Factory Methods ---

        /// <summary>
        /// Creates an MCQ answer result.
        /// </summary>
        public static QuizAnswerResult CreateMcqAnswer(
            int playerId,
            int choiceIndex,
            float responseTime,
            bool isCorrect)
        {
            return new QuizAnswerResult
            {
                PlayerId = playerId,
                QuestionType = QuestionType.MultipleChoice,
                McqChoiceIndex = choiceIndex,
                ResponseTimeSeconds = responseTime,
                IsCorrect = isCorrect,
                TimedOut = false
            };
        }

        /// <summary>
        /// Creates a Numeric answer result.
        /// </summary>
        public static QuizAnswerResult CreateNumericAnswer(
            int playerId,
            float value,
            float responseTime,
            float distance,
            bool isExact)
        {
            return new QuizAnswerResult
            {
                PlayerId = playerId,
                QuestionType = QuestionType.Numeric,
                NumericValue = value,
                ResponseTimeSeconds = responseTime,
                NumericDistance = distance,
                IsCorrect = isExact,
                TimedOut = false
            };
        }

        /// <summary>
        /// Creates a timed-out answer result.
        /// </summary>
        public static QuizAnswerResult CreateTimedOut(int playerId, QuestionType type)
        {
            return new QuizAnswerResult
            {
                PlayerId = playerId,
                QuestionType = type,
                TimedOut = true,
                IsCorrect = false,
                ResponseTimeSeconds = float.MaxValue
            };
        }

        public override string ToString()
        {
            if (TimedOut)
                return $"Player {PlayerId}: Timed Out";

            if (QuestionType == QuestionType.MultipleChoice)
                return $"Player {PlayerId}: MCQ[{McqChoiceIndex}] in {ResponseTimeSeconds:F2}s ({(IsCorrect ? "Correct" : "Wrong")})";

            return $"Player {PlayerId}: Numeric={NumericValue} (dist={NumericDistance:F2}) in {ResponseTimeSeconds:F2}s";
        }
    }
}
