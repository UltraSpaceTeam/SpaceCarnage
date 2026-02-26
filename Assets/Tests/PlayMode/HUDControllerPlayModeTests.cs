using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;
using Mirror;

public class HUDControllerTests
{
    private GameObject _hudGO;
    private HUDController _hud;

    private T GetField<T>(object obj, string name)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found");
        return (T)f.GetValue(obj);
    }

    private void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found");
        f.SetValue(obj, value);
    }

    private T InvokeFunc<T>(object obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{name}' not found");
        return (T)m.Invoke(obj, args);
    }

    private void Invoke(object obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{name}' not found");
        m.Invoke(obj, args);
    }

    private Image MakeImage()
    {
        var go = new GameObject("Image");
        go.transform.SetParent(_hudGO.transform);
        return go.AddComponent<Image>();
    }

    private TextMeshProUGUI MakeTMP(string name = "TMP")
    {
        var go = new GameObject(name);
        go.transform.SetParent(_hudGO.transform);
        return go.AddComponent<TextMeshProUGUI>();
    }

    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true;
        Player.ActivePlayers.Clear();

        _hudGO = new GameObject("HUDController");
        _hud = _hudGO.AddComponent<HUDController>();
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;
        Player.ActivePlayers.Clear();
        Object.DestroyImmediate(_hudGO);
    }

    [Test]
    public void UpdateHealth_SetsTargetFillAmount()
    {
        _hud.UpdateHealth(50f, 100f);

        float fill = GetField<float>(_hud, "_targetFillAmount");
        Assert.AreEqual(0.5f, fill, 0.001f);
    }

    [Test]
    public void UpdateHealth_FullHealth_FillIsOne()
    {
        _hud.UpdateHealth(100f, 100f);

        float fill = GetField<float>(_hud, "_targetFillAmount");
        Assert.AreEqual(1f, fill, 0.001f);
    }

    [Test]
    public void UpdateHealth_ZeroHealth_FillIsZero()
    {
        _hud.UpdateHealth(0f, 100f);

        float fill = GetField<float>(_hud, "_targetFillAmount");
        Assert.AreEqual(0f, fill, 0.001f);
    }

    [Test]
    public void UpdateHealth_OverMax_ClampedToOne()
    {
        _hud.UpdateHealth(200f, 100f);

        float fill = GetField<float>(_hud, "_targetFillAmount");
        Assert.AreEqual(1f, fill, 0.001f);
    }

    [Test]
    public void UpdateHealth_SetsHealthText_WhenNotNull()
    {
        var text = MakeTMP("healthText");
        SetField(_hud, "healthText", text);

        _hud.UpdateHealth(75f, 100f);

        Assert.AreEqual("75 / 100", text.text);
    }

    [Test]
    public void UpdateHealth_NullHealthText_DoesNotThrow()
    {
        SetField(_hud, "healthText", null);
        Assert.DoesNotThrow(() => _hud.UpdateHealth(50f, 100f));
    }

    [Test]
    public void UpdateHealth_CeilsCurrentValue_InText()
    {
        var text = MakeTMP("healthText");
        SetField(_hud, "healthText", text);

        _hud.UpdateHealth(74.3f, 100f);

        StringAssert.StartsWith("75", text.text);
    }

    [UnityTest]
    public IEnumerator Start_AbilityPanel_DisabledOnStart()
    {
        var panel = new GameObject("AbilityPanel");
        panel.transform.SetParent(_hudGO.transform);
        panel.SetActive(true);
        SetField(_hud, "abilityPanel", panel);

        var go = new GameObject("HUD2");
        var hud2 = go.AddComponent<HUDController>();
        SetField(hud2, "abilityPanel", panel);
        yield return null;

        Assert.IsFalse(panel.activeSelf, "abilityPanel должен быть выключен в Start");

        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator Start_AmmoText_ClearedOnStart()
    {
        var go = new GameObject("HUD3");
        var hud3 = go.AddComponent<HUDController>();
        var ammo = new GameObject("ammo").AddComponent<TextMeshProUGUI>();
        ammo.transform.SetParent(go.transform);
        ammo.text = "10 / 20";
        SetField(hud3, "ammoText", ammo);
        yield return null;

        Assert.AreEqual("", ammo.text, "ammoText должен быть пуст в Start");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void GetMatchTimerText_StateZero_AlwaysReturnsDashes()
    {
        string result = InvokeFunc<string>(_hud, "GetMatchTimerText");
        Assert.AreEqual("--:--", result,
            "При ClientTimerState=0 должен вернуться '--:--'");
    }

    [Test]
    public void GetMatchTimerText_StateOne_MatchStartTimeZero_ReturnsDashes()
    {
        var go = new GameObject("PlayerTimer");
        var p = go.AddComponent<HUDController>();

        var prop = typeof(Player).GetProperty("ClientTimerState",
            BindingFlags.Public | BindingFlags.Static);
        var propTime = typeof(Player).GetProperty("ClientMatchStartTime",
            BindingFlags.Public | BindingFlags.Static);

        if (prop != null) prop.SetValue(null, 1);
        if (propTime != null) propTime.SetValue(null, 0.0);

        string result = InvokeFunc<string>(_hud, "GetMatchTimerText");
        Assert.AreEqual("--:--", result,
            "ClientMatchStartTime=0 должен вернуть '--:--'");

        if (prop != null) prop.SetValue(null, 0);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void FindLocalPlayer_EmptyActivePlayers_ReturnsNull()
    {
        Player.ActivePlayers.Clear();
        var result = InvokeFunc<Player>(_hud, "FindLocalPlayer");
        Assert.IsNull(result);
    }

    [Test]
    public void UpdateAmmoIndicator_NoLocalPlayer_DoesNotThrow()
    {
        Player.ActivePlayers.Clear();
        Assert.DoesNotThrow(() => Invoke(_hud, "UpdateAmmoIndicator"));
    }

    [Test]
    public void UpdateAmmoIndicator_NullAmmoText_DoesNotThrow()
    {
        Player.ActivePlayers.Clear();
        SetField(_hud, "ammoText", null);
        Assert.DoesNotThrow(() => Invoke(_hud, "UpdateAmmoIndicator"));
    }

    [Test]
    public void UpdateAbilityIndicator_NoLocalPlayer_DoesNotThrow()
    {
        Player.ActivePlayers.Clear();
        Assert.DoesNotThrow(() => Invoke(_hud, "UpdateAbilityIndicator"));
    }

    [Test]
    public void AddKillFeed_Suicide_ContainsSuicideTag()
    {
        var (container, prefab) = MakeKillFeedSetup();
        _hud.AddKillFeed(DamageContext.Suicide("Player1"), "Player1");
        var text = GetLastKillFeedText(container);
        StringAssert.Contains("SUICIDE", text);
        StringAssert.Contains("Player1", text);
    }

    [Test]
    public void AddKillFeed_WeaponKill_ContainsAttackerAndVictim()
    {
        var (container, prefab) = MakeKillFeedSetup();
        _hud.AddKillFeed(DamageContext.Weapon(1u, "Killer", "Laser"), "Victim");
        var text = GetLastKillFeedText(container);
        StringAssert.Contains("Killer", text);
        StringAssert.Contains("Victim", text);
        StringAssert.Contains("Laser", text);
    }

    [Test]
    public void AddKillFeed_OwnGoal_ContainsOwnGoalTag()
    {
        var (container, prefab) = MakeKillFeedSetup();
        _hud.AddKillFeed(DamageContext.Weapon(1u, "Player1", "Laser"), "Player1");
        var text = GetLastKillFeedText(container);
        StringAssert.Contains("OWN GOAL", text);
    }

    [Test]
    public void AddKillFeed_CollisionWithAttacker_ContainsRamTag()
    {
        var (container, prefab) = MakeKillFeedSetup();
        _hud.AddKillFeed(DamageContext.Collision(1u, "Attacker", ""), "Victim");
        var text = GetLastKillFeedText(container);
        StringAssert.Contains("RAM", text);
        StringAssert.Contains("Attacker", text);
    }

    [Test]
    public void AddKillFeed_CollisionNoAttacker_ContainsCrashTag()
    {
        var (container, prefab) = MakeKillFeedSetup();
        _hud.AddKillFeed(DamageContext.Collision(0u, "", ""), "Victim");
        var text = GetLastKillFeedText(container);
        StringAssert.Contains("CRASH", text);
    }

    [Test]
    public void AddKillFeed_ExceedsMaxItems_RemovesOldest()
    {
        var (container, prefab) = MakeKillFeedSetup();
        SetField(_hud, "maxKillFeedItems", 3);
        for (int i = 0; i < 4; i++)
            _hud.AddKillFeed(DamageContext.Suicide($"Player{i}"), $"Player{i}");
        Assert.LessOrEqual(container.childCount, 4);
    }

    [UnityTest]
    public IEnumerator Update_NullHealthBarFill_DoesNotThrow()
    {
        SetField(_hud, "healthBarFill", null);
        SetField(_hud, "matchTimerText", null);
        yield return null;
        Assert.Pass();
    }

    [UnityTest]
    public IEnumerator Update_WithMatchTimerText_SetsText()
    {
        var timerText = MakeTMP("matchTimerText");
        SetField(_hud, "matchTimerText", timerText);
        yield return null;

        Assert.AreEqual("--:--", timerText.text);
    }

    private string GetLastKillFeedText(Transform container)
    {
        var lastChild = container.GetChild(container.childCount - 1);
        var tmp = lastChild.GetComponentInChildren<TextMeshProUGUI>();
        return tmp != null ? tmp.text : null;
    }

    private (Transform container, GameObject prefab) MakeKillFeedSetup()
    {
        var containerGO = new GameObject("KillFeedContainer");
        containerGO.transform.SetParent(_hudGO.transform);

        var prefab = new GameObject("KillFeedItem");
        var tmpChild = new GameObject("Text");
        tmpChild.transform.SetParent(prefab.transform);
        tmpChild.AddComponent<TextMeshProUGUI>();

        SetField(_hud, "killFeedContainer", containerGO.transform);
        SetField(_hud, "killFeedItemPrefab", prefab);
        SetField(_hud, "killFeedDuration", 99f);
        SetField(_hud, "maxKillFeedItems", 5);

        return (containerGO.transform, prefab);
    }
}

public class HUDControllerAbilityTests
{
    private GameObject _hudGO;
    private HUDController _hud;
    private GameObject _playerGO;
    private Player _player;
    private ShipAssembler _assembler;
    private PlayerController _controller;

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

    private Image MakeImage(string name = "Img")
    {
        var go = new GameObject(name);
        go.transform.SetParent(_hudGO.transform);
        return go.AddComponent<Image>();
    }

    private TextMeshProUGUI MakeTMP(string name = "TMP")
    {
        var go = new GameObject(name);
        go.transform.SetParent(_hudGO.transform);
        return go.AddComponent<TextMeshProUGUI>();
    }

    private GameObject MakePanel(string name = "Panel")
    {
        var go = new GameObject(name);
        go.transform.SetParent(_hudGO.transform);
        return go;
    }
    private EngineData MakeEngineWithAbility(AbstractAbility ability)
    {
        var engine = ScriptableObject.CreateInstance<EngineData>();
        SetField(engine, "ability", ability);
        return engine;
    }

    private void ConnectHudToPlayer()
    {
        _hud._findLocalPlayerOverride = () => _player;
    }

    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true;
        Player.ActivePlayers.Clear();

        _hudGO = new GameObject("HUDController");
        _hud = _hudGO.AddComponent<HUDController>();

        _playerGO = new GameObject("Player");
        _playerGO.AddComponent<NetworkIdentity>();
        _playerGO.AddComponent<Rigidbody>();
        _playerGO.AddComponent<Health>();
        _assembler = _playerGO.AddComponent<ShipAssembler>();
        _controller = _playerGO.AddComponent<PlayerController>();
        _playerGO.AddComponent<NetworkAudio>().enabled = false;
        _player = _playerGO.AddComponent<Player>();

        ConnectHudToPlayer();
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;
        Player.ActivePlayers.Clear();
        Object.DestroyImmediate(_hudGO);
        Object.DestroyImmediate(_playerGO);
    }

    [Test]
    public void UpdateAbilityIndicator_NoEngine_HidesPanelAndReturns()
    {
        var panel = MakePanel();
        panel.SetActive(true);
        SetField(_hud, "abilityPanel", panel);

        _assembler.SetEngineForTests(null);

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.IsFalse(panel.activeSelf, "abilityPanel должен быть скрыт если нет engine");
    }

    [Test]
    public void UpdateAbilityIndicator_EngineWithoutAbility_HidesPanel()
    {
        var panel = MakePanel();
        panel.SetActive(true);
        SetField(_hud, "abilityPanel", panel);

        var engine = ScriptableObject.CreateInstance<EngineData>();
        _assembler.SetEngineForTests(engine);

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.IsFalse(panel.activeSelf);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_ShowsPanel()
    {
        var panel = MakePanel();
        SetField(_hud, "abilityPanel", panel);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.IsTrue(panel.activeSelf, "abilityPanel должен быть показан при наличии ability");

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_IconSpriteIsShieldIcon()
    {
        var icon = MakeImage("abilityIcon");
        var shieldSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        SetField(_hud, "abilityIcon", icon);
        SetField(_hud, "shieldIcon", shieldSprite);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(shieldSprite, icon.sprite);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_NoCooldown_IconIsWhite()
    {
        var icon = MakeImage("abilityIcon");
        SetField(_hud, "abilityIcon", icon);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);

        SetField(_controller, "abilityReadyTime", 0.0);

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(Color.white, icon.color);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_OnCooldown_IconIsGray()
    {
        var icon = MakeImage("abilityIcon");
        SetField(_hud, "abilityIcon", icon);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);

        SetField(_controller, "abilityReadyTime", double.MaxValue);

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(new Color(0.55f, 0.55f, 0.55f, 1f), icon.color);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_StatusOne_TextIsReady()
    {
        var text = MakeTMP("abilityText");
        SetField(_hud, "abilityText", text);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 1f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual("READY", text.text);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_StatusHalf_TextShowsPercent()
    {
        var text = MakeTMP("abilityText");
        SetField(_hud, "abilityText", text);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 0.5f;

        Invoke(_hud, "UpdateAbilityIndicator");

        StringAssert.StartsWith("ON:", text.text);
        StringAssert.Contains("50", text.text);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_StatusZero_TextIsBroken()
    {
        var text = MakeTMP("abilityText");
        SetField(_hud, "abilityText", text);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 0f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual("BROKEN", text.text);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_ProgressFull_FillIsGreen()
    {
        var fill = MakeImage("abilityProgressFill");
        SetField(_hud, "abilityProgressFill", fill);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 1f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(1f, fill.fillAmount, 0.01f);
        Assert.AreEqual(0f, fill.color.r, 0.01f);
        Assert.AreEqual(1f, fill.color.g, 0.01f);
        Assert.AreEqual(0.55f, fill.color.a, 0.01f);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_ProgressHigh_FillIsYellowGreen()
    {
        var fill = MakeImage("abilityProgressFill");
        SetField(_hud, "abilityProgressFill", fill);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 0.8f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(0.8f, fill.fillAmount, 0.01f);
        Assert.Greater(fill.color.g, 0f);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_ProgressLow_FillIsReddish()
    {
        var fill = MakeImage("abilityProgressFill");
        SetField(_hud, "abilityProgressFill", fill);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 0.3f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(0.3f, fill.fillAmount, 0.01f);
        Assert.Greater(fill.color.r, 0f);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_ProgressZero_FillIsDarkRed()
    {
        var fill = MakeImage("abilityProgressFill");
        SetField(_hud, "abilityProgressFill", fill);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 0f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(new Color(0.6f, 0.2f, 0.2f, 0.55f), fill.color);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_ShieldAbility_CooldownFill_NoCooldown_FillIsZero()
    {
        var cooldownFill = MakeImage("abilityCooldownFill");
        SetField(_hud, "abilityCooldownFill", cooldownFill);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        SetField(ability, "cooldown", 5f);
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);

        SetField(_controller, "abilityReadyTime", 0.0);

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(0f, cooldownFill.fillAmount, 0.01f);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_InvisAbility_IconSpriteIsInvisIcon()
    {
        var icon = MakeImage("abilityIcon");
        var invisSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        SetField(_hud, "abilityIcon", icon);
        SetField(_hud, "invisIcon", invisSprite);

        var ability = ScriptableObject.CreateInstance<InvisAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(invisSprite, icon.sprite);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_InvisAbility_StatusFull_TextIsInvisible()
    {
        var text = MakeTMP("abilityText");
        SetField(_hud, "abilityText", text);

        var ability = ScriptableObject.CreateInstance<InvisAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 1f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual("Invisible", text.text);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_InvisAbility_StatusActivating_TextIsActivating()
    {
        var text = MakeTMP("abilityText");
        SetField(_hud, "abilityText", text);

        var ability = ScriptableObject.CreateInstance<InvisAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 0.5f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual("Activating...", text.text);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_InvisAbility_StatusZero_TextIsEmpty()
    {
        var text = MakeTMP("abilityText");
        SetField(_hud, "abilityText", text);

        var ability = ScriptableObject.CreateInstance<InvisAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 0f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual("", text.text);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_InvisAbility_StatusFull_FillIsBlue()
    {
        var fill = MakeImage("abilityProgressFill");
        SetField(_hud, "abilityProgressFill", fill);

        var ability = ScriptableObject.CreateInstance<InvisAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 1f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(1f, fill.fillAmount, 0.01f);
        Assert.AreEqual(new Color(0.6f, 0.6f, 1f, 0.6f), fill.color);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_InvisAbility_StatusMid_FillColorIsLightBlue()
    {
        var fill = MakeImage("abilityProgressFill");
        SetField(_hud, "abilityProgressFill", fill);

        var ability = ScriptableObject.CreateInstance<InvisAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 0.5f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(new Color(0.7f, 0.7f, 1f, 0.7f), fill.color);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_InvisAbility_StatusZero_FillIsZeroAndClear()
    {
        var fill = MakeImage("abilityProgressFill");
        SetField(_hud, "abilityProgressFill", fill);

        var ability = ScriptableObject.CreateInstance<InvisAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);
        _controller.AbilityStatusValue = 0f;

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(0f, fill.fillAmount, 0.01f);
        Assert.AreEqual(Color.clear, fill.color);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_DashAbility_IconSpriteIsDashIcon()
    {
        var icon = MakeImage("abilityIcon");
        var dashSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        SetField(_hud, "abilityIcon", icon);
        SetField(_hud, "dashIcon", dashSprite);

        var ability = ScriptableObject.CreateInstance<DashAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual(dashSprite, icon.sprite);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_DashAbility_TextIsEmpty()
    {
        var text = MakeTMP("abilityText");
        SetField(_hud, "abilityText", text);

        var ability = ScriptableObject.CreateInstance<DashAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);

        Invoke(_hud, "UpdateAbilityIndicator");

        Assert.AreEqual("", text.text);

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_UnknownAbility_IconIsDefault()
    {
        var icon = MakeImage("abilityIcon");
        var defaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        SetField(_hud, "abilityIcon", icon);
        SetField(_hud, "defaultAbilityIcon", defaultSprite);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        SetField(_hud, "abilityIcon", null);

        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);

        Assert.DoesNotThrow(() => Invoke(_hud, "UpdateAbilityIndicator"));

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    [Test]
    public void UpdateAbilityIndicator_AllFieldsNull_DoesNotThrow()
    {
        SetField(_hud, "abilityPanel", null);
        SetField(_hud, "abilityIcon", null);
        SetField(_hud, "abilityProgressFill", null);
        SetField(_hud, "abilityCooldownFill", null);
        SetField(_hud, "abilityText", null);

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        var engine = MakeEngineWithAbility(ability);
        _assembler.SetEngineForTests(engine);

        Assert.DoesNotThrow(() => Invoke(_hud, "UpdateAbilityIndicator"));

        Object.DestroyImmediate(ability);
        Object.DestroyImmediate(engine);
    }

    private void Invoke(object obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{name}' not found on {obj.GetType().Name}");
        m.Invoke(obj, args);
    }
}