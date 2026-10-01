using System;
using UnityEngine;
using Zillneuron.Utilities;

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

        soundOn = DataReader.GetSound();
        musicOn = DataReader.GetMusic();
    }

    public void SwitchSound(bool on)
    {
        DataReader.SetSound(on);
        soundOn = on;
    }

    public void SwitchMusic(bool on)
    {
        DataReader.SetMusic(on);
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