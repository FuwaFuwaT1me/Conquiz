using UnityEngine;

namespace Conquiz.Quiz
{
    /// <summary>
    /// Base class for all question types.
    /// Inherit from this to create specific question formats (MCQ, Numeric, etc.).
    /// </summary>
    public abstract class QuestionData : ScriptableObject
    {
        [Header("Question Content")]
        [TextArea(2, 5)]
        [Tooltip("The question text displayed to players")]
        [SerializeField] protected string questionText;

        [Header("Metadata")]
        [Tooltip("Optional category for filtering/organizing questions")]
        [SerializeField] protected string category;

        [Tooltip("Difficulty level (1 = easy, 5 = hard)")]
        [Range(1, 5)]
        [SerializeField] protected int difficulty = 1;

        /// <summary>
        /// The question text to display.
        /// </summary>
        public string QuestionText => questionText;

        /// <summary>
        /// Optional category for organizing questions.
        /// </summary>
        public string Category => category;

        /// <summary>
        /// Difficulty rating from 1 (easy) to 5 (hard).
        /// </summary>
        public int Difficulty => difficulty;

        /// <summary>
        /// Returns the type of question (MCQ, Numeric, etc.).
        /// Override in derived classes.
        /// </summary>
        public abstract QuestionType Type { get; }

        /// <summary>
        /// Validates that the question data is properly configured.
        /// Override in derived classes to add specific validation.
        /// </summary>
        public virtual bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(questionText);
        }
    }

    /// <summary>
    /// Enum defining available question types.
    /// </summary>
    public enum QuestionType
    {
        MultipleChoice,
        Numeric
    }
}
