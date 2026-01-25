using UnityEngine;
using Conquiz.UI;

namespace Conquiz.Quiz
{
    /// <summary>
    /// Simple test script to trigger quiz questions in Play Mode.
    /// Press M for MCQ, N for Numeric, H to hide.
    /// </summary>
    public class QuizTester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QuizUIController quizUI;

        [Header("Test Questions")]
        [SerializeField] private McqQuestionData[] mcqQuestions;
        [SerializeField] private NumericQuestionData[] numericQuestions;

        [Header("Settings")]
        [SerializeField] private float timeLimit = 15f;

        private int mcqIndex = 0;
        private int numericIndex = 0;
        private QuestionData currentQuestion;

        void Start()
        {
            if (quizUI == null)
            {
                quizUI = FindObjectOfType<QuizUIController>();
            }

            if (quizUI != null)
            {
                // Subscribe to events
                quizUI.OnMcqAnswerSubmitted += HandleMcqAnswer;
                quizUI.OnNumericAnswerSubmitted += HandleNumericAnswer;
                quizUI.OnTimerExpired += HandleTimeout;
            }

            Debug.Log("=== QUIZ TESTER ===");
            Debug.Log("Press M = Show MCQ question");
            Debug.Log("Press N = Show Numeric question");
            Debug.Log("Press H = Hide quiz");
            Debug.Log("===================");
        }

        void Update()
        {
            if (quizUI == null) return;

            // M key = MCQ question
            if (Input.GetKeyDown(KeyCode.M))
            {
                ShowNextMcq();
            }

            // N key = Numeric question
            if (Input.GetKeyDown(KeyCode.N))
            {
                ShowNextNumeric();
            }

            // H key = Hide
            if (Input.GetKeyDown(KeyCode.H))
            {
                quizUI.HideQuiz();
                Debug.Log("Quiz hidden");
            }
        }

        void OnDestroy()
        {
            if (quizUI != null)
            {
                quizUI.OnMcqAnswerSubmitted -= HandleMcqAnswer;
                quizUI.OnNumericAnswerSubmitted -= HandleNumericAnswer;
                quizUI.OnTimerExpired -= HandleTimeout;
            }
        }

        private void ShowNextMcq()
        {
            if (mcqQuestions == null || mcqQuestions.Length == 0)
            {
                Debug.LogWarning("No MCQ questions assigned to QuizTester!");
                return;
            }

            currentQuestion = mcqQuestions[mcqIndex];
            quizUI.ShowMcqQuestion(mcqQuestions[mcqIndex], timeLimit);
            Debug.Log($"Showing MCQ [{mcqIndex + 1}/{mcqQuestions.Length}]: {mcqQuestions[mcqIndex].QuestionText}");

            mcqIndex = (mcqIndex + 1) % mcqQuestions.Length;
        }

        private void ShowNextNumeric()
        {
            if (numericQuestions == null || numericQuestions.Length == 0)
            {
                Debug.LogWarning("No Numeric questions assigned to QuizTester!");
                return;
            }

            currentQuestion = numericQuestions[numericIndex];
            quizUI.ShowNumericQuestion(numericQuestions[numericIndex], timeLimit);
            Debug.Log($"Showing Numeric [{numericIndex + 1}/{numericQuestions.Length}]: {numericQuestions[numericIndex].QuestionText}");

            numericIndex = (numericIndex + 1) % numericQuestions.Length;
        }

        private void HandleMcqAnswer(int choiceIndex, float responseTime)
        {
            var mcq = currentQuestion as McqQuestionData;
            if (mcq == null) return;

            bool correct = mcq.IsCorrect(choiceIndex);
            
            Debug.Log($"MCQ Answer: {mcq.Choices[choiceIndex]} ({(correct ? "CORRECT" : "WRONG")}) in {responseTime:F2}s");

            // Show visual feedback
            quizUI.ShowMcqResult(mcq.CorrectIndex, choiceIndex);
            quizUI.ShowFeedback(
                correct ? "Correct!" : $"Wrong! Answer: {mcq.CorrectAnswer}",
                correct
            );
        }

        private void HandleNumericAnswer(float value, float responseTime)
        {
            var numeric = currentQuestion as NumericQuestionData;
            if (numeric == null) return;

            float distance = numeric.GetDistance(value);
            bool exact = numeric.IsExact(value);

            Debug.Log($"Numeric Answer: {value} (Distance: {distance:F1}, Exact: {exact}) in {responseTime:F2}s");

            quizUI.ShowFeedback(
                exact ? "Exact!" : $"Distance: {distance:F1} (Answer: {numeric.CorrectValue})",
                exact
            );
        }

        private void HandleTimeout()
        {
            Debug.Log("TIME'S UP!");
            
            if (currentQuestion is McqQuestionData mcq)
            {
                quizUI.ShowMcqResult(mcq.CorrectIndex, -1);
                quizUI.ShowFeedback($"Time's up! Answer: {mcq.CorrectAnswer}", false);
            }
            else if (currentQuestion is NumericQuestionData numeric)
            {
                quizUI.ShowFeedback($"Time's up! Answer: {numeric.CorrectValue}", false);
            }
        }
    }
}
