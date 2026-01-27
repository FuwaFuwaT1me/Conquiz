using System;
using System.Collections;
using UnityEngine;
using Conquiz.UI;

namespace Conquiz.Quiz
{
    /// <summary>
    /// Controls a 1v1 quiz session between two players.
    /// Shows both players' answers and handles round transitions.
    /// </summary>
    public class QuizSessionController : MonoBehaviour
    {
        [Header("Session Settings")]
        [SerializeField] private float mcqTimeLimit = 15f;
        [SerializeField] private float numericTimeLimit = 20f;
        [SerializeField] private float resultDisplayTime = 2f;

        [Header("UI Reference")]
        [SerializeField] private QuizUIController quizUI;

        // Events
        public event Action<SessionResult> OnSessionComplete;

        private IAnswerProvider player1Provider;
        private IAnswerProvider player2Provider;
        private bool sessionInProgress;

        public bool IsSessionInProgress => sessionInProgress;

        void Awake()
        {
            if (quizUI == null)
            {
                quizUI = FindObjectOfType<QuizUIController>();
            }
        }

        /// <summary>
        /// Starts a 1v1 quiz session.
        /// </summary>
        public void StartSession(
            IAnswerProvider provider1,
            IAnswerProvider provider2,
            McqQuestionData mcqQuestion,
            NumericQuestionData numericQuestion,
            Action<SessionResult> onComplete = null)
        {
            if (sessionInProgress)
            {
                Debug.LogWarning("QuizSessionController: Session already in progress!");
                return;
            }

            if (provider1 == null || provider2 == null)
            {
                Debug.LogError("QuizSessionController: Both answer providers must be set!");
                return;
            }

            if (mcqQuestion == null)
            {
                Debug.LogError("QuizSessionController: MCQ question is required!");
                return;
            }

            player1Provider = provider1;
            player2Provider = provider2;

            if (onComplete != null)
            {
                Action<SessionResult> wrapper = null;
                wrapper = (result) =>
                {
                    OnSessionComplete -= wrapper;
                    onComplete(result);
                };
                OnSessionComplete += wrapper;
            }

            StartCoroutine(RunSessionCoroutine(mcqQuestion, numericQuestion));
        }

        public void CancelSession()
        {
            if (!sessionInProgress) return;

            StopAllCoroutines();
            player1Provider?.CancelPendingRequest();
            player2Provider?.CancelPendingRequest();
            sessionInProgress = false;

            if (quizUI != null)
                quizUI.HideQuiz();
        }

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

            // Show player 1's result briefly
            if (quizUI != null && p1McqAnswer != null)
            {
                quizUI.ShowMcqResult(mcqQuestion.CorrectIndex,
                    p1McqAnswer.TimedOut ? -1 : p1McqAnswer.McqChoiceIndex);
            }

            yield return new WaitForSeconds(0.5f);

            // Player 2 (Bot) answers MCQ (silently)
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

            yield return new WaitForSeconds(0.3f);

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

            // Show both results
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

        private string FormatMcqAnswer(QuizAnswerResult answer, McqQuestionData question)
        {
            if (answer.TimedOut)
                return "Timed Out!";

            string choice = question.Choices[answer.McqChoiceIndex];
            string status = answer.IsCorrect ? "CORRECT" : "WRONG";
            return $"{choice} ({status})";
        }

        private string FormatNumericAnswer(QuizAnswerResult answer, NumericQuestionData question)
        {
            if (answer.TimedOut)
                return "Timed Out!";

            string exactText = answer.IsCorrect ? "EXACT!" : $"off by {answer.NumericDistance:F1}";
            return $"{answer.NumericValue:F1} ({exactText}) in {answer.ResponseTimeSeconds:F1}s";
        }

        private string GetMcqOutcomeText(QuizAnswerResult p1, QuizAnswerResult p2)
        {
            if (p1.IsCorrect && !p2.IsCorrect)
                return "YOU WIN THIS ROUND!";
            if (!p1.IsCorrect && p2.IsCorrect)
                return "OPPONENT WINS THIS ROUND!";
            if (p1.IsCorrect && p2.IsCorrect)
                return "TIE! Both correct - Tiebreaker needed!";
            return "TIE! Both wrong - Tiebreaker needed!";
        }

        private string GetNumericOutcomeText(QuizAnswerResult p1, QuizAnswerResult p2, NumericQuestionData question)
        {
            if (p1.TimedOut && p2.TimedOut)
                return "Both timed out - No winner!";
            if (p1.TimedOut)
                return "You timed out - OPPONENT WINS!";
            if (p2.TimedOut)
                return "Opponent timed out - YOU WIN!";

            float p1Dist = p1.NumericDistance;
            float p2Dist = p2.NumericDistance;

            if (p1.IsCorrect && p2.IsCorrect)
            {
                if (p1.ResponseTimeSeconds < p2.ResponseTimeSeconds)
                    return $"Both exact! You were faster ({p1.ResponseTimeSeconds:F2}s) - YOU WIN!";
                else
                    return $"Both exact! Opponent was faster - OPPONENT WINS!";
            }

            if (p1Dist < p2Dist)
                return $"You were closer! ({p1Dist:F1} vs {p2Dist:F1}) - YOU WIN!";
            if (p2Dist < p1Dist)
                return $"Opponent was closer! ({p2Dist:F1} vs {p1Dist:F1}) - OPPONENT WINS!";

            return "Identical answers - checking time...";
        }

        private SessionResult EvaluateMcqRound(QuizAnswerResult p1Answer, QuizAnswerResult p2Answer)
        {
            int p1Id = p1Answer.PlayerId;
            int p2Id = p2Answer.PlayerId;

            bool p1TimedOut = p1Answer.TimedOut;
            bool p2TimedOut = p2Answer.TimedOut;

            if (p1TimedOut && p2TimedOut)
                return null;

            if (p1TimedOut && !p2TimedOut)
            {
                return SessionResult.CreateFromMcq(p2Id, p1Id,
                    SessionDecisionType.McqAnswerVsTimeout, p1Answer, p2Answer);
            }

            if (!p1TimedOut && p2TimedOut)
            {
                return SessionResult.CreateFromMcq(p1Id, p2Id,
                    SessionDecisionType.McqAnswerVsTimeout, p1Answer, p2Answer);
            }

            bool p1Correct = p1Answer.IsCorrect;
            bool p2Correct = p2Answer.IsCorrect;

            if (p1Correct && !p2Correct)
            {
                return SessionResult.CreateFromMcq(p1Id, p2Id,
                    SessionDecisionType.McqCorrectVsWrong, p1Answer, p2Answer);
            }

            if (!p1Correct && p2Correct)
            {
                return SessionResult.CreateFromMcq(p2Id, p1Id,
                    SessionDecisionType.McqCorrectVsWrong, p1Answer, p2Answer);
            }

            return null;
        }

        private SessionResult EvaluateNumericRound(
            QuizAnswerResult p1McqAnswer,
            QuizAnswerResult p2McqAnswer,
            QuizAnswerResult p1NumericAnswer,
            QuizAnswerResult p2NumericAnswer,
            NumericQuestionData question)
        {
            int p1Id = p1NumericAnswer.PlayerId;
            int p2Id = p2NumericAnswer.PlayerId;

            bool p1TimedOut = p1NumericAnswer.TimedOut;
            bool p2TimedOut = p2NumericAnswer.TimedOut;

            if (p1TimedOut && p2TimedOut)
            {
                return SessionResult.CreateNoWinner(
                    p1McqAnswer, p2McqAnswer, p1NumericAnswer, p2NumericAnswer);
            }

            if (p1TimedOut && !p2TimedOut)
            {
                return SessionResult.CreateFromNumeric(p2Id, p1Id,
                    SessionDecisionType.NumericAnswerVsTimeout,
                    p1McqAnswer, p2McqAnswer, p1NumericAnswer, p2NumericAnswer);
            }

            if (!p1TimedOut && p2TimedOut)
            {
                return SessionResult.CreateFromNumeric(p1Id, p2Id,
                    SessionDecisionType.NumericAnswerVsTimeout,
                    p1McqAnswer, p2McqAnswer, p1NumericAnswer, p2NumericAnswer);
            }

            float p1Distance = p1NumericAnswer.NumericDistance;
            float p2Distance = p2NumericAnswer.NumericDistance;

            bool p1Exact = question.IsExact(p1NumericAnswer.NumericValue);
            bool p2Exact = question.IsExact(p2NumericAnswer.NumericValue);

            if (p1Exact && p2Exact)
            {
                if (p1NumericAnswer.ResponseTimeSeconds < p2NumericAnswer.ResponseTimeSeconds)
                {
                    return SessionResult.CreateFromNumeric(p1Id, p2Id,
                        SessionDecisionType.NumericFasterExact,
                        p1McqAnswer, p2McqAnswer, p1NumericAnswer, p2NumericAnswer);
                }
                else if (p2NumericAnswer.ResponseTimeSeconds < p1NumericAnswer.ResponseTimeSeconds)
                {
                    return SessionResult.CreateFromNumeric(p2Id, p1Id,
                        SessionDecisionType.NumericFasterExact,
                        p1McqAnswer, p2McqAnswer, p1NumericAnswer, p2NumericAnswer);
                }
                else
                {
                    return SessionResult.CreateNoWinner(
                        p1McqAnswer, p2McqAnswer, p1NumericAnswer, p2NumericAnswer);
                }
            }

            const float epsilon = 0.0001f;
            if (p1Distance < p2Distance - epsilon)
            {
                return SessionResult.CreateFromNumeric(p1Id, p2Id,
                    SessionDecisionType.NumericCloser,
                    p1McqAnswer, p2McqAnswer, p1NumericAnswer, p2NumericAnswer);
            }
            else if (p2Distance < p1Distance - epsilon)
            {
                return SessionResult.CreateFromNumeric(p2Id, p1Id,
                    SessionDecisionType.NumericCloser,
                    p1McqAnswer, p2McqAnswer, p1NumericAnswer, p2NumericAnswer);
            }
            else
            {
                if (p1NumericAnswer.ResponseTimeSeconds < p2NumericAnswer.ResponseTimeSeconds)
                {
                    return SessionResult.CreateFromNumeric(p1Id, p2Id,
                        SessionDecisionType.NumericFasterExact,
                        p1McqAnswer, p2McqAnswer, p1NumericAnswer, p2NumericAnswer);
                }
                else if (p2NumericAnswer.ResponseTimeSeconds < p1NumericAnswer.ResponseTimeSeconds)
                {
                    return SessionResult.CreateFromNumeric(p2Id, p1Id,
                        SessionDecisionType.NumericFasterExact,
                        p1McqAnswer, p2McqAnswer, p1NumericAnswer, p2NumericAnswer);
                }
                else
                {
                    return SessionResult.CreateNoWinner(
                        p1McqAnswer, p2McqAnswer, p1NumericAnswer, p2NumericAnswer);
                }
            }
        }

        private void CompleteSession(SessionResult result)
        {
            sessionInProgress = false;

            if (quizUI != null)
            {
                StartCoroutine(HideQuizDelayed(1f));
            }

            Debug.Log(result.GetDebugBreakdown());
            OnSessionComplete?.Invoke(result);
        }

        private IEnumerator HideQuizDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (quizUI != null)
                quizUI.HideQuiz();
        }
    }
}
