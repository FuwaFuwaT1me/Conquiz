using UnityEngine;

namespace Conquiz.Quiz
{
    /// <summary>
    /// Numeric Question data.
    /// Player must input a number; closest to correct value wins.
    /// If both players are exact, faster response time wins.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewNumericQuestion",
        menuName = "Conquiz/Questions/Numeric Question",
        order = 2)]
    public class NumericQuestionData : QuestionData
    {
        [Header("Correct Answer")]
        [Tooltip("The exact correct numeric value")]
        [SerializeField] private float correctValue;

        [Tooltip("Unit of measurement (e.g., 'km', 'years', '%')")]
        [SerializeField] private string unit;

        [Header("Input Constraints")]
        [Tooltip("Minimum allowed input value")]
        [SerializeField] private float allowedRangeMin = 0f;

        [Tooltip("Maximum allowed input value")]
        [SerializeField] private float allowedRangeMax = 1000000f;

        [Tooltip("Number of decimal places allowed (0 = integers only)")]
        [Range(0, 4)]
        [SerializeField] private int decimalPlaces = 0;

        /// <summary>
        /// The exact correct numeric answer.
        /// </summary>
        public float CorrectValue => correctValue;

        /// <summary>
        /// Unit of measurement for display (e.g., "km", "years").
        /// </summary>
        public string Unit => unit;

        /// <summary>
        /// Minimum value players can input.
        /// </summary>
        public float AllowedRangeMin => allowedRangeMin;

        /// <summary>
        /// Maximum value players can input.
        /// </summary>
        public float AllowedRangeMax => allowedRangeMax;

        /// <summary>
        /// Number of decimal places allowed in player input.
        /// </summary>
        public int DecimalPlaces => decimalPlaces;

        public override QuestionType Type => QuestionType.Numeric;

        /// <summary>
        /// Calculates how close a player's answer is to the correct value.
        /// Lower values are better (0 = exact match).
        /// </summary>
        /// <param name="playerAnswer">The player's numeric input</param>
        /// <returns>Absolute difference from correct value</returns>
        public float GetDistance(float playerAnswer)
        {
            return Mathf.Abs(playerAnswer - correctValue);
        }

        /// <summary>
        /// Checks if a player's answer is exactly correct.
        /// Uses small epsilon for floating-point comparison.
        /// </summary>
        /// <param name="playerAnswer">The player's numeric input</param>
        /// <returns>True if answer matches exactly</returns>
        public bool IsExact(float playerAnswer)
        {
            // Use appropriate epsilon based on decimal places
            float epsilon = decimalPlaces == 0 ? 0.5f : Mathf.Pow(10, -decimalPlaces) * 0.5f;
            return Mathf.Abs(playerAnswer - correctValue) < epsilon;
        }

        /// <summary>
        /// Compares two player answers and determines the winner.
        /// </summary>
        /// <param name="answer1">First player's answer</param>
        /// <param name="answer2">Second player's answer</param>
        /// <returns>
        /// -1 if player 1 is closer,
        ///  1 if player 2 is closer,
        ///  0 if tied (both same distance)
        /// </returns>
        public int CompareAnswers(float answer1, float answer2)
        {
            float dist1 = GetDistance(answer1);
            float dist2 = GetDistance(answer2);

            if (dist1 < dist2) return -1;
            if (dist2 < dist1) return 1;
            return 0; // Tie - caller should use response time as tiebreaker
        }

        /// <summary>
        /// Checks if a value is within the allowed input range.
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>True if within range</returns>
        public bool IsInAllowedRange(float value)
        {
            return value >= allowedRangeMin && value <= allowedRangeMax;
        }

        /// <summary>
        /// Clamps a value to the allowed input range.
        /// </summary>
        /// <param name="value">Value to clamp</param>
        /// <returns>Clamped value</returns>
        public float ClampToAllowedRange(float value)
        {
            return Mathf.Clamp(value, allowedRangeMin, allowedRangeMax);
        }

        /// <summary>
        /// Validates Numeric question requirements:
        /// - Range min must be less than max
        /// - Correct value must be within range
        /// </summary>
        public override bool IsValid()
        {
            if (!base.IsValid())
                return false;

            // Range must be valid
            if (allowedRangeMin >= allowedRangeMax)
                return false;

            // Correct value must be within range
            if (correctValue < allowedRangeMin || correctValue > allowedRangeMax)
                return false;

            return true;
        }

        /// <summary>
        /// Editor validation to ensure valid range.
        /// </summary>
        private void OnValidate()
        {
            // Ensure min is less than max
            if (allowedRangeMin > allowedRangeMax)
            {
                allowedRangeMin = allowedRangeMax;
            }

            // Clamp correct value to range
            correctValue = Mathf.Clamp(correctValue, allowedRangeMin, allowedRangeMax);
        }
    }
}
