using System.Collections;
using System.Reflection;
using kcp2k;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AbilityRuntimeBaseAndInvisManagerTests
{
    private class StubRuntime : AbilityRuntime
    {
        public bool RunCalled;
        public override void Run() => RunCalled = true;
    }

    private static T GetField<T>(object target, string name)
    {
        var f = target.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f == null)
            f = target.GetType().BaseType?
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Поле '{name}' не найдено в {target.GetType().Name}");
        return (T)f.GetValue(target);
    }

    private static void InvokePrivate(object target, string method, params object[] args)
    {
        var m = target.GetType()
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Метод '{method}' не найден в {target.GetType().Name}");
        m.Invoke(target, args);
    }

    #region Bind

    [Test]
    public void Bind_StoresRigidbody()
    {
        var go = new GameObject();
        var rb = go.AddComponent<Rigidbody>();
        var runtime = new StubRuntime();

        runtime.Bind(rb, null);

        Assert.AreSame(rb, GetField<Rigidbody>(runtime, "rb"),
            "Bind должен сохранить переданный Rigidbody в поле rb");

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator Bind_StoresPlayer()
    {
        var go = new GameObject();
        var rb = go.AddComponent<Rigidbody>();
        var pGo = new GameObject();
        pGo.AddComponent<NetworkIdentity>();

        var player = pGo.AddComponent<Player>();

        var runtime = new StubRuntime();
        runtime.Bind(rb, player);

        var storedPlayer = GetField<Player>(runtime, "player");
        Assert.IsNotNull(storedPlayer, "Bind должен сохранить Player");
        Assert.AreSame(player, storedPlayer, "Сохраненный Player должен быть тем же объектом");

        Object.Destroy(go);
        Object.Destroy(pGo);

        yield return null;

        var afterDestroy = GetField<Player>(runtime, "player");
        Assert.IsTrue(afterDestroy == null, "После уничтожения объектов поле player должно быть null");
    }

    [Test]
    public void Bind_AllowsNullPlayer()
    {
        var go = new GameObject();
        var rb = go.AddComponent<Rigidbody>();
        var runtime = new StubRuntime();

        Assert.DoesNotThrow(() => runtime.Bind(rb, null),
            "Bind с null player не должен бросать исключение");

        Object.Destroy(go);
    }

    #endregion

    #region Default virtual methods

    [Test]
    public void DefaultServerUpdate_DoesNotThrow()
    {
        var runtime = new StubRuntime();
        Assert.DoesNotThrow(() => runtime.ServerUpdate(),
            "Базовый ServerUpdate не должен бросать исключение");
    }

    [Test]
    public void DefaultAbsorbDamage_ReturnsIncomingDamageUnchanged()
    {
        var runtime = new StubRuntime();
        float result = runtime.AbsorbDamage(42f);
        Assert.AreEqual(42f, result, 0.001f,
            "Базовый AbsorbDamage должен вернуть урон без изменений");
    }

    [Test]
    public void DefaultAbsorbDamage_ReturnsZero_WhenZeroInput()
    {
        var runtime = new StubRuntime();
        Assert.AreEqual(0f, runtime.AbsorbDamage(0f), 0.001f,
            "AbsorbDamage(0) должен вернуть 0");
    }

    [Test]
    public void DefaultGetSpeedMultiplier_ReturnsOne()
    {
        var runtime = new StubRuntime();
        Assert.AreEqual(1f, runtime.GetSpeedMultiplier(), 0.001f,
            "Базовый GetSpeedMultiplier должен вернуть 1");
    }

    [Test]
    public void DefaultGetVisualStatus_ReturnsZero()
    {
        var runtime = new StubRuntime();
        Assert.AreEqual(0f, runtime.GetVisualStatus(), 0.001f,
            "Базовый GetVisualStatus должен вернуть 0");
    }

    [Test]
    public void DefaultOnEquipped_DoesNotThrow()
    {
        var runtime = new StubRuntime();
        Assert.DoesNotThrow(() => runtime.OnEquipped());
    }

    [Test]
    public void DefaultOnUnequipped_DoesNotThrow()
    {
        var runtime = new StubRuntime();
        Assert.DoesNotThrow(() => runtime.OnUnequipped());
    }

    [Test]
    public void DefaultOnOwnerDamaged_DoesNotThrow()
    {
        var runtime = new StubRuntime();
        Assert.DoesNotThrow(() => runtime.OnOwnerDamaged());
    }

    [Test]
    public void DefaultOnOwnerAttacked_DoesNotThrow()
    {
        var runtime = new StubRuntime();
        Assert.DoesNotThrow(() => runtime.OnOwnerAttacked());
    }

    #endregion

    #region Run (через stub)

    [Test]
    public void StubRun_IsCalled()
    {
        var runtime = new StubRuntime();
        runtime.Run();
        Assert.IsTrue(runtime.RunCalled,
            "Run должен вызываться и выполняться в наследнике");
    }

    #endregion

    #region Helpers

    private (InvisManager mgr, GameObject go) CreateInvisManager()
    {
        var go = new GameObject("InvisManagerTest");
        var mgr = go.AddComponent<InvisManager>();
        return (mgr, go);
    }

    #endregion

    #region Начальное состояние (SyncVar)

    [Test]
    public void InvisManager_DefaultIsVisible_IsTrue()
    {
        var (mgr, go) = CreateInvisManager();

        bool isVisible = GetField<bool>(mgr, "isVisible");
        Assert.IsTrue(isVisible,
            "По умолчанию isVisible должен быть true");

        Object.Destroy(go);
    }

    #endregion

    #region RefreshRenderers — null-safety

    [Test]
    public void InvisManager_RefreshRenderers_DoesNotThrow_WithoutAssembler()
    {
        var (mgr, go) = CreateInvisManager();

        Assert.DoesNotThrow(() => mgr.RefreshRenderers(),
            "RefreshRenderers не должен бросать, если ShipAssembler отсутствует");

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator InvisManager_RefreshRenderers_DoesNotThrow_AfterFrameDelay()
    {
        var (mgr, go) = CreateInvisManager();
        yield return null;

        Assert.DoesNotThrow(() => mgr.RefreshRenderers());

        Object.Destroy(go);
    }

    #endregion

    #region SetVisible — [Server] guard

    [Test]
    public void InvisManager_SetVisible_WhenNotServer_DoesNotChangeIsVisible()
    {
        var (mgr, go) = CreateInvisManager();

        bool before = GetField<bool>(mgr, "isVisible");
        mgr.SetVisible(!before);

        bool after = GetField<bool>(mgr, "isVisible");
        Assert.AreEqual(before, after,
            "SetVisible без активного Mirror-сервера не должен менять isVisible");

        Object.Destroy(go);
    }

    [Test]
    public void InvisManager_SetVisible_DoesNotThrow_OnDestroyedContext()
    {
        var (mgr, go) = CreateInvisManager();

        Assert.DoesNotThrow(() => mgr.SetVisible(false));

        Object.Destroy(go);
    }

    #endregion

    #region UpdateRendererList / ApplyVisibility — через RefreshRenderers

    [Test]
    public void InvisManager_ShipRenderersListEmpty_WithoutAssembler()
    {
        var (mgr, go) = CreateInvisManager();
        mgr.RefreshRenderers();

        var list = GetField<System.Collections.Generic.List<Renderer>>(
            mgr, "shipRenderers");
        Assert.IsNotNull(list, "shipRenderers не должен быть null");
        Assert.AreEqual(0, list.Count,
            "Без ShipAssembler список рендереров должен быть пустым");

        Object.Destroy(go);
    }

    [Test]
    public void InvisManager_ParticleRenderersListEmpty_WithoutAssembler()
    {
        var (mgr, go) = CreateInvisManager();
        mgr.RefreshRenderers();

        var list = GetField<System.Collections.Generic.List<ParticleSystem>>(
            mgr, "particleRenderers");
        Assert.IsNotNull(list, "particleRenderers не должен быть null");
        Assert.AreEqual(0, list.Count,
            "Без ShipAssembler список партиклов должен быть пустым");

        Object.Destroy(go);
    }

    [Test]
    public void InvisManager_RefreshRenderers_CanBeCalledMultipleTimes()
    {
        var (mgr, go) = CreateInvisManager();

        Assert.DoesNotThrow(() =>
        {
            mgr.RefreshRenderers();
            mgr.RefreshRenderers();
            mgr.RefreshRenderers();
        }, "Многократный вызов RefreshRenderers не должен бросать");

        Object.Destroy(go);
    }

    #endregion

    #region ApplyVisibility — через рефлексию (без Mirror)

    [Test]
    public void InvisManager_ApplyVisibility_True_DoesNotThrow_WithEmptyLists()
    {
        var (mgr, go) = CreateInvisManager();

        Assert.DoesNotThrow(() => InvokePrivate(mgr, "ApplyVisibility", true),
            "ApplyVisibility(true) с пустыми списками не должен бросать");

        Object.Destroy(go);
    }

    [Test]
    public void InvisManager_ApplyVisibility_False_DoesNotThrow_WithEmptyLists()
    {
        var (mgr, go) = CreateInvisManager();

        Assert.DoesNotThrow(() => InvokePrivate(mgr, "ApplyVisibility", false),
            "ApplyVisibility(false) с пустыми списками не должен бросать");

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator InvisManager_ApplyVisibility_WithEmptyList_DoesNotThrow_AndLogsMessage()
    {
        var go = new GameObject("InvisApplyVis");
        var mgr = go.AddComponent<InvisManager>();
        yield return null;

        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Applying visibility"));
        Assert.DoesNotThrow(() => InvokePrivate(mgr, "ApplyVisibility", true));

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator InvisManager_ApplyVisibility_False_LogsCorrectValue()
    {
        var go = new GameObject("InvisApplyVisFalse");
        var mgr = go.AddComponent<InvisManager>();
        yield return null;

        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Applying visibility: False"));
        Assert.DoesNotThrow(() => InvokePrivate(mgr, "ApplyVisibility", false));

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator InvisManager_ApplyVisibility_HandlesNullRenderers_Gracefully()
    {
        var go = new GameObject("InvisNullRenderer");
        var mgr = go.AddComponent<InvisManager>();

        var list = GetField<System.Collections.Generic.List<Renderer>>(mgr, "shipRenderers");
        list.Add(null);

        yield return null;

        Assert.DoesNotThrow(() => InvokePrivate(mgr, "ApplyVisibility", true),
            "ApplyVisibility должен пропускать null-рендереры без исключений");

        Object.Destroy(go);
    }

    #endregion

    #region OnVisibleChanged hook

    [Test]
    public void InvisManager_OnVisibleChanged_DoesNotThrow()
    {
        var (mgr, go) = CreateInvisManager();

        Assert.DoesNotThrow(() => InvokePrivate(mgr, "OnVisibleChanged", true, false));
        Assert.DoesNotThrow(() => InvokePrivate(mgr, "OnVisibleChanged", false, true));

        Object.Destroy(go);
    }

    #endregion
}