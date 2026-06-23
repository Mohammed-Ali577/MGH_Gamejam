using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Awake()
    {
        if (audioMixer == null) Debug.LogError("AudioMixer not assigned on " + name);
        if (musicSlider == null) Debug.LogError("musicSlider not assigned on " + name);
        if (sfxSlider == null) Debug.LogError("sfxSlider not assigned on " + name);
    }

    private void Start()
    {
        float m = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float s = PlayerPrefs.GetFloat("SFXVolume", 1f);

        if (musicSlider != null)
        {
            musicSlider.value = m;
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = s;
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        SetMusicVolume(m);
        SetSFXVolume(s);
    }

    private float SliderToDb(float volume)
    {
        return Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
    }

    public void SetMusicVolume(float volume)
    {
        if (audioMixer == null) return;

        float db = SliderToDb(volume);
        bool ok = audioMixer.SetFloat("MusicVolume", db);
        if (!ok)
        {
            Debug.LogError("AudioMixer parameter 'MusicVolume' not found. Check that the parameter name is exactly correct and is exposed in the AudioMixer.");
            return;
        }

        float currentDb;
        audioMixer.GetFloat("MusicVolume", out currentDb);
        Debug.Log("MusicVolume dB = " + currentDb);

        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer == null) return;

        float db = SliderToDb(volume);
        bool ok = audioMixer.SetFloat("SFXVolume", db);
        if (!ok)
        {
            Debug.LogError("AudioMixer parameter 'SFXVolume' not found. Check that the parameter name is exactly correct and is exposed in the AudioMixer.");
            return;
        }

        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }
}