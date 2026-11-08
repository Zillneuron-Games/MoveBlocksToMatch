using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zillneuron.UILayout;

public class SettingsDialog : ADialog
{
    private bool soundOn;
    private bool musicOn;
    private ELanguageCode languageCode;
    private Action onClickClose;

    [SerializeField]
    private GameObject logInButton;

    [SerializeField]
    private GameObject logOutButton;

    [SerializeField]
    private ToggleButton soundButton;

    [SerializeField]
    private ToggleButton musicButton;

    [SerializeField]
    private Sprite[] languageFlags;

    [SerializeField]
    private Image languageFlagImage;

    [SerializeField]
    private Button languageButton;

    [SerializeField]
    private GameObject signInButton;

    [SerializeField]
    private GameObject signOutButton;

    [SerializeField]
    private TMP_Text soundText;

    [SerializeField]
    private TMP_Text musicText;

    [SerializeField]
    private TMP_Text languageText;

    [SerializeField]
    private TMP_Text aboutUsText;    

    [SerializeField]
    private TMP_Text contactUsText;

    [SerializeField]
    private TMP_Text helpText;

    [SerializeField]
    private TMP_Text termsAndConditionsText;

    [SerializeField]
    private TMP_Text privacyPolicyText;

    [SerializeField]
    private TMP_Text signInText;

    [SerializeField]
    private TMP_Text signOutText;
    
    public static ADialogParameters CreateDialogParameters(Action onClickClose)
    {
        return new SettingsDialogParameters(onClickClose);
    }

    public override void SetUp(ADialogParameters parameters)
    {
        SettingsDialogParameters currentParameters = parameters as SettingsDialogParameters;

        onClickClose = currentParameters.OnClickClose;
        soundOn = SoundManager.Instance.SoundOn;
        musicOn = SoundManager.Instance.MusicOn;
        languageCode = LocalizationManager.Instance.LanguageCode;

        soundButton.Switch(soundOn);
        musicButton.Switch(musicOn);
        languageFlagImage.sprite = languageFlags[(int)languageCode];
    }

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

        soundText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.Sound);
        musicText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.Music);
        languageText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.Language);
        aboutUsText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.AboutUs);
        contactUsText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.ContactUs);
        helpText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.Help);
        termsAndConditionsText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.TermsAndConditions);
        privacyPolicyText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.PrivacyPolicy);
        signInText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.SignIn);
        signOutText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.SignOut);

        CheckIsGuest();
    }

    private void CheckIsGuest()
    {
        logInButton.SetActive(AccountService.Instance.IsGuest);
        logOutButton.SetActive(AccountService.Instance.IsLinked);
    }

    public void OnClick_Sound()
    {
        soundOn = !soundOn;
        SoundManager.Instance.SwitchSound(soundOn);
        soundButton.Switch(soundOn);
    }

    public void OnClick_Music()
    {
        musicOn = !musicOn;
        SoundManager.Instance.SwitchMusic(musicOn);
        musicButton.Switch(musicOn);
    }

    public void OnClick_Language()
    {
        ChangeToNextLanguage();
    }

    public void OnClick_AboutUs()
    {
       
    }

    public void OnClick_ContactUs()
    {
        
    }

    public void OnClick_Help()
    {
        
    }

    public void OnClick_Notification()
    {
        
    }

    public void OnClick_TermsAndConditions()
    {
        
    }

    public void OnClick_PrivacyPolicy()
    {
        
    }

    public void OnClick_SignIn()
    {
        SignIn();
    }

    private async void SignIn()
    {
        LoginOutcome result = await AccountService.Instance.LogInAsync(confirmReplaceGuestProgress: ConfirmGuestLossAsync);

        CheckIsGuest();
    }

    private async Task<bool> ConfirmGuestLossAsync()
    {
        return true;
    }

    public void OnClick_SignOut()
    {
        SignOut();
    }

    private async void SignOut()
    {
        await AccountService.Instance.LogOutAsync();

        CheckIsGuest();
    }

    public void OnClick_Close()
    {
        onClickClose();
    }

    private async void ChangeToNextLanguage()
    {
        languageButton.interactable = false;

        AccountService.Instance.SetLanguage(languageCode.Next());
        languageCode = AccountService.Instance.LanguageCode;

        languageFlagImage.sprite = languageFlags[(int)languageCode];

        await LocalizationManager.Instance.Initialize(languageCode);

        languageButton.interactable = true;

        UpdateViewElements();
    }
}
