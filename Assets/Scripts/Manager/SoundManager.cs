using UnityEngine;
using Zillneuron.Utilities;
using Zillneuron.Blockmates.Services.SignIn;

public class SoundManager :MonoSingleton<SoundManager>
{
    private bool soundOn;
    private bool musicOn;
    private AccountService accountService;

    [SerializeField]
    private AudioSource moveSound;
    [SerializeField]
    private AudioSource buttonSound;
    [SerializeField]
    private AudioSource backgroundMusic;

    public bool SoundOn { get { return soundOn; } }
    public bool MusicOn { get { return musicOn; } }

    protected override void OnInstanceCreated()
    {
        base.OnInstanceCreated();

        accountService = AccountService.Instance;
        accountService.OnSettingsChanged += ApplySettings;
        soundOn = accountService.SoundEnabled;
        musicOn = accountService.MusicEnabled;
    }

    private void Start()
    {
        if (!musicOn)
        {
            backgroundMusic.Pause();
        }
    }

    private void OnDestroy()
    {
        if (accountService != null)
        {
            accountService.OnSettingsChanged -= ApplySettings;
        }
    }

    public void SwitchSound(bool on)
    {
        accountService.SetSoundEnabled(on);
    }

    public void SwitchMusic(bool on)
    {
        accountService.SetMusicEnabled(on);
    }

    private void ApplySettings(SettingsData settings)
    {
        soundOn = settings.SoundEnabled;
        if (musicOn == settings.MusicEnabled)
        {
            return;
        }

        musicOn = settings.MusicEnabled;
        if (musicOn)
        {
            backgroundMusic.Play();
        }
        else
        {
            backgroundMusic.Pause();
        }
    }

    public void PlayMoveSound()
    {
        if (!soundOn)
        {
            return;
        }

        moveSound.Play();
    }

    public void StopMoveSound()
    {
        if (!soundOn)
        {
            return;
        }

        moveSound.Stop();
    }

    public void PlayButtonSound()
    {
        if (!soundOn)
        {
            return;
        }

        buttonSound.Play();
    }
}
