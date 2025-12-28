using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InvisManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnVisibleChanged))]
    private bool isVisible = true;

    private List<Renderer> shipRenderers = new List<Renderer>();
    private ShipAssembler assembler;

    private void Awake()
    {
        assembler = GetComponent<ShipAssembler>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateRendererList();
        ApplyVisibility(isVisible);
    }

    [Server]
    public void SetVisible(bool visible)
    {
        if (!isServer)
        {
            Debug.LogWarning("SetVisible called on client! Only server should change visibility.");
            return;
        }

        if (this == null || gameObject == null)
        {
            Debug.LogWarning("InvisManager: trying to set visibility on destroyed object!");
            return;
        }

        isVisible = visible;
    }

    private void OnVisibleChanged(bool oldValue, bool newValue)
    {
        ApplyVisibility(newValue);
    }

    private void ApplyVisibility(bool visible)
    {
        UpdateRendererList();
        foreach (var renderer in shipRenderers)
        {
            if (renderer == null) continue;
            renderer.enabled = visible;
        }
        Debug.Log("Applying visibility: " + visible);
    }

    private void UpdateRendererList()
    {
        shipRenderers.Clear();
        if (assembler != null && assembler.CurrentHullObject != null)
        {
            shipRenderers = assembler.CurrentHullObject.GetComponentsInChildren<Renderer>(true).ToList();
        }

        shipRenderers.RemoveAll(r => r is LineRenderer || r is TrailRenderer || r is ParticleSystemRenderer);
    }


    public void RefreshRenderers()
    {
        UpdateRendererList();
        ApplyVisibility(isVisible);
    }
}