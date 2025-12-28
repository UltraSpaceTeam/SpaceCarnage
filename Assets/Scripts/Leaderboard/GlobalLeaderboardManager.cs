using Network;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GlobalLeaderboardManager : MonoBehaviour
{
    public static GlobalLeaderboardManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform leaderboardContent;
    [SerializeField] private GameObject playerRowPrefab;
    [SerializeField] private TextMeshProUGUI totalPlayersText;
    [SerializeField] private TextMeshProUGUI yourPositionText;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [SerializeField] private int topPlayersLimit = 10;

    // Data
    private LeaderboardResponse currentLeaderboard;
    private PlayerStatsResponse currentPlayerStats;
    private int currentPlayerRank = -1;

    private bool isLoading = false;

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
            return;
        }

        if (refreshButton) refreshButton.onClick.AddListener(RefreshAllData);
        if (closeButton) closeButton.onClick.AddListener(HideLeaderboard);
    }

    private void Start()
    {
        HideLeaderboard();
    }

    public void ShowLeaderboard()
    {
        leaderboardPanel.SetActive(true);
        if (currentLeaderboard == null || currentPlayerStats == null)
        {
            RefreshAllData();
        }
        else
        {
            RebuildUI();
        }
    }

    public void HideLeaderboard()
    {
        leaderboardPanel.SetActive(false);
    }

    public async void RefreshAllData()
    {
        if (isLoading) return;
        isLoading = true;

        ClearLeaderboardUI();

        try
        {
            int playerId = GameData.Instance.PlayerId;
            if (playerId <= 0)
            {
                Debug.LogError("PlayerId не установлен в GameData!");
                return;
            }

            string query = $"players_limit={topPlayersLimit}&player_id={playerId}";
            currentLeaderboard = await APINetworkManager.Instance.GetRequestAsync<LeaderboardResponse>("/leaderboard", query);

            currentPlayerStats = await APINetworkManager.Instance.GetRequestAsync<PlayerStatsResponse>($"/leaderboard/{playerId}");

            currentPlayerRank = -1;
            for (int i = 0; i < currentLeaderboard.leaderboard.Count; i++)
            {
                if (currentLeaderboard.leaderboard[i].nickname == currentPlayerStats.nickname)
                {
                    currentPlayerRank = i + 1;
                    break;
                }
            }

            RebuildUI();
        }
        catch (Exception ex)
        {
            Debug.LogError("Ошибка загрузки лидерборда: " + ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void RebuildUI()
    {
        ClearLeaderboardUI();

        if (currentLeaderboard == null) return;

        int rank = 1;
        foreach (var entry in currentLeaderboard.leaderboard)
        {
            GameObject row = Instantiate(playerRowPrefab, leaderboardContent);
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 5)
            {
                texts[0].text = rank.ToString();
                texts[1].text = entry.nickname;
                texts[2].text = entry.kills.ToString();
                texts[3].text = entry.deaths.ToString();
                texts[4].text = entry.gamesPlayed.ToString();

                if (entry.nickname == currentPlayerStats.nickname)
                {
                    foreach (var txt in texts)
                    {
                        txt.color = new Color(1f, 0.9f, 0.3f);
                        txt.fontStyle = FontStyles.Bold;
                    }
                }
            }

            rank++;
        }

        if (totalPlayersText)
            totalPlayersText.text = $"Всего игроков: {currentLeaderboard.totalPlayers}";

        if (yourPositionText)
        {
            if (currentPlayerRank > 0)
                yourPositionText.text = $"Ваше место: {currentPlayerRank} из {currentLeaderboard.totalPlayers}";
            else
                yourPositionText.text = $"Ваше место: вне топ-{topPlayersLimit} (из {currentLeaderboard.totalPlayers})";
        }
    }

    private void ClearLeaderboardUI()
    {
        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }
    }

    public PlayerStatsResponse GetCurrentPlayerStats() => currentPlayerStats;
    public bool IsPlayerInTop() => currentPlayerRank > 0;
    public int GetPlayerRank() => currentPlayerRank;
}