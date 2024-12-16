using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Unity.Services.Leaderboards;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const string LeaderboardId = "best_scores";

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
        try
        {
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { IncludeMetadata = true });

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

                Debug.Log($"Rank: {scoreEntry.Rank}, Player: {username}, Score: {score}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error retrieving leaderboard scores: {e.Message}");
        }
    }
}
