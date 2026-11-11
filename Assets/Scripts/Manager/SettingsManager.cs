using UnityEngine;
using Zillneuron.Utilities;

public sealed class SettingsManager : MonoSingleton<SettingsManager>
{
    [SerializeField]
    private GlobalSettings globalSettings;

    public GlobalSettings Settings
    {
        get
        {
            if (globalSettings == null)
            {
                throw new System.InvalidOperationException("Assign GlobalSettings to the scene's SettingsManager.");
            }

            return globalSettings;
        }
    }
}
