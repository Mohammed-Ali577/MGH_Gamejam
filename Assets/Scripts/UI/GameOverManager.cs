using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel;

    public AudioSource audioSource;
    public AudioClip buttonClickSound;

    public void GameOver()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Retry()
    {
        audioSource.PlayOneShot(buttonClickSound);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        audioSource.PlayOneShot(buttonClickSound);

        Time.timeScale = 1f;
        SceneManager.LoadScene("HarrisonW-MainMenu");
    }
}