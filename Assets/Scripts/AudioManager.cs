using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header ("---------- Audio Source ----------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;


    [Header ("---------- Audio Source ----------")]
    public AudioClip background;
    public AudioClip GameOver;
    public AudioClip Click;

    private void Start()
    {
        musicSource.clip = background;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    { 
    SFXSource.PlayOneShot(clip);
    }
}
