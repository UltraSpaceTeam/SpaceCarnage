using NUnit.Framework;

public class DamageContextTests
{
    // Классы эквивалентности: разные типы источников урона (Weapon / Collision / Suicide / Runaway)

    [Test]
    public void Suicide_Factory_SetsExpectedFields()
    {
        var ctx = DamageContext.Suicide("Player1");

        Assert.AreEqual(0u, ctx.AttackerId);
        Assert.AreEqual("Player1", ctx.AttackerName);
        Assert.AreEqual("Suicide", ctx.WeaponID);
        Assert.AreEqual(DamageType.Suicide, ctx.Type);
    }

    [Test]
    public void Environment_Factory_SetsExpectedFields()
    {
        var ctx = DamageContext.Environment("Asteroid");

        Assert.AreEqual(0u, ctx.AttackerId);
        Assert.AreEqual("Environment", ctx.AttackerName);
        Assert.AreEqual("Asteroid", ctx.WeaponID);
        Assert.AreEqual(DamageType.Collision, ctx.Type);
    }

    [Test]
    public void Weapon_Factory_SetsExpectedFields()
    {
        var ctx = DamageContext.Weapon(42u, "Attacker", "Laser");

        Assert.AreEqual(42u, ctx.AttackerId);
        Assert.AreEqual("Attacker", ctx.AttackerName);
        Assert.AreEqual("Laser", ctx.WeaponID);
        Assert.AreEqual(DamageType.Weapon, ctx.Type);
    }

    [Test]
    public void Collision_Factory_SetsExpectedFields()
    {
        var ctx = DamageContext.Collision(7u, "Ship", "Ram");

        Assert.AreEqual(7u, ctx.AttackerId);
        Assert.AreEqual("Ship", ctx.AttackerName);
        Assert.AreEqual("Ram", ctx.WeaponID);
        Assert.AreEqual(DamageType.Collision, ctx.Type);
    }

    [Test]
    public void Runaway_Factory_SetsExpectedFields()
    {
        var ctx = DamageContext.Runaway();

        Assert.AreEqual(0u, ctx.AttackerId);
        Assert.AreEqual("Space radiation", ctx.AttackerName);
        Assert.AreEqual("Zone", ctx.WeaponID);
        Assert.AreEqual(DamageType.Runaway, ctx.Type);
    }

    [Test]
    public void ToString_ContainsAttackerWeaponAndType()
    {
        var ctx = new DamageContext(1u, "A", "W", DamageType.Weapon);
        var s = ctx.ToString();

        StringAssert.Contains("A", s);
        StringAssert.Contains("W", s);
        StringAssert.Contains(DamageType.Weapon.ToString(), s);
    }

    [Test]
    public void ToString_WhenAttackerNameIsNull_DoesNotThrow()
    {
        var ctx = new DamageContext(1u, null, "W", DamageType.Weapon);

        Assert.DoesNotThrow(() => ctx.ToString());
    }

    [Test]
    public void Weapon_Factory_AllowsEmptyWeaponId()
    {
        var ctx = DamageContext.Weapon(1u, "A", "");
        Assert.AreEqual("", ctx.WeaponID);
    }

    [Test]
    public void Constructor_SetsFieldsExactly()
    {
        var ctx = new DamageContext(uint.MaxValue, "X", "Y", DamageType.Generic);

        Assert.AreEqual(uint.MaxValue, ctx.AttackerId);
        Assert.AreEqual("X", ctx.AttackerName);
        Assert.AreEqual("Y", ctx.WeaponID);
        Assert.AreEqual(DamageType.Generic, ctx.Type);
    }
}
