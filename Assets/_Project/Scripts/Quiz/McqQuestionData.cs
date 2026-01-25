using UnityEngine;

namespace Conquiz.Quiz
{
    /// <summary>
    /// Multiple Choice Question data.
    /// Contains 4 answer choices with one correct answer.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewMcqQuestion",
        menuName = "Conquiz/Questions/MCQ Question",
        order = 1)]
    public class McqQuestionData : QuestionData
    {
        [Header("Answer Choices")]
        [Tooltip("Four possible answers (one must be correct)")]
        [SerializeField] private string[] choices = new string[4];

        [Tooltip("Index of the correct answer (0-3)")]
        [Range(0, 3)]
        [SerializeField] private int correctIndex;

        /// <summary>
        /// The four answer choices.
        /// </summary>
        public string[] Choices => choices;

        /// <summary>
        /// Index of the correct answer (0-3).
        /// </summary>
        public int CorrectIndex => correctIndex;

        /// <summary>
        /// Returns the correct answer text.
        /// </summary>
        public string CorrectAnswer => 
            (choices != null && correctIndex >= 0 && correctIndex < choices.Length) 
                ? choices[correctIndex] 
                : string.Empty;

        public override QuestionType Type => QuestionType.MultipleChoice;

        /// <summary>
        /// Checks if a given answer index is correct.
        /// </summary>
        /// <param name="answerIndex">The index chosen by the player (0-3)</param>
        /// <returns>True if the answer is correct</returns>
        public bool IsCorrect(int answerIndex)
        {
            return answerIndex == correctIndex;
        }

        /// <summary>
        /// Validates MCQ-specific requirements:
        /// - Must have exactly 4 non-empty choices
        /// - Correct index must be valid
        /// </summary>
        public override bool IsValid()
        {
            if (!base.IsValid())
                return false;

            // Must have exactly 4 choices
            if (choices == null || choices.Length != 4)
                return false;

            // All choices must be non-empty
            foreach (var choice in choices)
            {
                if (string.IsNullOrWhiteSpace(choice))
                    return false;
            }

            // Correct index must be in valid range
            if (correctIndex < 0 || correctIndex >= choices.Length)
                return false;

            return true;
        }

        /// <summary>
        /// Editor validation to ensure 4 choices array.
        /// </summary>
        private void OnValidate()
        {
            // Ensure choices array is always length 4
            if (choices == null || choices.Length != 4)
            {
                var newChoices = new string[4];
                if (choices != null)
                {
                    for (int i = 0; i < Mathf.Min(choices.Length, 4); i++)
                    {
                        newChoices[i] = choices[i];
                    }
                }
                choices = newChoices;
            }
        }
    }
}
