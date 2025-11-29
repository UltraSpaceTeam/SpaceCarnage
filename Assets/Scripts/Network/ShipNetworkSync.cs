using UnityEngine;
using Mirror;

[RequireComponent(typeof(ShipAssembler))]
public class ShipNetworkSync : NetworkBehaviour
{
    private ShipAssembler _assembler;

    [SyncVar(hook = nameof(OnHullChanged))]
    private int _hullIndex = -1;

    [SyncVar(hook = nameof(OnWeaponChanged))]
    private int _weaponIndex = -1;

    [SyncVar(hook = nameof(OnEngineChanged))]
    private int _engineIndex = -1;

    private void Awake()
    {
        _assembler = GetComponent<ShipAssembler>();
    }

    public override void OnStartLocalPlayer()
    {
        int hIndex = 2;
        int wIndex = 3;
        int eIndex = 0;

        CmdSetupShip(hIndex, wIndex, eIndex);
    }

    private int GetIndex<T>(string id, System.Collections.Generic.List<T> list) where T : ShipPartData
    {
        return list.FindIndex(x => x.id == id);
    }

    [Command]
    private void CmdSetupShip(int hullIdx, int weaponIdx, int engineIdx)
    {
        _hullIndex = hullIdx;
        _weaponIndex = weaponIdx;
        _engineIndex = engineIdx;
    }


    private void OnHullChanged(int oldIndex, int newIndex)
    {
        if (newIndex < 0) return;
        var data = GameResources.Instance.partDatabase.hulls[newIndex];
        _assembler.EquipHull(data);
    }

    private void OnWeaponChanged(int oldIndex, int newIndex)
    {
        if (newIndex < 0) return;
        var data = GameResources.Instance.partDatabase.weapons[newIndex];
        _assembler.EquipWeapon(data);
    }

    private void OnEngineChanged(int oldIndex, int newIndex)
    {
        if (newIndex < 0) return;
        var data = GameResources.Instance.partDatabase.engines[newIndex];
        _assembler.EquipEngine(data);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (_hullIndex >= 0) OnHullChanged(-1, _hullIndex);
        if (_weaponIndex >= 0) OnWeaponChanged(-1, _weaponIndex);
        if (_engineIndex >= 0) OnEngineChanged(-1, _engineIndex);
    }
}