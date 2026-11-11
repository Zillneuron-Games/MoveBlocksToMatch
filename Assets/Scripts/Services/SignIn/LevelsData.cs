using System;

namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>JSON envelope for saved level progress.</summary>
    [Serializable]
    public class LevelsData
    {
        public GameDataDynamic[] Levels;
    }
}
