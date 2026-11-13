using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Zillneuron.Utilities;
using Zillneuron.Blockmates.Services.SignIn;

public class LocalizationManager : MonoSingleton<LocalizationManager>
{
    private const string TranslationFilePathTemplate = "Translations/{0}.json";
    private Dictionary<string, string> translations;

    public ELanguageCode LanguageCode { get {  return AccountService.Instance.LanguageCode; }  }
    public async Task Initialize(ELanguageCode languageCode)
    {
        var loadedTranslations = new Dictionary<string, string>();
        string path = Path.Combine(Application.streamingAssetsPath, string.Format(TranslationFilePathTemplate, languageCode));
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android packages StreamingAssets inside the APK.
        using var request = UnityWebRequest.Get(path);
        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            throw new IOException("Could not load the translation file: " + request.error);
        }
        string translationJson = request.downloadHandler.text;
#else
        string translationJson = await File.ReadAllTextAsync(path);
#endif
        TranslationData translationData = JsonUtility.FromJson<TranslationData>(translationJson);

        for (int i = 0; i < translationData.Phrases.Length; i++)
        {
            loadedTranslations.Add(translationData.Phrases[i].Key, translationData.Phrases[i].Value);
        }

        translations = loadedTranslations;
    }

    public string GetPhrase(string phraseKey)
    {
        return translations[phraseKey];
    }
}
