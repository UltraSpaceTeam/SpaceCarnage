using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.VisualScripting;

public class ShipAssembler : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private Transform shipRoot;

    public HullData CurrentHull { get; private set; }
    public GameObject CurrentHullObject;
    public WeaponData CurrentWeapon { get; private set; }
    public GameObject CurrentWeaponObject { get; private set; }
    public EngineData CurrentEngine { get; private set; }
    public GameObject CurrentEngineObject { get; private set; }

    private List<PartSocket> _activeSockets = new List<PartSocket>();
    public event Action<HullData> OnHullEquipped;
    public event Action<GameObject> OnEngineEquipped;

    public void EquipHull(HullData newHullData)
    {
        if (newHullData == null || newHullData.prefab == null) return;

        if (CurrentHullObject != null)
        {
            CleanUpObject(CurrentHullObject);
        }

        CurrentHullObject = Instantiate(newHullData.prefab, shipRoot);
        CurrentHullObject.transform.localPosition = Vector3.zero;
        CurrentHullObject.transform.localRotation = Quaternion.identity;

        CurrentHull = newHullData;

        _activeSockets = CurrentHullObject.GetComponentsInChildren<PartSocket>(true).ToList();

        OnHullEquipped?.Invoke(newHullData);

        if (CurrentWeapon != null) EquipWeapon(CurrentWeapon);
        if (CurrentEngine != null) EquipEngine(CurrentEngine);
    }
    public void EquipWeapon(WeaponData weaponData)
    {
        CurrentWeapon = weaponData;
        if (_activeSockets == null || _activeSockets.Count == 0) return;

        AttachWeaponToSocket(weaponData);
    }

    public void EquipEngine(EngineData engineData)
    {
        if (CurrentEngine?.ability != null)
        {
            CurrentEngine.ability.OnUnequipped();
        }

        CurrentEngine = engineData;
        if (_activeSockets == null || _activeSockets.Count == 0) return;

        GameObject newEngineObj = AttachPartToSocket(engineData, PartType.Engine);

        if (newEngineObj != null)
        {
            CurrentEngineObject = newEngineObj;
            OnEngineEquipped?.Invoke(newEngineObj);
        }
    }

    private GameObject AttachPartToSocket(ShipPartData partData, PartType type)
    {
        PartSocket targetSocket = _activeSockets.FirstOrDefault(s => s.socketType == type);

        GameObject newPart = null;
        if (targetSocket != null)
        {
            for (int i = targetSocket.transform.childCount - 1; i >= 0; i--)
            {
                CleanUpObject(targetSocket.transform.GetChild(i).gameObject);
            }

            if (partData != null && partData.prefab != null)
            {
                newPart = Instantiate(partData.prefab, targetSocket.transform);
                newPart.transform.localPosition = Vector3.zero;
                newPart.transform.localRotation = Quaternion.identity;

                CurrentEngineObject = newPart;
            }
        }

        return newPart;
    }

    private void AttachWeaponToSocket(WeaponData partData)
    {
        PartSocket targetSocket = _activeSockets.FirstOrDefault(s => s.socketType == PartType.Weapon);

        if (targetSocket != null)
        {
            for (int i = targetSocket.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(targetSocket.transform.GetChild(i).gameObject);
            }

            if (partData != null && partData.prefab != null)
            {
                GameObject newPart = Instantiate(partData.prefab, targetSocket.transform);
                newPart.transform.localPosition = Vector3.zero;
                newPart.transform.localRotation = Quaternion.identity;

                CurrentWeaponObject = newPart;
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

    public void StopEngineParticles()
    {
        if (CurrentEngineObject == null) return;

        var particles = this.CurrentEngineObject.GetComponentsInChildren<ParticleSystem>(true).ToList();
        foreach (var ps in particles)
        {
            if (ps == null) continue;

            var emission = ps.emission;
            emission.enabled = false;
            ps.Stop();
        }
    }

    public void StartEngineParticles()
    {
        if (CurrentEngineObject == null) return;

        var particles = this.CurrentEngineObject.GetComponentsInChildren<ParticleSystem>(true).ToList();
        foreach (var ps in particles)
        {
            if (ps == null) continue;

            var emission = ps.emission;
            emission.enabled = true;
            ps.Play();
        }
    }
}