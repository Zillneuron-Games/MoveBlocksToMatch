namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>Describes which local save slot to select after authentication changes.</summary>
    public class PlayerIdentityChange
    {
        public string PreviousPlayerId
        {
            get;
        }

        public string CurrentPlayerId
        {
            get;
        }

        public bool DiscardPreviousGuestProgress
        {
            get;
        }

        public bool CurrentIsGuest
        {
            get;
        }

        public PlayerIdentityChange(string previousPlayerId, string currentPlayerId, bool discardPreviousGuestProgress, bool currentIsGuest)
        {
            PreviousPlayerId = previousPlayerId;
            CurrentPlayerId = currentPlayerId;
            DiscardPreviousGuestProgress = discardPreviousGuestProgress;
            CurrentIsGuest = currentIsGuest;
        }
    }
}
