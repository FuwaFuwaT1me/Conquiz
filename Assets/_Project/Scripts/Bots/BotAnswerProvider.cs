using System;
using System.Collections;
using UnityEngine;
using Conquiz.Quiz;

namespace Conquiz.Bots
{
    /// <summary>
    /// Bot implementation of IAnswerProvider.
    /// Simulates thinking time and provides answers with configurable accuracy.
    /// </summary>
    public class BotAnswerProvider : MonoBehaviour, IAnswerProvider
    {
        [Header("Bot Identity")]
        [SerializeField] private int playerId = 1;

        [Header("Bot Difficulty")]
        [Tooltip("Chance of answering MCQ correctly (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float mcqAccuracy = 0.6f;

        [Tooltip("How close to the correct numeric answer (0 = perfect, higher = worse)")]
        [Min(0f)]
        [SerializeField] private float numericErrorRange = 50f;

        [Tooltip("Chance of getting exact numeric answer when errorRange allows")]
        [Range(0f, 1f)]
        [SerializeField] private float numericExactChance = 0.3f;

        [Header("Response Time")]
        [Tooltip("Minimum thinking time in seconds")]
        [Min(0.1f)]
        [SerializeField] private float minResponseTime = 1f;

        [Tooltip("Maximum thinking time in seconds")]
        [Min(0.5f)]
        [SerializeField] private float maxResponseTime = 8f;

        private Coroutine pendingAnswerCoroutine;

        public int PlayerId => playerId;

        /// <summary>
        /// Sets the bot's player ID at runtime.
        /// </summary>
        public void SetPlayerId(int id)
        {
            playerId = id;
        }

        /// <summary>
        /// Configures bot difficulty.
        /// </summary>
        /// <param name="mcqAcc">MCQ accuracy (0-1)</param>
        /// <param name="numericError">Numeric error range</param>
        /// <param name="exactChance">Chance of exact numeric answer</param>
        public void SetDifficulty(float mcqAcc, float numericError, float exactChance)
        {
            mcqAccuracy = Mathf.Clamp01(mcqAcc);
            numericErrorRange = Mathf.Max(0, numericError);
            numericExactChance = Mathf.Clamp01(exactChance);
        }

        public void RequestMcqAnswer(
            McqQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady)
        {
            CancelPendingRequest();
            pendingAnswerCoroutine = StartCoroutine(
                GenerateMcqAnswerCoroutine(question, timeLimit, onAnswerReady));
        }

        public void RequestNumericAnswer(
            NumericQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady)
        {
            CancelPendingRequest();
            pendingAnswerCoroutine = StartCoroutine(
                GenerateNumericAnswerCoroutine(question, timeLimit, onAnswerReady));
        }

        public void CancelPendingRequest()
        {
            if (pendingAnswerCoroutine != null)
            {
                StopCoroutine(pendingAnswerCoroutine);
                pendingAnswerCoroutine = null;
            }
        }

        private IEnumerator GenerateMcqAnswerCoroutine(
            McqQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady)
        {
            // Simulate thinking time
            float thinkTime = UnityEngine.Random.Range(minResponseTime, Mathf.Min(maxResponseTime, timeLimit - 0.5f));
            thinkTime = Mathf.Max(0.5f, thinkTime);

            yield return new WaitForSeconds(thinkTime);

            // Decide if bot answers correctly
            bool answerCorrectly = UnityEngine.Random.value < mcqAccuracy;

            int chosenIndex;
            if (answerCorrectly)
            {
                chosenIndex = question.CorrectIndex;
            }
            else
            {
                // Pick a random wrong answer
                do
                {
                    chosenIndex = UnityEngine.Random.Range(0, 4);
                } while (chosenIndex == question.CorrectIndex);
            }

            bool isCorrect = question.IsCorrect(chosenIndex);

            var result = QuizAnswerResult.CreateMcqAnswer(
                playerId,
                chosenIndex,
                thinkTime,
                isCorrect);

            pendingAnswerCoroutine = null;
            onAnswerReady?.Invoke(result);
        }

        private IEnumerator GenerateNumericAnswerCoroutine(
            NumericQuestionData question,
            float timeLimit,
            Action<QuizAnswerResult> onAnswerReady)
        {
            // Simulate thinking time
            float thinkTime = UnityEngine.Random.Range(minResponseTime, Mathf.Min(maxResponseTime, timeLimit - 0.5f));
            thinkTime = Mathf.Max(0.5f, thinkTime);

            yield return new WaitForSeconds(thinkTime);

            // Generate answer
            float botAnswer;
            bool tryExact = UnityEngine.Random.value < numericExactChance;

            if (tryExact || numericErrorRange < 0.01f)
            {
                // Exact answer
                botAnswer = question.CorrectValue;
            }
            else
            {
                // Add random error
                float error = UnityEngine.Random.Range(-numericErrorRange, numericErrorRange);
                botAnswer = question.CorrectValue + error;

                // Clamp to allowed range
                botAnswer = question.ClampToAllowedRange(botAnswer);

                // Round based on decimal places
                if (question.DecimalPlaces == 0)
                {
                    botAnswer = Mathf.Round(botAnswer);
                }
                else
                {
                    float multiplier = Mathf.Pow(10, question.DecimalPlaces);
                    botAnswer = Mathf.Round(botAnswer * multiplier) / multiplier;
                }
            }

            float distance = question.GetDistance(botAnswer);
            bool isExact = question.IsExact(botAnswer);

            var result = QuizAnswerResult.CreateNumericAnswer(
                playerId,
                botAnswer,
                thinkTime,
                distance,
                isExact);

            pendingAnswerCoroutine = null;
            onAnswerReady?.Invoke(result);
        }

        void OnDisable()
        {
            CancelPendingRequest();
        }
    }
}
