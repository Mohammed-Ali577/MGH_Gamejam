using UnityEngine;
using System;

/// <summary>
/// Simple static score manager. Other systems (UI) can subscribe to OnScoreChanged to update visuals.
/// </summary>
public static class ScoreManager01
{
    public static int Score { get; private set; } = 0;

    public static event Action<int> OnScoreChanged;

    public static void AddPoints(int amount)
    {
        Score += amount;
        Debug.Log($"ScoreManager: Score changed by {amount}, new score = {Score}");
        OnScoreChanged?.Invoke(Score);
    }

    public static void Reset()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }
}
