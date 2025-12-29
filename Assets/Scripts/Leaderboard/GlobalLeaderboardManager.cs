using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GlobalLeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private TextMeshProUGUI totalPlayersText;
    [SerializeField] private TextMeshProUGUI yourPositionText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button refreshButton;

    [Header("Settings")]
    [SerializeField] private int topLimit = 20;

    private bool isLoading = false;

    private void Awake()
    {
        closeButton.onClick.AddListener(Hide);
        refreshButton.onClick.AddListener(Refresh);
    }

    private void Start()
    {
        Hide();
    }

    public async void Show()
    {
        panel.SetActive(true);
        await LoadAndDisplayLeaderboard();
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    public void Toggle()
    {
        if (panel.activeSelf)
            Hide();
        else
            Show();
    }

    public async void Refresh()
    {
        ClearRows();
        await LoadAndDisplayLeaderboard();
    }

    private async Task LoadAndDisplayLeaderboard()
    {
        if (isLoading) return;
        isLoading = true;

        ClearRows();

        try
        {

            int playerId = ConfigManager.LoadConfig().player_id;
            Debug.Log($"[Leaderboard] PlayerId from GameData: {playerId}");

            if (playerId <= 0)
            {
                Debug.LogError("PlayerId <= 0 - player is not logged in or data has not been uploaded.");
                yourPositionText.text = "Error: you are not logged in";
                totalPlayersText.text = "Требуется вход в аккаунт";
                isLoading = false;
                return;
            }

            if (APINetworkManager.Instance == null)
            {
                Debug.LogError("APINetworkManager.Instance is NULL! Object is not created.");
                yourPositionText.text = "Network error";
                totalPlayersText.text = "APINetworkManager not found";
                isLoading = false;
                return;
            }

            if (string.IsNullOrEmpty(APINetworkManager.AuthToken))
            {
                Debug.LogError("AuthToken is empty or null - no auth on GameServer.");
                yourPositionText.text = "Error: no token";
                totalPlayersText.text = "Re-entry required";
                isLoading = false;
                return;
            }

            Debug.Log($"[Leaderboard] Sending request from player_id={playerId}, token (length: {APINetworkManager.AuthToken.Length})");

            string query = $"players_limit={topLimit}&player_id={playerId}";
            var leaderboardResponse = await APINetworkManager.Instance.GetRequestAsync<LeaderboardResponse>("/leaderboard", query);

            if (leaderboardResponse == null)
            {
                Debug.LogError("Response from /leaderboard - null");
                yourPositionText.text = "No data";
                isLoading = false;
                return;
            }

            Debug.Log($"[Leaderboard] Received {leaderboardResponse.leaderboard?.Count ?? 0} recordings in the top");

            var playerStats = await APINetworkManager.Instance.GetRequestAsync<PlayerStatsResponse>($"/leaderboard/{playerId}");

            DisplayLeaderboard(leaderboardResponse, playerStats);
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception while loading leaderboard: " + ex.Message);
            Debug.LogError("StackTrace: " + ex.StackTrace);
            yourPositionText.text = "Network error";
            totalPlayersText.text = "Check internet";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void DisplayLeaderboard(LeaderboardResponse response, PlayerStatsResponse playerStats)
    {
        ClearRows();

        int rank = 1;
        bool playerFoundInTop = false;

        foreach (var entry in response.leaderboard)
        {
            var row = Instantiate(rowPrefab, content).GetComponent<RectTransform>();
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 5)
            {
                texts[0].text = rank.ToString();
                texts[1].text = entry.nickname;
                texts[2].text = entry.kills.ToString();
                texts[3].text = entry.deaths.ToString();
                texts[4].text = entry.gamesPlayed.ToString();

                if (entry.nickname == playerStats.nickname)
                {
                    playerFoundInTop = true;
                    foreach (var txt in texts)
                    {
                        txt.color = new Color(1f, 0.95f, 0.4f);
                        txt.fontStyle = FontStyles.Bold;
                    }
                }
            }
            rank++;
        }

        totalPlayersText.text = $"Total players: {response.totalPlayers}";

        if (playerFoundInTop)
        {
            yourPositionText.text = $"Your place is: {GetPlayerRank(response.leaderboard, playerStats.nickname)} of {response.totalPlayers}";
        }
        else
        {
            yourPositionText.text = $"You are outside top {topLimit}";
        }
    }

    private int GetPlayerRank(List<PlayerLeaderboardEntry> list, string nickname)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].nickname == nickname)
                return i + 1;
        }
        return -1;
    }

    private void ClearRows()
    {
        foreach (Transform child in content)
        {
            if (child.gameObject != rowPrefab)
                Destroy(child.gameObject);
        }
    }
}