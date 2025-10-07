using TMPro;
using UnityEngine;
using Zillneuron.UILayout;

public class MainMenuView : ACompleteView
{
    [SerializeField]
    private TMP_Text playText;

    [SerializeField]
    private TMP_Text levelText;

    protected override void OnLaunch()
    {
        base.OnLaunch();

        gameObject.SetActive(true);
    }

    protected override void OnFinish()
    {
        base.OnFinish();

        gameObject.SetActive(false);
    }

    protected override void SetTexts()
    {
        base.SetTexts();

        playText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.Play);
        levelText.text = $"{LocalizationManager.Instance.GetPhrase(PhraseCollection.Level)} {DataReader.GetNextGameId().ToString()}"; 
    }

    public void Play_ButtonHandler()
    {
        ChangeActivityToNext<GameplayView>();
    }

    public void Levels_ButtonHandler()
    {
        ChangeActivityToNext<LevelsView>();
    }

    public void Settings_ButtonHandler()
    {
        SettingsDialogParameters parameters = new SettingsDialogParameters();
        ViewContext.Instance.OpenDialog<SettingsDialog>(parameters); 
    }

    public void Shop_ButtonHandler()
    {
        ShopDialogParameters parameters = new ShopDialogParameters();
        ViewContext.Instance.OpenDialog<ShopDialog>(parameters);
    }

    public void RemoveAds_ButtonHandler()
    {
        ShopDialogParameters parameters = new ShopDialogParameters();
        ViewContext.Instance.OpenDialog<ShopDialog>(parameters);
    }
}
