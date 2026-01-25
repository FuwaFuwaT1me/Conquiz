using UnityEngine;

namespace Conquiz.Map
{
    /// <summary>
    /// Static/immutable data for a territory.
    /// Configured in Editor, never changes during gameplay.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewTerritory",
        menuName = "Conquiz/Map/Territory Data",
        order = 1)]
    public class TerritoryStaticData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this territory")]
        [SerializeField] private string id;

        [Tooltip("Display name shown to players")]
        [SerializeField] private string displayName;

        [Header("Scoring")]
        [Tooltip("Points this territory contributes to final score")]
        [Min(1)]
        [SerializeField] private int points = 1;

        [Header("Base Configuration")]
        [Tooltip("Is this territory a player's base?")]
        [SerializeField] private bool isBase;

        [Tooltip("Number of towers if this is a base (ignored if not base)")]
        [Range(1, 5)]
        [SerializeField] private int baseTowers = 3;

        [Header("Map Position (Optional)")]
        [Tooltip("Grid coordinates for hex map placement")]
        [SerializeField] private Vector2Int gridPosition;

        // --- Public Properties ---

        /// <summary>
        /// Unique identifier for this territory.
        /// </summary>
        public string Id => id;

        /// <summary>
        /// Human-readable name for UI display.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Points awarded at end of match for owning this territory.
        /// </summary>
        public int Points => points;

        /// <summary>
        /// True if this territory is a player's starting base.
        /// Bases have towers and special capture rules.
        /// </summary>
        public bool IsBase => isBase;

        /// <summary>
        /// Number of towers this base starts with (only relevant if IsBase).
        /// </summary>
        public int BaseTowers => isBase ? baseTowers : 0;

        /// <summary>
        /// Grid position for map layout.
        /// </summary>
        public Vector2Int GridPosition => gridPosition;

        /// <summary>
        /// Creates a new runtime state initialized from this static data.
        /// Call this at match start for each territory.
        /// </summary>
        /// <returns>Fresh runtime state ready for gameplay</returns>
        public TerritoryRuntimeState CreateRuntimeState()
        {
            return new TerritoryRuntimeState(this);
        }

        /// <summary>
        /// Validates the territory configuration.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            if (points < 1)
                return false;

            if (isBase && baseTowers < 1)
                return false;

            return true;
        }

        /// <summary>
        /// Auto-generate ID from asset name if empty.
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = name.Replace(" ", "_").ToLower();
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }
        }
    }
}
