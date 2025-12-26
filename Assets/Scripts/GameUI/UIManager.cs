using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Leaderboard UI")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform leaderboardContent;
    [SerializeField] private GameObject playerRowPrefab;

    private bool isLeaderboardShown = false;
    private float updateTimer = 0f; // temp

    [Header("Controllers")]
    [SerializeField] private HUDController hudController;
    [SerializeField] private DeathScreenController deathScreenController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowDeathScreen(DamageContext source)
    {
        deathScreenController.Show(source);
    }

    public void HideDeathScreen()
    {
        deathScreenController.Hide();
    }

    public void UpdateHealth(float current, float max)
    {
        hudController.UpdateHealth(current, max);
    }

    public void AddKillFeedEntry(DamageContext ctx, string victim)
    {
        hudController.AddKillFeed(ctx, victim);
    }

    public void SetLeaderboardVisible(bool visible)
    {
        if (isLeaderboardShown == visible) return;

        isLeaderboardShown = visible;
        leaderboardPanel.SetActive(visible);

        if (visible)
        {
            RefreshLeaderboard();
        }
    }

    private void RefreshLeaderboard()
    {
        Debug.Log($"[Leaderboard] Refresh. Players in ActivePlayers: {Player.ActivePlayers.Count}");

        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }

        if (Player.ActivePlayers.Count == 0)
        {
            Debug.LogWarning("[Leaderboard] ActivePlayers is empty! No data to display.");
            return;
        }

        var playersList = new List<Player>(Player.ActivePlayers.Values);
        playersList.Sort((a, b) =>
        {
            if (a.Kills != b.Kills) return b.Kills.CompareTo(a.Kills);
            if (a.Deaths != b.Deaths) return a.Deaths.CompareTo(b.Deaths);
            return string.Compare(a.Nickname, b.Nickname, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var player in playersList)
        {
            GameObject row = Instantiate(playerRowPrefab, leaderboardContent);
            var texts = row.GetComponentsInChildren<TMPro.TextMeshProUGUI>();

            if (texts.Length < 3)
            {
                Debug.LogError("[Leaderboard] In PlayerRowPrefab less than 3 TextMeshPro! Fix prefab.");
                continue;
            }

            texts[0].text = player.Nickname;
            texts[1].text = player.Kills.ToString();
            texts[2].text = player.Deaths.ToString();

            Debug.Log($"[Leaderboard] Добавлена строка: {player.Nickname} | Kills: {player.Kills} | Deaths: {player.Deaths}");
        }
    }

    // перестраховка, пока непонятно, нужна ли
    private void Update()
    {
        if (isLeaderboardShown)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= 1f)
            {
                RefreshLeaderboard();
                updateTimer = 0f;
            }
        }
    }
}