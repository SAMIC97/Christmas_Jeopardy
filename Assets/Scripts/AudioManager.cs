using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance; // Singleton for global access

    [Header("Audio Sources")]
    public AudioSource musicSource;      // For background music
    public AudioSource sfxSource;        // For sound effects
    public AudioSource tickingSource;

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;    // Music for the game
    public AudioClip mainMenuMusic;      // Music while being in the main menu
    public AudioClip winnerMusic;        // Music while being in the main menu
    public AudioClip buttonClickSFX;     // Sound effect for button clicks
    public AudioClip categoryClickSFX;     // Sound effect for button clicks
    public AudioClip correctAnswerSFX;   // Sound effect for correct answers
    public AudioClip wrongAnswerSFX;     // Sound effect for wrong answers
    public AudioClip timeTickingSFX;      // Sound effect when the timer is counting
    public AudioClip grichLaughSFX;      // Sound effect when the timer is counting

    private void Awake()
    {
        // Singleton pattern to ensure a single instance
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Play background music at the start
        PlayMusic(mainMenuMusic);
    }

    // Play background music
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource != null && clip != null)
        {
            musicSource.clip = clip;
            musicSource.loop = true; // Ensure the music loops
            musicSource.Play();
        }
    }

    // Play a sound effect
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip); // Play the sound without interrupting others
        }
    }

    public void StartTickingSound()
    {
        if (tickingSource != null && timeTickingSFX != null)
        {
            tickingSource.clip = timeTickingSFX;
            tickingSource.loop = true; // Loop the ticking sound
            tickingSource.Play();
        }
    }

    public void StopTickingSound()
    {
        if (tickingSource != null)
        {
            tickingSource.Stop(); // Stop the ticking sound
        }
    }
}
