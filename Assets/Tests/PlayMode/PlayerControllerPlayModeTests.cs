using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Reflection;
using Mirror;

public class PlayerControllerPlayModeTests
{
    private GameObject go;
    private PlayerController pc;
    private Rigidbody rb;
    private ShipAssembler assembler;
    private Health health;

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        go = new GameObject("TestPlayerCtrl");
        go.tag = "Player";

        rb = go.AddComponent<Rigidbody>();
        assembler = go.AddComponent<ShipAssembler>();
        health = go.AddComponent<Health>();

        pc = go.AddComponent<PlayerController>();

        pc.enabled = false;

        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        if (go != null) Object.DestroyImmediate(go);
    }

    [Test]
    public void Awake_InitializesRequiredReferences()
    {
        MethodInfo awake = typeof(PlayerController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        awake?.Invoke(pc, null);

        var rbField = GetPrivateField<Rigidbody>("rb");
        var assemblerField = GetPrivateField<ShipAssembler>("shipAssembler");

        Assert.IsNotNull(rbField, "Rigidbody должен быть найден");
        Assert.IsNotNull(assemblerField, "ShipAssembler должен быть найден");
    }

    [Test]
    public void Start_SetsDefaultValues_IfAny()
    {
        MethodInfo start = typeof(PlayerController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
        start?.Invoke(pc, null);

        Assert.AreEqual(0f, pc.CurrentThrustOutput, "Тяга по умолчанию должна быть 0");
    }

    [Test]
    public void ServerAbsorbDamage_ReturnsSameValue()
    {
        float input = 47.3f;
        float output = pc.ServerAbsorbDamage(input);
        Assert.AreEqual(input, output, "Должен возвращать входное значение без изменений");
    }

    [Test]
    public void ServerNotifyDamaged_CallsWithoutError()
    {
        Assert.DoesNotThrow(() => pc.ServerNotifyDamaged());
    }

    [Test]
    public void ServerNotifyAttacked_CallsWithoutError()
    {
        Assert.DoesNotThrow(() => pc.ServerNotifyAttacked());
    }

    [Test]
    public void CurrentThrustOutput_CanBeReadAndWritten()
    {
        pc.CurrentThrustOutput = 0.82f;
        Assert.AreEqual(0.82f, pc.CurrentThrustOutput, "SyncVar должен корректно хранить значение");
    }

    [Test]
    public void InputFields_CanBeSetViaReflection()
    {
        SetPrivateField("thrustInput", 0.7f);
        SetPrivateField("rollInput", -0.4f);
        SetPrivateField("aimTargetInput", new Vector2(0.3f, -0.6f));

        Assert.AreEqual(0.7f, GetPrivateField<float>("thrustInput"));
        Assert.AreEqual(-0.4f, GetPrivateField<float>("rollInput"));
        Assert.AreEqual(new Vector2(0.3f, -0.6f), GetPrivateField<Vector2>("aimTargetInput"));
    }

    private T GetPrivateField<T>(string fieldName)
    {
        var field = typeof(PlayerController).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Поле {fieldName} не найдено");
        return (T)field.GetValue(pc);
    }

    private void SetPrivateField(string fieldName, object value)
    {
        var field = typeof(PlayerController).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Поле {fieldName} не найдено");
        field.SetValue(pc, value);
    }
}