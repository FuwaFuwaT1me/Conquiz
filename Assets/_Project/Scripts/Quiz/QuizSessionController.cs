using System;
using System.Collections;
using UnityEngine;
using Conquiz.UI;

namespace Conquiz.Quiz
{
    /// <summary>
    /// Controls a 1v1 quiz session between two players.
    /// Shows both players' answers and handles round transitions.
    /// The result screen stays visible until Continue is clicked.
    /// </summary>
    public class QuizSessionController : MonoBehaviour
    {
        [Header("Session Settings")]
        [SerializeField] private float mcqTimeLimit = 15f;
        [SerializeField] private float numericTimeLimit = 20f;

        [Header("UI Reference")]
        [SerializeField] private QuizUIController quizUI;

        // Events
        public event Action<SessionResult> OnSessionComplete;

        private IAnswerProvider player1Provider;
        private IAnswerProvider player2Provider;
        private bool sessionInProgress;
        private bool continueClicked;

        public bool IsSessionInProgress => sessionInProgress;

        void Awake()
        {
            if (quizUI == null)
            {
                quizUI = FindObjectOfType<QuizUIController>();
            }
        }

        void OnEnable()
        {
            if (quizUI != null)
            {
                quizUI.OnContinueClicked += HandleContinueClicked;
            }
        }

        void OnDisable()
        {
            if (quizUI != null)
            {
                quizUI.OnContinueClicked -= HandleContinueClicked;
            }
        }

        private void HandleContinueClicked()
        {
            continueClicked = true;
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
            continueClicked = false;

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
                // Winner determined in Round 1
                yield return ShowFinalResultAndWait(round1Result, mcqQuestion, null);
                CompleteSession(round1Result);
                yield break;
            }

            // Tie in Round 1 -> proceed to Round 2
            Debug.Log("MCQ Tie -> Proceeding to Round 2");

            if (numericQuestion == null)
            {
                Debug.LogWarning("No numeric question provided, cannot break tie!");
                var noWinnerResult = SessionResult.CreateNoWinner(p1McqAnswer, p2McqAnswer);
                yield return ShowFinalResultAndWait(noWinnerResult, mcqQuestion, null);
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

            // Show numeric reveal animation
            if (quizUI != null)
            {
                yield return quizUI.ShowNumericRevealCoroutine(
                    p1NumericAnswer,
                    p2NumericAnswer,
                    numericQuestion.CorrectValue
                );
            }

            // Evaluate Round 2
            var round2Result = EvaluateNumericRound(
                p1McqAnswer, p2McqAnswer,
                p1NumericAnswer, p2NumericAnswer,
                numericQuestion);

            // Show final result and wait for Continue
            yield return ShowFinalResultAndWait(round2Result, mcqQuestion, numericQuestion);
            CompleteSession(round2Result);
        }

        private IEnumerator ShowFinalResultAndWait(
            SessionResult result,
            McqQuestionData mcqQuestion,
            NumericQuestionData numericQuestion)
        {
            if (quizUI == null) yield break;

            // Determine display values
            string playerAnswer;
            string opponentAnswer;
            float? playerError = null;
            float? opponentError = null;
            string correctAnswer;
            string winnerName;
            string decisionReason;

            bool isNumericRound = result.DecidingRound == 2 && result.Player1NumericAnswer != null;

            if (isNumericRound && numericQuestion != null)
            {
                // Numeric round result
                var p1 = result.Player1NumericAnswer;
                var p2 = result.Player2NumericAnswer;

                playerAnswer = p1.TimedOut ? "Timed Out" : $"{p1.NumericValue:F1}";
                opponentAnswer = p2.TimedOut ? "Timed Out" : $"{p2.NumericValue:F1}";
                playerError = p1.TimedOut ? null : (float?)p1.NumericDistance;
                opponentError = p2.TimedOut ? null : (float?)p2.NumericDistance;
                correctAnswer = $"{numericQuestion.CorrectValue:F1}";
            }
            else
            {
                // MCQ round result
                var p1 = result.Player1McqAnswer;
                var p2 = result.Player2McqAnswer;

                playerAnswer = p1.TimedOut ? "Timed Out" : mcqQuestion.Choices[p1.McqChoiceIndex];
                opponentAnswer = p2.TimedOut ? "Timed Out" : mcqQuestion.Choices[p2.McqChoiceIndex];
                correctAnswer = mcqQuestion.CorrectAnswer;
            }

            // Determine winner name
            if (!result.HasWinner)
            {
                winnerName = "NO WINNER";
            }
            else if (result.WinnerPlayerId == player1Provider.PlayerId)
            {
                winnerName = "YOU WIN!";
            }
            else
            {
                winnerName = "OPPONENT WINS!";
            }

            // Decision reason
            decisionReason = GetDecisionReasonText(result);

            // Show the final result UI
            quizUI.ShowFinalResult(
                playerAnswer,
                opponentAnswer,
                playerError,
                opponentError,
                correctAnswer,
                winnerName,
                decisionReason
            );

            // Wait for Continue button
            continueClicked = false;
            Debug.Log("Waiting for Continue button...");

            yield return quizUI.WaitForContinueCoroutine();

            Debug.Log("Continue clicked, proceeding...");
        }

        private string GetDecisionReasonText(SessionResult result)
        {
            switch (result.DecisionType)
            {
                case SessionDecisionType.McqCorrectVsWrong:
                    return "Correct answer beats wrong answer";
                case SessionDecisionType.McqAnswerVsTimeout:
                    return "Answer submitted beats timeout";
                case SessionDecisionType.NumericCloser:
                    return "Closer to the correct value";
                case SessionDecisionType.NumericFasterExact:
                    return "Same distance, faster response";
                case SessionDecisionType.NumericAnswerVsTimeout:
                    return "Answer submitted beats timeout";
                case SessionDecisionType.NoWinner:
                    return "Both players tied";
                default:
                    return "";
            }
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
                quizUI.HideQuiz();
            }

            Debug.Log(result.GetDebugBreakdown());
            OnSessionComplete?.Invoke(result);
        }
    }
}
