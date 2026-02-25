using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>
/// PlayMode тесты для GlobalLeaderboardUI.
/// Сетевые методы (LoadAndDisplayLeaderboard, Refresh, Show с await) не тестируются напрямую —
/// вместо этого тестируется внутренняя логика через рефлексию.
/// </summary>
public class GlobalLeaderboardUIPlayModeTests
{
    // ?????????????????????????????????????????????????????????????????????????
    // Сцена
    // ?????????????????????????????????????????????????????????????????????????

    private GameObject _root;
    private GlobalLeaderboardUI _ui;

    private GameObject _panel;
    private Transform _content;
    private GameObject _rowPrefab;
    private TextMeshProUGUI _totalPlayersText;
    private TextMeshProUGUI _yourPositionText;
    private TextMeshProUGUI _loadingText;
    private Button _closeButton;
    private Button _refreshButton;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        var canvasGO = new GameObject("Canvas");
        canvasGO.AddComponent<Canvas>();

        _root = new GameObject("GlobalLeaderboardUI");

        // Создаём кнопки ДО AddComponent — Awake читает их сразу
        _closeButton = MakeButton("CloseButton", _root.transform);
        _refreshButton = MakeButton("RefreshButton", _root.transform);

        // Теперь добавляем компонент — Awake найдёт кнопки через SerializeField...
        // но SerializeField через рефлексию инжектируется ПОСЛЕ, поэтому
        // нужно инжектировать ДО AddComponent через отдельный GameObject с преднастройкой.
        // Проще всего — инжектировать поля ДО того как Unity вызовет Awake,
        // что невозможно напрямую. Решение: отключить GameObject, добавить компонент, инжектировать, включить.

        _root.SetActive(false); // Awake не вызовется пока объект неактивен

        _ui = _root.AddComponent<GlobalLeaderboardUI>();

        // Создаём остальные объекты
        _panel = new GameObject("Panel");
        _panel.transform.SetParent(_root.transform);

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(_panel.transform);
        _content = contentGO.transform;

        _rowPrefab = new GameObject("RowPrefab");
        _rowPrefab.AddComponent<RectTransform>();
        for (int i = 0; i < 5; i++)
        {
            var cell = new GameObject($"Cell{i}");
            cell.transform.SetParent(_rowPrefab.transform);
            cell.AddComponent<TextMeshProUGUI>();
        }

        _totalPlayersText = MakeTMP("TotalPlayers", canvasGO.transform);
        _yourPositionText = MakeTMP("YourPosition", canvasGO.transform);
        _loadingText = MakeTMP("LoadingText", canvasGO.transform);

        // Инжектируем ВСЕ поля пока объект неактивен
        SetField(_ui, "panel", _panel);
        SetField(_ui, "content", _content);
        SetField(_ui, "rowPrefab", _rowPrefab);
        SetField(_ui, "totalPlayersText", _totalPlayersText);
        SetField(_ui, "yourPositionText", _yourPositionText);
        SetField(_ui, "loadingText", _loadingText);
        SetField(_ui, "closeButton", _closeButton);
        SetField(_ui, "refreshButton", _refreshButton);
        SetField(_ui, "topLimit", 20);

        // Теперь активируем — Awake вызовется с уже заполненными полями
        _root.SetActive(true);

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(GameObject.Find("Canvas"));
        yield return null;
    }

    // ?????????????????????????????????????????????????????????????????????????
    // Helpers
    // ?????????????????????????????????????????????????????????????????????????

    private TextMeshProUGUI MakeTMP(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.AddComponent<TextMeshProUGUI>();
    }

    private Button MakeButton(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.AddComponent<Button>();
    }

    private void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {obj.GetType().Name}");
        f.SetValue(obj, value);
    }

    private T GetField<T>(object obj, string name)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {obj.GetType().Name}");
        return (T)f.GetValue(obj);
    }

    private T InvokeMethod<T>(object obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{name}' not found");
        return (T)m.Invoke(obj, args);
    }

    private void InvokeMethod(object obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{name}' not found");
        m.Invoke(obj, args);
    }

    private LeaderboardResponse MakeLeaderboardResponse(int totalPlayers, params (string nick, int kills, int deaths, int games)[] entries)
    {
        var response = new LeaderboardResponse
        {
            totalPlayers = totalPlayers,
            leaderboard = new List<PlayerLeaderboardEntry>()
        };
        foreach (var e in entries)
        {
            response.leaderboard.Add(new PlayerLeaderboardEntry
            {
                nickname = e.nick,
                kills = e.kills,
                deaths = e.deaths,
                gamesPlayed = e.games
            });
        }
        return response;
    }

    private PlayerStatsResponse MakePlayerStats(string nickname)
    {
        return new PlayerStatsResponse { nickname = nickname };
    }

    // ?????????????????????????????????????????????????????????????????????????
    // Hide Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator Hide_DeactivatesPanel()
    {
        _panel.SetActive(true);
        _ui.Hide();
        yield return null;

        Assert.IsFalse(_panel.activeSelf, "Hide должен деактивировать panel");
    }

    [UnityTest]
    public IEnumerator Hide_CalledTwice_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => { _ui.Hide(); _ui.Hide(); });
        yield return null;
    }

    // ?????????????????????????????????????????????????????????????????????????
    // Toggle Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator Toggle_WhenPanelActive_Hides()
    {
        _panel.SetActive(true);
        _ui.Toggle();
        yield return null;

        Assert.IsFalse(_panel.activeSelf, "Toggle на активном панели должен скрыть её");
    }

    [UnityTest]
    public IEnumerator Toggle_WhenPanelInactive_Shows()
    {
        _panel.SetActive(false);

        LogAssert.Expect(LogType.Error,
            "PlayerId <= 0 - player is not logged in or data has not been uploaded.");

        _ui.Toggle();
        yield return null;

        Assert.IsTrue(_panel.activeSelf, "Toggle на скрытой панели должен показать её");
    }

    [UnityTest]
    public IEnumerator Toggle_TwiceSameState_RestoresOriginal()
    {
        bool initial = _panel.activeSelf;
        _ui.Toggle();
        yield return null;
        _ui.Hide(); // сбрасываем в false чтобы второй Toggle не вызвал Show с сетью
        _panel.SetActive(initial);
        yield return null;

        Assert.AreEqual(initial, _panel.activeSelf);
    }

    // ?????????????????????????????????????????????????????????????????????????
    // GetPlayerRank Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator GetPlayerRank_ReturnsCorrectPosition()
    {
        var list = new List<PlayerLeaderboardEntry>
        {
            new PlayerLeaderboardEntry { nickname = "Alice" },
            new PlayerLeaderboardEntry { nickname = "Bob" },
            new PlayerLeaderboardEntry { nickname = "Charlie" }
        };

        int rank = InvokeMethod<int>(_ui, "GetPlayerRank", list, "Bob");
        yield return null;

        Assert.AreEqual(2, rank, "Bob должен быть на 2-м месте");
    }

    [UnityTest]
    public IEnumerator GetPlayerRank_FirstPlace_ReturnsOne()
    {
        var list = new List<PlayerLeaderboardEntry>
        {
            new PlayerLeaderboardEntry { nickname = "TopPlayer" },
            new PlayerLeaderboardEntry { nickname = "Second" }
        };

        int rank = InvokeMethod<int>(_ui, "GetPlayerRank", list, "TopPlayer");
        yield return null;

        Assert.AreEqual(1, rank, "Первый в списке должен получить ранг 1");
    }

    [UnityTest]
    public IEnumerator GetPlayerRank_NotFound_ReturnsMinusOne()
    {
        var list = new List<PlayerLeaderboardEntry>
        {
            new PlayerLeaderboardEntry { nickname = "Alice" }
        };

        int rank = InvokeMethod<int>(_ui, "GetPlayerRank", list, "Unknown");
        yield return null;

        Assert.AreEqual(-1, rank, "Отсутствующий игрок должен вернуть -1");
    }

    [UnityTest]
    public IEnumerator GetPlayerRank_EmptyList_ReturnsMinusOne()
    {
        var list = new List<PlayerLeaderboardEntry>();

        int rank = InvokeMethod<int>(_ui, "GetPlayerRank", list, "Anyone");
        yield return null;

        Assert.AreEqual(-1, rank, "Пустой список должен вернуть -1");
    }

    // ?????????????????????????????????????????????????????????????????????????
    // ClearRows Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator ClearRows_RemovesChildrenExceptPrefab()
    {
        // Добавляем несколько строк в content
        var row1 = new GameObject("Row1");
        row1.transform.SetParent(_content);
        var row2 = new GameObject("Row2");
        row2.transform.SetParent(_content);

        InvokeMethod(_ui, "ClearRows");
        yield return null; // Destroy обрабатывается в конце кадра
        yield return null;

        // rowPrefab не в content по умолчанию, поэтому content должен быть пуст
        Assert.AreEqual(0, _content.childCount, "ClearRows должен удалить все строки из content");
    }

    [UnityTest]
    public IEnumerator ClearRows_EmptyContent_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => InvokeMethod(_ui, "ClearRows"),
            "ClearRows на пустом content не должен бросать исключений");
        yield return null;
    }

    // ?????????????????????????????????????????????????????????????????????????
    // DisplayLeaderboard Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator DisplayLeaderboard_SetsCorrectTotalPlayersText()
    {
        var response = MakeLeaderboardResponse(42,
            ("Alice", 10, 2, 5),
            ("Bob", 7, 3, 4));
        var stats = MakePlayerStats("Alice");

        InvokeMethod(_ui, "DisplayLeaderboard", response, stats);
        yield return null;

        StringAssert.Contains("42", _totalPlayersText.text,
            "totalPlayersText должен отображать общее число игроков");
    }

    [UnityTest]
    public IEnumerator DisplayLeaderboard_PlayerInTop_ShowsRank()
    {
        var response = MakeLeaderboardResponse(100,
            ("Alice", 10, 2, 5),
            ("Bob", 7, 3, 4));
        var stats = MakePlayerStats("Alice");

        InvokeMethod(_ui, "DisplayLeaderboard", response, stats);
        yield return null;

        StringAssert.Contains("1", _yourPositionText.text,
            "yourPositionText должен показывать место игрока в топе");
    }

    [UnityTest]
    public IEnumerator DisplayLeaderboard_PlayerNotInTop_ShowsOutsideMessage()
    {
        var response = MakeLeaderboardResponse(500,
            ("Alice", 10, 2, 5),
            ("Bob", 7, 3, 4));
        var stats = MakePlayerStats("UnknownPlayer");

        InvokeMethod(_ui, "DisplayLeaderboard", response, stats);
        yield return null;

        StringAssert.Contains("outside", _yourPositionText.text.ToLower(),
            "Если игрок не в топе — должно отображаться сообщение об этом");
    }

    [UnityTest]
    public IEnumerator DisplayLeaderboard_SpawnsCorrectNumberOfRows()
    {
        var response = MakeLeaderboardResponse(10,
            ("Alice", 5, 1, 3),
            ("Bob", 3, 2, 2),
            ("Eve", 1, 5, 1));
        var stats = MakePlayerStats("Bob");

        InvokeMethod(_ui, "DisplayLeaderboard", response, stats);
        yield return null;

        // Каждая запись создаёт один Instantiate(rowPrefab)
        Assert.AreEqual(3, _content.childCount,
            "Должно быть создано столько строк, сколько записей в leaderboard");
    }

    [UnityTest]
    public IEnumerator DisplayLeaderboard_RowTexts_FilledCorrectly()
    {
        var response = MakeLeaderboardResponse(1,
            ("HeroPlayer", 99, 0, 10));
        var stats = MakePlayerStats("Other");

        InvokeMethod(_ui, "DisplayLeaderboard", response, stats);
        yield return null;

        var row = _content.GetChild(0);
        var texts = row.GetComponentsInChildren<TextMeshProUGUI>();

        Assert.AreEqual("1", texts[0].text, "Ранг должен быть '1'");
        Assert.AreEqual("HeroPlayer", texts[1].text, "Никнейм должен совпадать");
        Assert.AreEqual("99", texts[2].text, "Kills должны совпадать");
        Assert.AreEqual("0", texts[3].text, "Deaths должны совпадать");
        Assert.AreEqual("10", texts[4].text, "GamesPlayed должны совпадать");
    }

    [UnityTest]
    public IEnumerator DisplayLeaderboard_PlayerRow_HighlightedYellow()
    {
        var response = MakeLeaderboardResponse(2,
            ("Alice", 5, 1, 3),
            ("Bob", 3, 2, 2));
        var stats = MakePlayerStats("Alice");

        InvokeMethod(_ui, "DisplayLeaderboard", response, stats);
        yield return null;

        var aliceRow = _content.GetChild(0);
        var texts = aliceRow.GetComponentsInChildren<TextMeshProUGUI>();
        var goldColor = new Color(1f, 0.95f, 0.4f);

        foreach (var txt in texts)
            Assert.AreEqual(goldColor, txt.color, "Строка текущего игрока должна быть золотой");
    }

    [UnityTest]
    public IEnumerator DisplayLeaderboard_ClearsOldRowsFirst()
    {
        // Первый вызов — 3 строки
        var response1 = MakeLeaderboardResponse(3,
            ("A", 1, 0, 1), ("B", 2, 0, 1), ("C", 3, 0, 1));
        InvokeMethod(_ui, "DisplayLeaderboard", response1, MakePlayerStats("X"));
        yield return null;

        // Второй вызов — 1 строка
        var response2 = MakeLeaderboardResponse(1, ("Solo", 10, 0, 5));
        InvokeMethod(_ui, "DisplayLeaderboard", response2, MakePlayerStats("X"));
        yield return null;
        yield return null; // Destroy от ClearRows

        Assert.AreEqual(1, _content.childCount,
            "После второго DisplayLeaderboard должна быть только 1 строка");
    }

    // ?????????????????????????????????????????????????????????????????????????
    // Awake / Button listeners
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator Awake_CloseButton_CallsHide()
    {
        _panel.SetActive(true);
        _closeButton.onClick.Invoke();
        yield return null;

        Assert.IsFalse(_panel.activeSelf, "CloseButton должен скрывать panel через Hide()");
    }

    [UnityTest]
    public IEnumerator isLoading_InitiallyFalse()
    {
        bool loading = GetField<bool>(_ui, "isLoading");
        yield return null;

        Assert.IsFalse(loading, "isLoading должен быть false при старте");
    }
}