using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AbilityRuntimePlayModeTests
{
    private static T GetField<T>(object target, string fieldName)
    {
        var f = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"���� '{fieldName}' �� ������� � {target.GetType().Name}");
        return (T)f.GetValue(target);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var f = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"���� '{fieldName}' �� ������� � {target.GetType().Name}");
        f.SetValue(target, value);
    }

    #region Helpers � Dash

    private (DashAbility ability, AbilityRuntime runtime, Rigidbody rb, GameObject go)
        CreateDashSetup(float boost = 100f)
    {
        var go = new GameObject("DashShip");
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;

        var ability = ScriptableObject.CreateInstance<DashAbility>();
        ability.dashVelocityBoost = boost;

        var runtime = ability.CreateRuntime();
        runtime.Bind(rb, null);

        return (ability, runtime, rb, go);
    }

    #endregion

    [Test]
    public void Dash_DefaultCooldown_Is10()
    {
        var ability = ScriptableObject.CreateInstance<DashAbility>();
        Assert.AreEqual(10f, ability.cooldown, "��������� ������� Dash ������ ���� 10");
        Object.Destroy(ability);
    }

    [UnityTest]
    public IEnumerator Dash_Run_ChangesRigidbodyVelocity()
    {
        var (ability, runtime, rb, go) = CreateDashSetup(boost: 100f);

        var velocityBefore = rb.linearVelocity;
        runtime.Run();
        yield return new WaitForFixedUpdate();

        Assert.AreNotEqual(velocityBefore, rb.linearVelocity,
            "����� Dash �������� Rigidbody ������ ����������");

        Object.Destroy(go);
        Object.Destroy(ability);
    }

    [UnityTest]
    public IEnumerator Dash_Run_PushesForwardAlongLocalZ()
    {
        var (ability, runtime, rb, go) = CreateDashSetup(boost: 100f);

        runtime.Run();
        yield return new WaitForFixedUpdate();

        Assert.Greater(rb.linearVelocity.z, 0f,
            "AddRelativeForce(Vector3.forward) ��� ������� �������� ������ ������ +Z � ������� �����������");

        Object.Destroy(go);
        Object.Destroy(ability);
    }

    [UnityTest]
    public IEnumerator Dash_Run_ScalesWithBoostValue()
    {
        var (abilityLow, runtimeLow, rbLow, goLow) = CreateDashSetup(boost: 50f);
        var (abilityHigh, runtimeHigh, rbHigh, goHigh) = CreateDashSetup(boost: 200f);

        runtimeLow.Run();
        runtimeHigh.Run();
        yield return new WaitForFixedUpdate();

        Assert.Greater(rbHigh.linearVelocity.magnitude, rbLow.linearVelocity.magnitude,
            "��?����� dashVelocityBoost ������ ������ ��?����� ��������");

        Object.Destroy(goLow); Object.Destroy(abilityLow);
        Object.Destroy(goHigh); Object.Destroy(abilityHigh);
    }

    #region Helpers � Shield

    private (ShieldAbility ability, AbilityRuntime runtime, Rigidbody rb, GameObject go)
        CreateShieldSetup(
            float maxHealth = 100f,
            float regenDelay = 5f,
            float regenRate = 20f,
            float speedReduce = 0.3f)
    {
        var go = new GameObject("ShieldShip");
        var rb = go.AddComponent<Rigidbody>();

        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        ability.maxShieldHealth = maxHealth;
        ability.regenerationDelay = regenDelay;
        ability.regenerationRate = regenRate;
        ability.speedReductionPercent = speedReduce;

        var runtime = ability.CreateRuntime();
        runtime.Bind(rb, null);

        return (ability, runtime, rb, go);
    }

    #endregion

    [Test]
    public void Shield_OnEquipped_SetsFullHealth()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(maxHealth: 80f);
        runtime.OnEquipped();

        Assert.AreEqual(80f, GetField<float>(runtime, "currentShieldHealth"),
            "����� OnEquipped �������� ���� ������ ���� maxShieldHealth");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_OnEquipped_ShieldIsInactive()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup();
        runtime.OnEquipped();

        Assert.IsFalse(GetField<bool>(runtime, "isActive"),
            "����� OnEquipped ��� �� ������ ���� �������");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_OnUnequipped_DeactivatesShield()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup();
        runtime.OnEquipped();
        runtime.Run();
        runtime.OnUnequipped();

        Assert.IsFalse(GetField<bool>(runtime, "isActive"),
            "OnUnequipped ������ �������������� ���");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_Run_ActivatesShieldOnFirstCall()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup();
        runtime.OnEquipped();
        runtime.Run();

        Assert.IsTrue(GetField<bool>(runtime, "isActive"),
            "������ Run ������ ������������ ���");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_Run_DeactivatesShieldOnSecondCall()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup();
        runtime.OnEquipped();
        runtime.Run();
        runtime.Run();

        Assert.IsFalse(GetField<bool>(runtime, "isActive"),
            "������ Run ������ �������������� ���");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_Run_StartsRegenTimerOnDeactivation()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(regenDelay: 5f);
        runtime.OnEquipped();
        runtime.Run();
        runtime.Run();

        Assert.AreEqual(5f, GetField<float>(runtime, "regenTimer"), 0.001f,
            "����� ���������� ���� ������ ����������� regenTimer = regenerationDelay");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_AbsorbDamage_WhenInactive_ReturnsFullDamage()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup();
        runtime.OnEquipped();

        float result = runtime.AbsorbDamage(50f);

        Assert.AreEqual(50f, result,
            "���������� ��� �� ������ ��������� ����");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_AbsorbDamage_WhenActive_AbsorbsFullDamage()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(maxHealth: 100f);
        runtime.OnEquipped();
        runtime.Run();

        float result = runtime.AbsorbDamage(40f);

        Assert.AreEqual(0f, result, 0.001f,
            "�������� ��� � ������� �������� ������ ��������� ��������� ����");
        Assert.AreEqual(60f, GetField<float>(runtime, "currentShieldHealth"), 0.001f,
            "�������� ���� ������ ����������� �� �������� ������������ �����");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_AbsorbDamage_Overflow_PassesThroughRemainder()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(maxHealth: 30f);
        runtime.OnEquipped();
        runtime.Run();

        float result = runtime.AbsorbDamage(50f);

        Assert.AreEqual(20f, result, 0.001f,
            "����, ����������� �������� ����, ������ �������� ������ ������");
        Assert.IsFalse(GetField<bool>(runtime, "isActive"),
            "��� ������ ��������� ��� ���������� ��������� �����");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_AbsorbDamage_ExactMatch_BreaksShieldAndPassesZero()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(maxHealth: 50f);
        runtime.OnEquipped();
        runtime.Run();

        float result = runtime.AbsorbDamage(50f);

        Assert.AreEqual(0f, result, 0.001f,
            "��� ������ ���������� ����� � �������� ���� ������ ������ ������ 0");
        Assert.IsFalse(GetField<bool>(runtime, "isActive"),
            "��� � ������� ��������� ������ ����������������");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_GetSpeedMultiplier_ReducedWhenActive()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(speedReduce: 0.3f);
        runtime.OnEquipped();
        runtime.Run();

        Assert.AreEqual(0.7f, runtime.GetSpeedMultiplier(), 0.001f,
            "�������� ��� ������ ������� �������� �� speedReductionPercent");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_GetSpeedMultiplier_FullWhenInactive()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(speedReduce: 0.3f);
        runtime.OnEquipped();

        Assert.AreEqual(1f, runtime.GetSpeedMultiplier(), 0.001f,
            "���������� ��� �� ������ ������� ��������");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_ServerUpdate_RegeneratesHealthWhenRegenTimerIsZero()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(
            maxHealth: 100f, regenRate: 20f, regenDelay: 0f);
        runtime.OnEquipped();

        SetField(runtime, "currentShieldHealth", 50f);
        SetField(runtime, "regenTimer", 0f);

        runtime.ServerUpdate();

        Assert.Greater(GetField<float>(runtime, "currentShieldHealth"), 50f,
            "ServerUpdate ������ �������������� �������� ���� ����� regenTimer = 0");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_ServerUpdate_DoesNotRegenWhileTimerIsRunning()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(regenDelay: 5f, regenRate: 20f);
        runtime.OnEquipped();

        SetField(runtime, "currentShieldHealth", 50f);
        SetField(runtime, "regenTimer", 3f);

        runtime.ServerUpdate();

        Assert.AreEqual(50f, GetField<float>(runtime, "currentShieldHealth"), 0.001f,
            "����������� �� ������ ���������� ���� regenTimer > 0");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_ServerUpdate_HealthClampedToMax()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(maxHealth: 100f, regenRate: 9999f);
        runtime.OnEquipped();

        SetField(runtime, "currentShieldHealth", 99f);
        SetField(runtime, "regenTimer", 0f);

        runtime.ServerUpdate();

        Assert.LessOrEqual(GetField<float>(runtime, "currentShieldHealth"), 100f,
            "�������� ���� �� ������ ��������� maxShieldHealth");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_ServerUpdate_BreaksShieldWhenHealthHitsZero()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(maxHealth: 100f);
        runtime.OnEquipped();
        runtime.Run();

        SetField(runtime, "currentShieldHealth", 0f);

        runtime.ServerUpdate();

        Assert.IsFalse(GetField<bool>(runtime, "isActive"),
            "ServerUpdate ������ �������������� ��� ��� currentShieldHealth <= 0");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_GetVisualStatus_ReflectsHealthRatio()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(maxHealth: 100f);
        runtime.OnEquipped();
        SetField(runtime, "currentShieldHealth", 75f);

        Assert.AreEqual(0.75f, runtime.GetVisualStatus(), 0.001f,
            "GetVisualStatus ������ ���������� currentHealth / maxHealth");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Shield_GetVisualStatus_ZeroWhenMaxHealthIsZero()
    {
        var (ability, runtime, rb, go) = CreateShieldSetup(maxHealth: 0f);
        runtime.OnEquipped();

        Assert.AreEqual(0f, runtime.GetVisualStatus(),
            "��� maxShieldHealth = 0 GetVisualStatus ������ ���������� 0 (��� ������� �� 0)");

        Object.Destroy(go); Object.Destroy(ability);
    }

    #region Helpers � Invis

    private (InvisAbility ability, AbilityRuntime runtime, Rigidbody rb, GameObject go)
        CreateInvisSetup(
            float activationDelay = 1.5f,
            bool breakOnAttack = true,
            bool breakOnDamage = true)
    {
        var go = new GameObject("InvisShip");
        var rb = go.AddComponent<Rigidbody>();
        go.AddComponent<InvisManager>();

        var ability = ScriptableObject.CreateInstance<InvisAbility>();
        ability.activationDelay = activationDelay;
        ability.breakOnAttack = breakOnAttack;
        ability.breakOnDamage = breakOnDamage;

        var runtime = ability.CreateRuntime();
        runtime.Bind(rb, null);

        return (ability, runtime, rb, go);
    }

    #endregion

    [Test]
    public void Invis_InitialState_IsInactive()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup();

        Assert.IsFalse(GetField<bool>(runtime, "isActive"),
            "������ ��������� runtime �� ������ ���� ��������");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_Run_ActivatesWhenInactive()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup();
        runtime.Run();

        Assert.IsTrue(GetField<bool>(runtime, "isActive"),
            "������ Run ������ ������������ �����������");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_Run_SetsDelayTimerOnActivation()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(activationDelay: 2f);
        runtime.Run();

        Assert.AreEqual(2f, GetField<float>(runtime, "delayTimer"), 0.001f,
            "��� ��������� delayTimer ������ ������������ � activationDelay");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_Run_DeactivatesWhenActive()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup();
        runtime.Run();
        runtime.Run();

        Assert.IsFalse(GetField<bool>(runtime, "isActive"),
            "������ Run ������ �������������� �����������");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_Run_ResetsTimerAndVisibilityOnDeactivation()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(activationDelay: 2f);
        runtime.Run();
        runtime.Run();

        Assert.AreEqual(0f, GetField<float>(runtime, "delayTimer"), 0.001f);
        Assert.IsFalse(GetField<bool>(runtime, "isFullyInvisible"),
            "����� ����������� isFullyInvisible ������ ����������");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [UnityTest]
    public IEnumerator Invis_ServerUpdate_CountsDownDelayTimer()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(activationDelay: 1f);
        runtime.Run();

        runtime.ServerUpdate();
        yield return new WaitForFixedUpdate();
        runtime.ServerUpdate();

        Assert.Less(GetField<float>(runtime, "delayTimer"), 1f,
            "ServerUpdate ������ ��������� delayTimer");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [UnityTest]
    public IEnumerator Invis_ServerUpdate_SetsFullyInvisibleAfterDelayExpires()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(activationDelay: 0.05f);
        runtime.Run();

        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            runtime.ServerUpdate();
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        Assert.IsTrue(GetField<bool>(runtime, "isFullyInvisible"),
            "����� ��������� �������� isFullyInvisible ������ ����� true");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_ServerUpdate_DoesNothingWhenInactive()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(activationDelay: 1f);

        runtime.ServerUpdate();

        Assert.IsFalse(GetField<bool>(runtime, "isActive"));
        Assert.IsFalse(GetField<bool>(runtime, "isFullyInvisible"));

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_OnOwnerDamaged_BreaksInvis_WhenFlagTrue()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(breakOnDamage: true);
        runtime.Run();
        runtime.OnOwnerDamaged();

        Assert.IsFalse(GetField<bool>(runtime, "isActive"),
            "���� ������ ��������� ����������� ��� breakOnDamage = true");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_OnOwnerDamaged_DoesNotBreak_WhenFlagFalse()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(breakOnDamage: false);
        runtime.Run();
        runtime.OnOwnerDamaged();

        Assert.IsTrue(GetField<bool>(runtime, "isActive"),
            "���� �� ������ ��������� ����������� ��� breakOnDamage = false");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_OnOwnerAttacked_BreaksInvis_WhenFlagTrue()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(breakOnAttack: true);
        runtime.Run();
        runtime.OnOwnerAttacked();

        Assert.IsFalse(GetField<bool>(runtime, "isActive"),
            "����� ������ ��������� ����������� ��� breakOnAttack = true");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_OnOwnerAttacked_DoesNotBreak_WhenFlagFalse()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(breakOnAttack: false);
        runtime.Run();
        runtime.OnOwnerAttacked();

        Assert.IsTrue(GetField<bool>(runtime, "isActive"),
            "����� �� ������ ��������� ����������� ��� breakOnAttack = false");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_OnUnequipped_DeactivatesIfActive()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup();
        runtime.Run();
        runtime.OnUnequipped();

        Assert.IsFalse(GetField<bool>(runtime, "isActive"),
            "OnUnequipped ������ �������������� �������� �����������");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_OnEquipped_ResetsAllState()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(activationDelay: 2f);
        runtime.Run();

        runtime.OnEquipped();

        Assert.IsFalse(GetField<bool>(runtime, "isActive"), "isActive должен быть false");
        Assert.IsFalse(GetField<bool>(runtime, "isFullyInvisible"), "isFullyInvisible должен быть false");
        Assert.AreEqual(0f, GetField<float>(runtime, "delayTimer"), 0.001f, "delayTimer должен быть 0");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_GetVisualStatus_ZeroWhenInactive()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup();

        Assert.AreEqual(0f, runtime.GetVisualStatus(),
            "GetVisualStatus ������ ���������� 0 ����� ����������� ���������");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_GetVisualStatus_ZeroAtStartOfDelay()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(activationDelay: 2f);
        runtime.Run();

        Assert.AreEqual(0f, runtime.GetVisualStatus(), 0.001f,
            "� ������ �������� GetVisualStatus должен быть 0 (��� �� ������ ���������)");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_GetVisualStatus_HalfwayThroughDelay()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(activationDelay: 2f);
        runtime.Run();

        SetField(runtime, "delayTimer", 1f);

        Assert.AreEqual(0.5f, runtime.GetVisualStatus(), 0.001f,
            "�� �������� �������� GetVisualStatus должен быть 0.5");

        Object.Destroy(go); Object.Destroy(ability);
    }

    [Test]
    public void Invis_GetVisualStatus_OneWhenZeroDelay()
    {
        var (ability, runtime, rb, go) = CreateInvisSetup(activationDelay: 0f);
        runtime.Run();

        Assert.AreEqual(1f, runtime.GetVisualStatus(), 0.001f,
            "��� activationDelay = 0 GetVisualStatus ������ ����� ���������� 1");

        Object.Destroy(go); Object.Destroy(ability);
    }
}