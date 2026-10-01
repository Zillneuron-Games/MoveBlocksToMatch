using TMPro;
using UnityEngine;
using Zillneuron.UILayout;

public class MainMenuView : ACompleteView
{
    private Canvas canvas;

    [SerializeField]
    private PurchaseManager purchaseManager;

    [SerializeField]
    private TMP_Text playText;

    [SerializeField]
    private TMP_Text levelText;
    
    protected override void OnInitialize()
    {
        base.OnInitialize();

        canvas = GetComponentInParent<Canvas>(true);

        SoundManager.Instance.SwitchMusic(SoundManager.Instance.MusicOn);
    }

    protected override void OnLaunch()
    {
        base.OnLaunch();

        canvas.enabled = true;
    }

    protected override void OnFinish()
    {
        base.OnFinish();

        canvas.enabled = false;
    }

    protected override void SetTexts()
    {
        base.SetTexts();

        playText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.Play);
        levelText.text = $"{LocalizationManager.Instance.GetPhrase(PhraseCollection.Level)} {DataReader.GetNextGameId()}"; 
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
        SettingsDialogParameters parameters = new SettingsDialogParameters(
            () => { ViewContext.Instance.HideDialog<SettingsDialog>(); });

        ViewContext.Instance.OpenDialog<SettingsDialog>(parameters); 
    }

    public void Shop_ButtonHandler()
    {
        ADialogParameters parameters = ShopDialog.CreateDialogParameters(purchaseManager);
        ViewContext.Instance.OpenDialog<ShopDialog>(parameters);
    }

    public void RemoveAds_ButtonHandler()
    {
        ADialogParameters parameters = ShopDialog.CreateDialogParameters(purchaseManager);
        ViewContext.Instance.OpenDialog<ShopDialog>(parameters);
    }
}
