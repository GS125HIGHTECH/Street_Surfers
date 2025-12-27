using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Leaderboards;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const string LeaderboardId = "best_scores";

    [Header("UI Elements")]
    public Transform content;
    public GameObject entryPrefab;

    private readonly List<(int rank, string username, double score)> cachedLeaderboardEntries = new();

    private void Awake()
    {
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

    public async void AddScoreToLeaderboard(double score)
    {
        try
        {
            string username = AuthenticationManager.Instance.GetUsername();

            var metadata = new Dictionary<string, string>() { { "username", username } };

            await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, score, new AddPlayerScoreOptions { Metadata = metadata });
            FetchLeaderboardScores();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding score to leaderboard: {e.Message}");
        }
    }

    public async void FetchLeaderboardScores()
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }

            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { IncludeMetadata = true });

            cachedLeaderboardEntries.Clear();

            if (scoresResponse.Results == null || scoresResponse.Results.Count == 0)
            {
                ShowCachedLeaderboard();
                return;
            }

            foreach (var scoreEntry in scoresResponse.Results)
            {
                string playerId = scoreEntry.PlayerId;
                double score = scoreEntry.Score;

                string username = "Unknown";
                if (!string.IsNullOrEmpty(scoreEntry.Metadata))
                {
                    try
                    {
                        var metadata = JsonConvert.DeserializeObject<Dictionary<string, string>>(scoreEntry.Metadata);

                        if (metadata != null && metadata.TryGetValue("username", out var extractedUsername))
                        {
                            username = extractedUsername;
                        }
                    }
                    catch (Exception jsonException)
                    {
                        Debug.LogError($"Error parsing metadata: {jsonException.Message}");
                    }
                }
                cachedLeaderboardEntries.Add((scoreEntry.Rank + 1, username, score));
                CreateLeaderboardEntry(scoreEntry.Rank + 1, username, score);
            }
        }
        else
        {
            ShowCachedLeaderboard();
        }
    }

    private async void ShowCachedLeaderboard()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        if (cachedLeaderboardEntries.Count > 0)
        {
            foreach (var (rank, username, score) in cachedLeaderboardEntries)
            {
                CreateLeaderboardEntry(rank, username, score);
            }
        }
        else
        {
            string username = AuthenticationManager.Instance.GetUsername();
            double bestScore = await GameManager.Instance.LoadData("bestScore", 0.0, true);

            CreateLeaderboardEntry(1, username, bestScore);
        }
    }

    private void CreateLeaderboardEntry(int rank, string username, double score)
    {
        GameObject entryObject = Instantiate(entryPrefab, content);

        if (entryObject.TryGetComponent<TextMeshProUGUI>(out var textField))
        {
            textField.text = $"{rank}. {username} - {score:F2}";
            if(AuthenticationManager.Instance.GetUsername() == username)
            {
                textField.fontStyle |= FontStyles.Underline;
            }
        }
        else
        {
            Debug.LogError("TextMeshPro component not found in entryPrefab!");
        }
    }

    public void ShowLeaderboard()
    {
        if(!GameManager.Instance.leaderboardPanel.activeSelf)
        {
            AudioManager.Instance.PlayClickSound();
            GameManager.Instance.touchScreenText.SetActive(false);
            FetchLeaderboardScores();
            GameManager.Instance.leaderboardPanel.SetActive(true);
            GameManager.Instance.profilePanel.SetActive(false);
            GameManager.Instance.upgradePanel.SetActive(false);
        }
    }

    public void HideLeaderboard()
    {
        GameManager.Instance.leaderboardPanel.SetActive(false);
        GameManager.Instance.touchScreenText.SetActive(true);
    }
}
