using UnityEngine;
using Conquiz.Bots;

namespace Conquiz.Quiz
{
    /// <summary>
    /// Test harness for QuizSessionController.
    /// Press SPACE to start a bot vs bot session.
    /// </summary>
    public class QuizSessionTester : MonoBehaviour
    {
        [Header("Session Controller")]
        [SerializeField] private QuizSessionController sessionController;

        [Header("Bot Providers")]
        [SerializeField] private BotAnswerProvider bot1;
        [SerializeField] private BotAnswerProvider bot2;

        [Header("Test Questions")]
        [SerializeField] private McqQuestionData[] mcqQuestions;
        [SerializeField] private NumericQuestionData[] numericQuestions;

        private int mcqIndex;
        private int numericIndex;

        void Start()
        {
            // Auto-create session controller if not assigned
            if (sessionController == null)
            {
                sessionController = gameObject.AddComponent<QuizSessionController>();
            }

            // Auto-create bots if not assigned
            if (bot1 == null)
            {
                var bot1Go = new GameObject("Bot1");
                bot1Go.transform.SetParent(transform);
                bot1 = bot1Go.AddComponent<BotAnswerProvider>();
                bot1.SetPlayerId(0);
                bot1.SetDifficulty(0.7f, 30f, 0.2f); // 70% MCQ accuracy
            }

            if (bot2 == null)
            {
                var bot2Go = new GameObject("Bot2");
                bot2Go.transform.SetParent(transform);
                bot2 = bot2Go.AddComponent<BotAnswerProvider>();
                bot2.SetPlayerId(1);
                bot2.SetDifficulty(0.5f, 50f, 0.1f); // 50% MCQ accuracy (easier bot)
            }

            // Subscribe to events
            sessionController.OnMcqQuestionStarted += HandleMcqStart;
            sessionController.OnNumericQuestionStarted += HandleNumericStart;
            sessionController.OnPlayerAnswered += HandlePlayerAnswered;
            sessionController.OnSessionComplete += HandleSessionComplete;

            Debug.Log("=== QUIZ SESSION TESTER ===");
            Debug.Log("Press SPACE to run a Bot vs Bot session");
            Debug.Log("Press R to run 10 sessions and show statistics");
            Debug.Log("===========================");
        }

        void OnDestroy()
        {
            if (sessionController != null)
            {
                sessionController.OnMcqQuestionStarted -= HandleMcqStart;
                sessionController.OnNumericQuestionStarted -= HandleNumericStart;
                sessionController.OnPlayerAnswered -= HandlePlayerAnswered;
                sessionController.OnSessionComplete -= HandleSessionComplete;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartTestSession();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                StartCoroutine(RunMultipleSessions(10));
            }
        }

        private void StartTestSession()
        {
            if (sessionController.IsSessionInProgress)
            {
                Debug.Log("Session already in progress!");
                return;
            }

            if (mcqQuestions == null || mcqQuestions.Length == 0)
            {
                Debug.LogError("No MCQ questions assigned!");
                return;
            }

            var mcq = mcqQuestions[mcqIndex % mcqQuestions.Length];
            mcqIndex++;

            NumericQuestionData numeric = null;
            if (numericQuestions != null && numericQuestions.Length > 0)
            {
                numeric = numericQuestions[numericIndex % numericQuestions.Length];
                numericIndex++;
            }

            Debug.Log($"Starting session: MCQ=\"{mcq.QuestionText}\"");
            if (numeric != null)
            {
                Debug.Log($"Tiebreaker: Numeric=\"{numeric.QuestionText}\"");
            }

            sessionController.StartSession(bot1, bot2, mcq, numeric);
        }

        private System.Collections.IEnumerator RunMultipleSessions(int count)
        {
            int bot1Wins = 0;
            int bot2Wins = 0;
            int noWinner = 0;
            int decidedRound1 = 0;
            int decidedRound2 = 0;

            Debug.Log($"=== Running {count} sessions ===");

            for (int i = 0; i < count; i++)
            {
                if (mcqQuestions == null || mcqQuestions.Length == 0)
                {
                    Debug.LogError("No MCQ questions for batch test!");
                    yield break;
                }

                var mcq = mcqQuestions[mcqIndex % mcqQuestions.Length];
                mcqIndex++;

                NumericQuestionData numeric = null;
                if (numericQuestions != null && numericQuestions.Length > 0)
                {
                    numeric = numericQuestions[numericIndex % numericQuestions.Length];
                    numericIndex++;
                }

                SessionResult result = null;
                sessionController.StartSession(bot1, bot2, mcq, numeric, (r) => result = r);

                // Wait for session to complete
                while (result == null)
                {
                    yield return null;
                }

                // Tally results
                if (!result.HasWinner)
                {
                    noWinner++;
                }
                else if (result.WinnerPlayerId == bot1.PlayerId)
                {
                    bot1Wins++;
                }
                else
                {
                    bot2Wins++;
                }

                if (result.DecidingRound == 1)
                    decidedRound1++;
                else
                    decidedRound2++;

                // Small delay between sessions
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log("=== BATCH RESULTS ===");
            Debug.Log($"Bot1 (70% acc) wins: {bot1Wins}");
            Debug.Log($"Bot2 (50% acc) wins: {bot2Wins}");
            Debug.Log($"No winner: {noWinner}");
            Debug.Log($"Decided in Round 1: {decidedRound1}");
            Debug.Log($"Decided in Round 2: {decidedRound2}");
            Debug.Log("=====================");
        }

        private void HandleMcqStart(int playerId, McqQuestionData question)
        {
            Debug.Log($"[MCQ] Player {playerId} answering: {question.QuestionText}");
        }

        private void HandleNumericStart(int playerId, NumericQuestionData question)
        {
            Debug.Log($"[NUMERIC] Player {playerId} answering: {question.QuestionText}");
        }

        private void HandlePlayerAnswered(QuizAnswerResult result)
        {
            Debug.Log($"[ANSWER] {result}");
        }

        private void HandleSessionComplete(SessionResult result)
        {
            Debug.Log($"[SESSION COMPLETE] {result}");
        }
    }
}
