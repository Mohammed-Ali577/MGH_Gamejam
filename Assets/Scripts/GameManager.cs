using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Singleton game manager that tracks total spawned enemies and ends the game when a limit is reached.
/// You can now drag Scene assets into the inspector for Start / Pause / GameOver screens.
/// The scene asset's name (file name without extension) is stored into the corresponding scene name string
/// and used at runtime for SceneManager loading. A warning is emitted in the Editor if the scene is not in Build Settings.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("When total spawned enemies reaches this value the game ends.")]
    public int spawnLimit = 10;

    [Tooltip("If true, time will be paused (Time.timeScale = 0) when the game ends.")]
    public bool pauseOnEnd = true;

    [Tooltip("If true, load the GameOver scene automatically when spawn limit is reached.")]
    public bool loadSceneOnEnd = false;

    [Header("Scene references (drag scene assets here in the Editor)")]
#if UNITY_EDITOR
    [Tooltip("Drag the Start scene asset (e.g. MainMenu). The asset's filename (without extension) will be used at runtime.")]
    [SerializeField] private SceneAsset startSceneAsset;
    [Tooltip("Drag the Pause scene asset (UI overlay recommended).")]
    [SerializeField] private SceneAsset pauseSceneAsset;
    [Tooltip("Drag the Game Over scene asset.")]
    [SerializeField] private SceneAsset gameOverSceneAsset;
#endif

    [Tooltip("Runtime scene name to load for Start screen (auto-filled from the Start Scene Asset in Editor).")]
    public string startSceneName = "";

    [Tooltip("Runtime scene name to load for Pause screen (auto-filled from the Pause Scene Asset in Editor).")]
    public string pauseSceneName = "";

    [Tooltip("Runtime scene name to load for GameOver screen (auto-filled from the GameOver Scene Asset in Editor).")]
    public string gameOverSceneName = "";

    [Header("Win by Score")]
    [Tooltip("Total score required to trigger a win.")]
    public int winScore = 80;

    public int totalSpawned { get; private set; } = 0;
    public bool GameOver { get; private set; } = false;

    public static event Action OnGameEnd;

#if UNITY_EDITOR
    // Keep the string names synced with dragged SceneAsset(s) while editing
    private void OnValidate()
    {
        SyncSceneAssetToName(startSceneAsset, ref startSceneName);
        SyncSceneAssetToName(pauseSceneAsset, ref pauseSceneName);
        SyncSceneAssetToName(gameOverSceneAsset, ref gameOverSceneName);
    }

    private void SyncSceneAssetToName(SceneAsset sceneAsset, ref string destName)
    {
        if (sceneAsset == null)
            return;

        string path = AssetDatabase.GetAssetPath(sceneAsset);
        if (string.IsNullOrEmpty(path))
            return;

        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        if (!string.Equals(destName, fileName, StringComparison.Ordinal))
            destName = fileName;

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(path);
        if (buildIndex == -1)
        {
            Debug.LogWarning($"GameManager: Scene asset '{fileName}' is not in Build Settings. Add it to Build Settings to allow loading by name at runtime.", this);
        }
    }
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Subscribe to score changes to detect win-by-score
        ScoreManager.OnScoreChanged += HandleScoreChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid leaks when object is destroyed
        ScoreManager.OnScoreChanged -= HandleScoreChanged;
    }

    /// <summary>
    /// Call when enemies are spawned. When totalSpawned >= spawnLimit the game ends.
    /// </summary>
    public void AddSpawned(int count = 1)
    {
        if (GameOver) return;

        totalSpawned += Math.Max(0, count);
        Debug.Log($"GameManager: Added {count} spawned enemy(ies). TotalSpawned = {totalSpawned}/{spawnLimit}");

        if (totalSpawned >= spawnLimit)
            EndGame();
    }

    private void HandleScoreChanged(int newScore)
    {
        if (GameOver) return;

        if (newScore >= winScore)
        {
            Debug.Log($"GameManager: Score reached {newScore} (>= {winScore}) — You win!");
            EndGame();
        }
    }

    private void EndGame()
    {
        if (GameOver) return;
        GameOver = true;

        Debug.Log("GameManager: EndGame triggered.");

        OnGameEnd?.Invoke();

        if (pauseOnEnd)
            Time.timeScale = 0f;

        if (loadSceneOnEnd && !string.IsNullOrEmpty(gameOverSceneName))
            SceneManager.LoadScene(gameOverSceneName);
    }
}