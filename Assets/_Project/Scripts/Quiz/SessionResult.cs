using System;
using System.Text;

namespace Conquiz.Quiz
{
    /// <summary>
    /// The outcome of a 1v1 quiz session.
    /// Contains winner information and detailed breakdown for debugging.
    /// </summary>
    [Serializable]
    public class SessionResult
    {
        /// <summary>
        /// The winning player's ID. -1 if no winner (both timed out).
        /// </summary>
        public int WinnerPlayerId { get; private set; }

        /// <summary>
        /// The losing player's ID. -1 if no loser.
        /// </summary>
        public int LoserPlayerId { get; private set; }

        /// <summary>
        /// True if there was a definitive winner.
        /// </summary>
        public bool HasWinner => WinnerPlayerId >= 0;

        /// <summary>
        /// Which round decided the outcome (1 = MCQ, 2 = Numeric).
        /// </summary>
        public int DecidingRound { get; private set; }

        /// <summary>
        /// How the session was decided.
        /// </summary>
        public SessionDecisionType DecisionType { get; private set; }

        /// <summary>
        /// Player 1's MCQ answer (Round 1).
        /// </summary>
        public QuizAnswerResult Player1McqAnswer { get; private set; }

        /// <summary>
        /// Player 2's MCQ answer (Round 1).
        /// </summary>
        public QuizAnswerResult Player2McqAnswer { get; private set; }

        /// <summary>
        /// Player 1's Numeric answer (Round 2, null if not reached).
        /// </summary>
        public QuizAnswerResult Player1NumericAnswer { get; private set; }

        /// <summary>
        /// Player 2's Numeric answer (Round 2, null if not reached).
        /// </summary>
        public QuizAnswerResult Player2NumericAnswer { get; private set; }

        /// <summary>
        /// Creates a session result decided in Round 1 (MCQ).
        /// </summary>
        public static SessionResult CreateFromMcq(
            int winnerPlayerId,
            int loserPlayerId,
            SessionDecisionType decisionType,
            QuizAnswerResult player1Answer,
            QuizAnswerResult player2Answer)
        {
            return new SessionResult
            {
                WinnerPlayerId = winnerPlayerId,
                LoserPlayerId = loserPlayerId,
                DecidingRound = 1,
                DecisionType = decisionType,
                Player1McqAnswer = player1Answer,
                Player2McqAnswer = player2Answer
            };
        }

        /// <summary>
        /// Creates a session result decided in Round 2 (Numeric).
        /// </summary>
        public static SessionResult CreateFromNumeric(
            int winnerPlayerId,
            int loserPlayerId,
            SessionDecisionType decisionType,
            QuizAnswerResult player1McqAnswer,
            QuizAnswerResult player2McqAnswer,
            QuizAnswerResult player1NumericAnswer,
            QuizAnswerResult player2NumericAnswer)
        {
            return new SessionResult
            {
                WinnerPlayerId = winnerPlayerId,
                LoserPlayerId = loserPlayerId,
                DecidingRound = 2,
                DecisionType = decisionType,
                Player1McqAnswer = player1McqAnswer,
                Player2McqAnswer = player2McqAnswer,
                Player1NumericAnswer = player1NumericAnswer,
                Player2NumericAnswer = player2NumericAnswer
            };
        }

        /// <summary>
        /// Creates a no-winner result (e.g., both timed out in all rounds).
        /// </summary>
        public static SessionResult CreateNoWinner(
            QuizAnswerResult player1McqAnswer,
            QuizAnswerResult player2McqAnswer,
            QuizAnswerResult player1NumericAnswer = null,
            QuizAnswerResult player2NumericAnswer = null)
        {
            int decidingRound = player1NumericAnswer != null ? 2 : 1;

            return new SessionResult
            {
                WinnerPlayerId = -1,
                LoserPlayerId = -1,
                DecidingRound = decidingRound,
                DecisionType = SessionDecisionType.NoWinner,
                Player1McqAnswer = player1McqAnswer,
                Player2McqAnswer = player2McqAnswer,
                Player1NumericAnswer = player1NumericAnswer,
                Player2NumericAnswer = player2NumericAnswer
            };
        }

        /// <summary>
        /// Returns a detailed breakdown string for debugging.
        /// </summary>
        public string GetDebugBreakdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SESSION RESULT ===");

            if (HasWinner)
            {
                sb.AppendLine($"Winner: Player {WinnerPlayerId}");
                sb.AppendLine($"Decided in Round {DecidingRound} by: {DecisionType}");
            }
            else
            {
                sb.AppendLine("No Winner");
                sb.AppendLine($"Reason: {DecisionType}");
            }

            sb.AppendLine();
            sb.AppendLine("--- Round 1 (MCQ) ---");
            sb.AppendLine($"  P1: {Player1McqAnswer}");
            sb.AppendLine($"  P2: {Player2McqAnswer}");

            if (Player1NumericAnswer != null || Player2NumericAnswer != null)
            {
                sb.AppendLine();
                sb.AppendLine("--- Round 2 (Numeric) ---");
                sb.AppendLine($"  P1: {Player1NumericAnswer}");
                sb.AppendLine($"  P2: {Player2NumericAnswer}");
            }

            sb.AppendLine("======================");
            return sb.ToString();
        }

        public override string ToString()
        {
            if (HasWinner)
                return $"SessionResult: Player {WinnerPlayerId} wins (Round {DecidingRound}, {DecisionType})";
            return $"SessionResult: No winner ({DecisionType})";
        }
    }

    /// <summary>
    /// How the session winner was determined.
    /// </summary>
    public enum SessionDecisionType
    {
        /// <summary>One player correct, other wrong in MCQ.</summary>
        McqCorrectVsWrong,

        /// <summary>One player answered MCQ, other timed out.</summary>
        McqAnswerVsTimeout,

        /// <summary>Player closer to correct numeric value.</summary>
        NumericCloser,

        /// <summary>Both exact numeric, faster response time wins.</summary>
        NumericFasterExact,

        /// <summary>One player answered Numeric, other timed out.</summary>
        NumericAnswerVsTimeout,

        /// <summary>No winner could be determined.</summary>
        NoWinner
    }
}
