using System;
using UnityEngine;
using Zillneuron.Utilities;
using Zillneuron.Blockmates.Services.SignIn;

public class SoundManager :MonoSingleton<SoundManager>
{
    private bool soundOn;
    private bool musicOn;

    [SerializeField]
    private AudioSource moveSound;
    [SerializeField]
    private AudioSource buttonSound;
    [SerializeField]
    private AudioSource backgroundMusic;

    public bool SoundOn { get { return soundOn; } }
    public bool MusicOn { get { return musicOn; } }

    protected override void Awake()
    {
        base.Awake();

        soundOn = AccountService.Instance.SoundEnabled;
        musicOn = AccountService.Instance.MusicEnabled;
    }

    public void SwitchSound(bool on)
    {
        AccountService.Instance.SetSoundEnabled(on);
        soundOn = on;
    }

    public void SwitchMusic(bool on)
    {
        AccountService.Instance.SetMusicEnabled(on);
        musicOn = on;

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