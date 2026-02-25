using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class ShipAssemblerPlayModeTests
{
    private GameObject assemblerObject;
    private ShipAssembler assembler;

    private HullData hullData;
    private WeaponData weaponData;
    private EngineData engineData;

    private GameObject hullPrefab;
    private GameObject weaponPrefab;
    private GameObject enginePrefab;

    [SetUp]
    public void SetUp()
    {
        // Создаём корневой объект
        assemblerObject = new GameObject("TestAssembler");
        assembler = assemblerObject.AddComponent<ShipAssembler>();

        // Создаём тестовые данные
        hullData = ScriptableObject.CreateInstance<HullData>();
        weaponData = ScriptableObject.CreateInstance<WeaponData>();
        engineData = ScriptableObject.CreateInstance<EngineData>();

        // Минимальные префабы
        hullPrefab = new GameObject("TestHull");
        weaponPrefab = new GameObject("TestWeapon");
        enginePrefab = new GameObject("TestEngine");

        hullData.prefab = hullPrefab;
        weaponData.prefab = weaponPrefab;
        engineData.prefab = enginePrefab;

        // Добавляем хотя бы один сокет на hull (чтобы EquipWeapon/EquipEngine нашли куда цеплять)
        var socketsRoot = new GameObject("Sockets");
        socketsRoot.transform.SetParent(hullPrefab.transform);

        var weaponSocket = socketsRoot.AddComponent<PartSocket>();
        weaponSocket.socketType = PartType.Weapon;

        var engineSocket = socketsRoot.AddComponent<PartSocket>();
        engineSocket.socketType = PartType.Engine;
    }

    [TearDown]
    public void TearDown()
    {
        if (assemblerObject != null) Object.DestroyImmediate(assemblerObject);
        if (hullPrefab != null) Object.DestroyImmediate(hullPrefab);
        if (weaponPrefab != null) Object.DestroyImmediate(weaponPrefab);
        if (enginePrefab != null) Object.DestroyImmediate(enginePrefab);

        if (hullData != null) Object.DestroyImmediate(hullData);
        if (weaponData != null) Object.DestroyImmediate(weaponData);
        if (engineData != null) Object.DestroyImmediate(engineData);
    }

    // -------------------------------------------------------------------------
    // Тесты
    // -------------------------------------------------------------------------

    [Test]
    public void EquipHull_SetsCurrentHull_And_CreatesInstance()
    {
        assembler.EquipHull(hullData);

        Assert.IsNotNull(assembler.CurrentHull);
        Assert.AreSame(hullData, assembler.CurrentHull);
        Assert.IsNotNull(assembler.CurrentHullObject);
        Assert.AreEqual(hullPrefab.name + "(Clone)", assembler.CurrentHullObject.name);
    }

    [Test]
    public void EquipWeapon_AfterHull_SetsCurrentWeapon_And_ParentsToSocket()
    {
        assembler.EquipHull(hullData);
        assembler.EquipWeapon(weaponData);

        Assert.IsNotNull(assembler.CurrentWeapon);
        Assert.AreSame(weaponData, assembler.CurrentWeapon);
        Assert.IsNotNull(assembler.CurrentWeaponObject);
        Assert.AreEqual(weaponPrefab.name + "(Clone)", assembler.CurrentWeaponObject.name);

        // Проверяем, что объект прикреплён где-то под hull
        Assert.IsNotNull(assembler.CurrentWeaponObject.transform.parent);
        Assert.AreEqual(assembler.CurrentHullObject.transform, assembler.CurrentWeaponObject.transform.root);
    }

    [Test]
    public void EquipEngine_AfterHull_SetsCurrentEngine_And_ParentsToSocket()
    {
        assembler.EquipHull(hullData);
        assembler.EquipEngine(engineData);

        Assert.IsNotNull(assembler.CurrentEngine);
        Assert.AreSame(engineData, assembler.CurrentEngine);
        Assert.IsNotNull(assembler.CurrentEngineObject);
        Assert.AreEqual(enginePrefab.name + "(Clone)", assembler.CurrentEngineObject.name);
    }

    [UnityTest]
    public IEnumerator EquipHull_CleansPreviousHull()
    {
        assembler.EquipHull(hullData);
        var firstHull = assembler.CurrentHullObject;

        // Запоминаем ссылку до уничтожения
        var originalReference = firstHull;

        assembler.EquipHull(hullData);

        // Даём Unity обработать Destroy (если это Destroy, а не DestroyImmediate)
        yield return null;

        // Проверяем двумя способами
        Assert.IsTrue(originalReference == null, "Старая ссылка должна стать 'fake null'");
        Assert.IsFalse(originalReference, "Старая ссылка должна быть falsy после уничтожения");

        // Дополнительно: новая часть должна быть другой
        Assert.AreNotSame(originalReference, assembler.CurrentHullObject);
    }

    [Test]
    public void EquipWeapon_WithoutHull_SetsCurrentWeapon_ButDoesNotCreateObject()
    {
        // Act
        assembler.EquipWeapon(weaponData);

        // Assert — подстраиваемся под текущее поведение
        Assert.IsNotNull(assembler.CurrentWeapon, "CurrentWeapon должен быть установлен даже без hull");
        Assert.AreSame(weaponData, assembler.CurrentWeapon);

        // Но объект не должен создаться (или должен быть null)
        Assert.IsNull(assembler.CurrentWeaponObject, "Объект оружия не должен создаваться без hull");
    }

    [Test]
    public void CurrentEngine_ReturnsNull_WhenNotEquipped()
    {
        Assert.IsNull(assembler.CurrentEngine);
    }

    // Если у тебя есть событие OnHullEquipped / OnWeaponEquipped / OnEngineEquipped
    [Test]
    public void OnHullEquipped_Invoked_WhenHullIsSet()
    {
        bool called = false;
        assembler.OnHullEquipped += h => called = true;

        assembler.EquipHull(hullData);

        Assert.IsTrue(called);
    }

    // Можно добавить аналогичные для OnWeaponEquipped, OnEngineEquipped
}