using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace UI
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject pausePanel;
        public GameObject optionsPanel;

        [Header("Input")]
        public KeyCode toggleKey = KeyCode.Escape;

        [Header("UI Click Sound (optional)")]
        public AudioClip clickClip;
        public AudioMixerGroup sfxMixerGroup;
        public AudioSource audioSource;

        [Header("Behaviour")]
        public bool startPaused = false;
        public bool showCursorOnPause = true;

        bool isPaused = false;
        CursorLockMode savedLockState;
        bool savedCursorVisible;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;

            if (sfxMixerGroup != null)
                audioSource.outputAudioMixerGroup = sfxMixerGroup;

            if (pausePanel != null)
                pausePanel.SetActive(startPaused);

            isPaused = startPaused;
            savedLockState = Cursor.lockState;
            savedCursorVisible = Cursor.visible;
            if (isPaused && showCursorOnPause) ShowCursor();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                TogglePause();
        }

        public void TogglePause()
        {
            if (isPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            PlayClick();
            if (pausePanel != null) pausePanel.SetActive(true);
            Time.timeScale = 0f;
            isPaused = true;
            if (showCursorOnPause) ShowCursor();
        }

        public void Resume()
        {
            PlayClick();
            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                optionsPanel.SetActive(false);
                return;
            }

            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
            isPaused = false;
            if (showCursorOnPause) RestoreCursor();
        }

        public void OpenOptions()
        {
            PlayClick();
            if (optionsPanel != null) optionsPanel.SetActive(true);
        }

        public void CloseOptions()
        {
            PlayClick();
            if (optionsPanel != null) optionsPanel.SetActive(false);
        }

        public void ResumeAfterClick()
        {
            if (clickClip == null || audioSource == null)
            {
                Resume();
                return;
            }
            PlayClick();
            StartCoroutine(ResumeAfterRealtime(clickClip.length));
        }

        IEnumerator ResumeAfterRealtime(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
            isPaused = false;
            if (showCursorOnPause) RestoreCursor();
        }

        void PlayClick()
        {
            if (clickClip == null || audioSource == null) return;
            audioSource.PlayOneShot(clickClip);
        }

        void ShowCursor()
        {
            savedLockState = Cursor.lockState;
            savedCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void RestoreCursor()
        {
            Cursor.lockState = savedLockState;
            Cursor.visible = savedCursorVisible;
        }
    }
}
