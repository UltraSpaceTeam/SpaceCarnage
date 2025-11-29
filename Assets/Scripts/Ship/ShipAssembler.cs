using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShipAssembler : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private Transform shipRoot;

    public HullData CurrentHull { get; private set; }
    public WeaponData CurrentWeapon { get; private set; }
    public EngineData CurrentEngine { get; private set; }

    private GameObject _currentHullObject;
    private List<PartSocket> _activeSockets = new List<PartSocket>();

    public void EquipHull(HullData newHullData)
    {
        if (newHullData == null || newHullData.prefab == null) return;

        if (_currentHullObject != null)
        {
            CleanUpObject(_currentHullObject);
        }

        _currentHullObject = Instantiate(newHullData.prefab, shipRoot);
        _currentHullObject.transform.localPosition = Vector3.zero;
        _currentHullObject.transform.localRotation = Quaternion.identity;

        CurrentHull = newHullData;

        _activeSockets = _currentHullObject.GetComponentsInChildren<PartSocket>(true).ToList();

        if (CurrentWeapon != null) EquipWeapon(CurrentWeapon);
        if (CurrentEngine != null) EquipEngine(CurrentEngine);
    }
    public void EquipWeapon(WeaponData weaponData)
    {
        CurrentWeapon = weaponData;
        if (_activeSockets == null || _activeSockets.Count == 0) return;

        AttachPartToSocket(weaponData, PartType.Weapon);
    }

    public void EquipEngine(EngineData engineData)
    {
        CurrentEngine = engineData;
        if (_activeSockets == null || _activeSockets.Count == 0) return;

        AttachPartToSocket(engineData, PartType.Engine);
    }

    private void AttachPartToSocket(ShipPartData partData, PartType type)
    {
        PartSocket targetSocket = _activeSockets.FirstOrDefault(s => s.socketType == type);

        if (targetSocket != null)
        {
            for (int i = targetSocket.transform.childCount - 1; i >= 0; i--)
            {
                CleanUpObject(targetSocket.transform.GetChild(i).gameObject);
            }

            if (partData != null && partData.prefab != null)
            {
                GameObject newPart = Instantiate(partData.prefab, targetSocket.transform);
                newPart.transform.localPosition = Vector3.zero;
                newPart.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void CleanUpObject(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        obj.transform.SetParent(null);

        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}