using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour
{
    [Header("UI Click Sound (optional)")]
    public AudioClip clickClip;
    public AudioMixerGroup sfxMixerGroup; // route to mixer group controlled by SFXVolume
    public AudioSource audioSource;       // leave null to auto-add
    public bool waitForClipBeforeLoad = false;

    [Header("UI Panels")]
    public GameObject[] panels;           // list all panels you want managed (menu, options, credits, etc.)

    private GameObject currentPanel;
    private Stack<GameObject> panelStack = new Stack<GameObject>();

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        if (sfxMixerGroup != null)
            audioSource.outputAudioMixerGroup = sfxMixerGroup;

        // find first active panel as currentPanel
        if (panels != null && panels.Length > 0)
        {
            foreach (var p in panels)
            {
                if (p != null && p.activeSelf)
                {
                    currentPanel = p;
                    break;
                }
            }
        }
    }

    public void PlayGame()
    {
        PlayClick();
        if (waitForClipBeforeLoad && clickClip != null)
            StartCoroutine(LoadAfterDelay(clickClip.length));
        else
            SceneManager.LoadSceneAsync(1);
    }

    public void QuitGame()
    {
        PlayClick();
        if (waitForClipBeforeLoad && clickClip != null)
            StartCoroutine(QuitAfterDelay(clickClip.length));
        else
            Application.Quit();
    }

    public void PlayClick()
    {
        if (clickClip == null || audioSource == null) return;
        audioSource.PlayOneShot(clickClip);
    }

    // Shows only the requested panel and hides all others.
    // Clears the panel history stack (use when switching main sections).
    public void ShowOnlyPanel(GameObject panelToOpen)
    {
        PlayClick();
        if (panelToOpen == null) return;

        if (panels != null && panels.Length > 0)
        {
            foreach (var p in panels)
            {
                if (p == null) continue;
                p.SetActive(p == panelToOpen);
            }
        }
        else
        {
            if (currentPanel != null && currentPanel != panelToOpen)
                currentPanel.SetActive(false);
            panelToOpen.SetActive(true);
        }

        currentPanel = panelToOpen;
        panelStack.Clear();
    }

    // Opens a panel additively and pushes the previous panel onto the stack.
    // Useful for Options -> Cancel returning to previous panel.
    public void OpenPanelPush(GameObject panelToOpen)
    {
        PlayClick();
        if (panelToOpen == null) return;

        if (currentPanel != null && currentPanel != panelToOpen)
            panelStack.Push(currentPanel);

        panelToOpen.SetActive(true);
        currentPanel = panelToOpen;
    }

    // Closes the current panel and returns to the previous one on the stack.
    // If stack is empty, will try to enable the first panel in the panels list.
    public void CloseCurrentAndPop()
    {
        PlayClick();
        if (currentPanel != null)
            currentPanel.SetActive(false);

        if (panelStack.Count > 0)
        {
            var prev = panelStack.Pop();
            if (prev != null)
            {
                prev.SetActive(true);
                currentPanel = prev;
                return;
            }
        }

        // fallback: find any active panel from list or set current to null
        currentPanel = null;
        if (panels != null && panels.Length > 0)
        {
            foreach (var p in panels)
            {
                if (p != null && p.activeSelf)
                {
                    currentPanel = p;
                    break;
                }
            }
        }
    }

    // Hides a specific panel (no stack logic).
    public void ClosePanel(GameObject panelToClose)
    {
        PlayClick();
        if (panelToClose == null) return;
        panelToClose.SetActive(false);

        if (panelToClose == currentPanel)
        {
            currentPanel = null;
            // try set current to any active panel
            if (panels != null && panels.Length > 0)
            {
                foreach (var p in panels)
                {
                    if (p != null && p.activeSelf)
                    {
                        currentPanel = p;
                        break;
                    }
                }
            }
        }
    }

    // Toggle a panel on/off (no stack changes)
    public void TogglePanel(GameObject panel)
    {
        PlayClick();
        if (panel == null) return;
        bool next = !panel.activeSelf;
        panel.SetActive(next);
        if (next) currentPanel = panel;
        else if (panel == currentPanel) currentPanel = null;
    }

    private IEnumerator LoadAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadSceneAsync(1);
    }

    private IEnumerator QuitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Application.Quit();
    }
}
