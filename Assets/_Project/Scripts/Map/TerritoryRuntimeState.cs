using System;

namespace Conquiz.Map
{
    /// <summary>
    /// Runtime state for a territory during a match.
    /// Created from TerritoryStaticData at match start.
    /// This is a plain C# class (not MonoBehaviour) for easy serialization and testing.
    /// </summary>
    [Serializable]
    public class TerritoryRuntimeState
    {
        // --- State Fields ---

        private readonly TerritoryStaticData staticData;
        private int ownerPlayerId;
        private int defenseBonus;
        private int towersRemaining;

        // --- Constants ---

        /// <summary>
        /// Player ID indicating no owner (neutral territory).
        /// </summary>
        public const int NO_OWNER = -1;

        /// <summary>
        /// Defense bonus sequence for successful base defenses.
        /// Each successful defense grants diminishing bonus points.
        /// </summary>
        private static readonly int[] DefenseBonusSequence = { 6, 3, 2, 1, 1, 1 };

        // --- Constructor ---

        /// <summary>
        /// Creates runtime state from static data.
        /// Initializes to neutral ownership with full towers (if base).
        /// </summary>
        /// <param name="staticData">The immutable territory configuration</param>
        public TerritoryRuntimeState(TerritoryStaticData staticData)
        {
            this.staticData = staticData ?? throw new ArgumentNullException(nameof(staticData));
            
            // Initialize to neutral
            ownerPlayerId = NO_OWNER;
            defenseBonus = 0;
            
            // Bases start with full towers
            towersRemaining = staticData.IsBase ? staticData.BaseTowers : 0;
        }

        // --- Public Properties ---

        /// <summary>
        /// Reference to the immutable static data.
        /// </summary>
        public TerritoryStaticData StaticData => staticData;

        /// <summary>
        /// Shortcut to territory ID.
        /// </summary>
        public string Id => staticData.Id;

        /// <summary>
        /// Shortcut to display name.
        /// </summary>
        public string DisplayName => staticData.DisplayName;

        /// <summary>
        /// Shortcut to check if this is a base.
        /// </summary>
        public bool IsBase => staticData.IsBase;

        /// <summary>
        /// Player ID of current owner (-1 if neutral).
        /// </summary>
        public int OwnerPlayerId => ownerPlayerId;

        /// <summary>
        /// True if territory has no owner.
        /// </summary>
        public bool IsNeutral => ownerPlayerId == NO_OWNER;

        /// <summary>
        /// Accumulated defense bonus points from successful base defenses.
        /// Only applies to bases; resets on capture.
        /// </summary>
        public int DefenseBonus => defenseBonus;

        /// <summary>
        /// Remaining towers (only for bases, 0 for normal territories).
        /// When this reaches 0, base can be captured.
        /// </summary>
        public int TowersRemaining => towersRemaining;

        /// <summary>
        /// Total points this territory is worth (base points + defense bonus).
        /// </summary>
        public int TotalPoints => staticData.Points + defenseBonus;

        /// <summary>
        /// Number of successful defenses (for defense bonus calculation).
        /// </summary>
        private int successfulDefenses;

        // --- State Modification Methods ---

        /// <summary>
        /// Sets the owner of this territory.
        /// Use for initial base selection or settlement draft.
        /// </summary>
        /// <param name="playerId">Player ID to assign ownership</param>
        public void SetOwner(int playerId)
        {
            ownerPlayerId = playerId;
        }

        /// <summary>
        /// Captures this territory for a new owner.
        /// For normal territories: immediate transfer.
        /// For bases: only call after all towers destroyed.
        /// </summary>
        /// <param name="newOwnerId">The attacking player's ID</param>
        public void Capture(int newOwnerId)
        {
            ownerPlayerId = newOwnerId;
            
            // Defense bonus does NOT transfer on capture
            defenseBonus = 0;
            successfulDefenses = 0;
            
            // If capturing a base, towers stay at 0 (already destroyed)
        }

        /// <summary>
        /// Destroys one tower on this base.
        /// Only valid for bases with towers remaining.
        /// </summary>
        /// <returns>True if tower was destroyed, false if no towers left</returns>
        public bool DestroyTower()
        {
            if (!staticData.IsBase || towersRemaining <= 0)
                return false;

            towersRemaining--;
            return true;
        }

        /// <summary>
        /// Records a successful base defense.
        /// Adds diminishing defense bonus points.
        /// </summary>
        public void RecordSuccessfulDefense()
        {
            if (!staticData.IsBase)
                return;

            // Get bonus from sequence (caps at last value)
            int bonusIndex = Math.Min(successfulDefenses, DefenseBonusSequence.Length - 1);
            defenseBonus += DefenseBonusSequence[bonusIndex];
            successfulDefenses++;
        }

        /// <summary>
        /// Checks if this territory is owned by a specific player.
        /// </summary>
        /// <param name="playerId">Player ID to check</param>
        /// <returns>True if owned by that player</returns>
        public bool IsOwnedBy(int playerId)
        {
            return ownerPlayerId == playerId;
        }

        /// <summary>
        /// Checks if this base can be captured (all towers destroyed).
        /// </summary>
        /// <returns>True if base has no towers remaining</returns>
        public bool CanBaseBeCaptured()
        {
            return staticData.IsBase && towersRemaining <= 0;
        }

        // --- Debug ---

        public override string ToString()
        {
            string ownerStr = IsNeutral ? "Neutral" : $"Player {ownerPlayerId}";
            string baseStr = IsBase ? $" [Base: {towersRemaining} towers]" : "";
            return $"{DisplayName} ({ownerStr}){baseStr}";
        }
    }
}
