namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>Level progress and the write lock returned by Cloud Save.</summary>
    public class LevelsCloudSnapshot
    {
        public GameDataDynamic[] Levels
        {
            get;
        }

        public string WriteLock
        {
            get;
        }

        public bool Exists
        {
            get;
        }

        public LevelsCloudSnapshot(GameDataDynamic[] levels, string writeLock, bool exists)
        {
            Levels = levels;
            WriteLock = writeLock;
            Exists = exists;
        }
    }
}
