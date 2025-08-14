using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Zillneuron.Utilities;

public class LocalizationManager : MonoSingleton<LocalizationManager>
{
    private const string TranslationFilePathTemplate = "Translations/{0}.json";
    private Dictionary<string, string> translations;

    public async Task Initialize(ELanguageCode languageCode)
    {
        translations = new Dictionary<string, string>();

        string translationJson = await File.ReadAllTextAsync(Path.Combine(Application.streamingAssetsPath, string.Format(TranslationFilePathTemplate, languageCode)));
        TranslationData translationData = JsonUtility.FromJson<TranslationData>(translationJson);

        for (int i = 0; i < translationData.Phrases.Length; i++)
        {
            translations.Add(translationData.Phrases[i].Key, translationData.Phrases[i].Value);
        }
    }

    public string GetPhrase(string phraseKey)
    {
        return translations[phraseKey];
    }
}
