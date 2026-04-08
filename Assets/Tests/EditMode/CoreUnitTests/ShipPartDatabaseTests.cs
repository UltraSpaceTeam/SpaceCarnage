using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ShipPartDatabaseTests
{
    private ShipPartDatabase _db;
    private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

    [SetUp]
    public void SetUp()
    {
        _db = ScriptableObject.CreateInstance<ShipPartDatabase>();
        _db.hulls = new List<HullData>();
        _db.weapons = new List<WeaponData>();
        _db.engines = new List<EngineData>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var o in _created)
        {
            if (o != null)
                UnityEngine.Object.DestroyImmediate(o);
        }

        if (_db != null)
            ScriptableObject.DestroyImmediate(_db);
    }

    [Test]
    public void GetPartByID_WhenNotFound_ReturnsNull()
    {
        Assert.IsNull(_db.GetPartByID("missing"));
    }

    [Test]
    public void GetPartByID_FindsHull()
    {
        var hull = ScriptableObject.CreateInstance<HullData>();
        hull.id = "h1";
        _created.Add(hull);
        _db.hulls.Add(hull);

        Assert.AreSame(hull, _db.GetPartByID("h1"));
    }

    [Test]
    public void GetPartByID_FindsWeapon()
    {
        var w = ScriptableObject.CreateInstance<WeaponData>();
        w.id = "w1";
        _created.Add(w);
        _db.weapons.Add(w);

        Assert.AreSame(w, _db.GetPartByID("w1"));
    }

    [Test]
    public void GetPartByID_FindsEngine()
    {
        var e = ScriptableObject.CreateInstance<EngineData>();
        e.id = "e1";
        _created.Add(e);
        _db.engines.Add(e);

        Assert.AreSame(e, _db.GetPartByID("e1"));
    }

    [Test]
    public void GetPartByID_WhenDuplicates_ReturnsFirstInOrder_HullsBeforeWeaponsBeforeEngines()
    {
        var hull = ScriptableObject.CreateInstance<HullData>();
        hull.id = "dup";
        var weapon = ScriptableObject.CreateInstance<WeaponData>();
        weapon.id = "dup";
        var engine = ScriptableObject.CreateInstance<EngineData>();
        engine.id = "dup";

        _created.Add(hull);
        _created.Add(weapon);
        _created.Add(engine);

        _db.hulls.Add(hull);
        _db.weapons.Add(weapon);
        _db.engines.Add(engine);

        Assert.AreSame(hull, _db.GetPartByID("dup"));
    }

    [Test]
    public void GetPartByID_WhenListsAreEmpty_ReturnsNull()
    {
        _db.hulls.Clear();
        _db.weapons.Clear();
        _db.engines.Clear();
        Assert.IsNull(_db.GetPartByID("any"));
    }

    [Test]
    public void GetPartByID_WhenAnyListIsNull_ThrowsArgumentNullException()
    {
        // Негативный тест: граничный случай для защиты от NRE в рантайме
        _db.hulls = null;
        Assert.Throws<ArgumentNullException>(() => _db.GetPartByID("x"));
    }
}
