#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mirror;
using System.Linq;

public class SpawnPointScatterer : EditorWindow
{
    private int count = 20;
    private float radius = 60f;
    private float innerRadius = 5f;
    private float minDistance = 6f;
    private float surfaceBias = 0.0f;
    private bool useFullSphere = true;
    private bool clearExisting = true;
    private string parentName = "SpawnPoints";

    [MenuItem("Tools/Spawn Points/Scatter SpawnPoints")]
    public static void Open()
    {
        GetWindow<SpawnPointScatterer>("SpawnPoints Scatter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Scatter SpawnPoints (Mirror NetworkStartPosition)", EditorStyles.boldLabel);

        count = EditorGUILayout.IntField("Count", Mathf.Max(1, count));
        radius = EditorGUILayout.FloatField("Radius", Mathf.Max(0.1f, radius));
        innerRadius = EditorGUILayout.FloatField("Inner Radius", Mathf.Max(0f, innerRadius));
        minDistance = EditorGUILayout.FloatField("Min Distance", Mathf.Max(0f, minDistance));
        surfaceBias = EditorGUILayout.Slider("Surface Bias", surfaceBias, 0f, 1f);

        useFullSphere = EditorGUILayout.Toggle("Use Full Sphere (3D)", useFullSphere);
        clearExisting = EditorGUILayout.Toggle("Clear Existing Under Parent", clearExisting);

        parentName = EditorGUILayout.TextField("Parent Object Name", parentName);

        GUILayout.Space(10);

        if (GUILayout.Button("Generate"))
        {
            Generate();
        }

        if (GUILayout.Button("Clear Only"))
        {
            Clear();
        }
    }

    private void Generate()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("No valid scene open.");
            return;
        }

        Transform parent = GetOrCreateParent(parentName);

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        if (clearExisting)
            ClearChildren(parent);

        var points = new System.Collections.Generic.List<Vector3>();
        int maxAttempts = count * 200;

        for (int i = 0; i < count; i++)
        {
            bool placed = false;
            for (int a = 0; a < maxAttempts; a++)
            {
                Vector3 p = SamplePosition(radius, innerRadius, surfaceBias, useFullSphere);

                if (points.All(x => (x - p).sqrMagnitude >= (minDistance * minDistance)))
                {
                    points.Add(p);
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                Debug.LogWarning($"Could not place spawn #{i} with minDistance={minDistance}. Try lowering minDistance or count.");
                points.Add(SamplePosition(radius, innerRadius, surfaceBias, useFullSphere));
            }
        }

        for (int i = 0; i < points.Count; i++)
        {
            var go = new GameObject($"SpawnPoint_{i:00}");
            Undo.RegisterCreatedObjectUndo(go, "Create SpawnPoint");
            go.transform.SetParent(parent, false);
            go.transform.position = parent.position + points[i];
            go.transform.rotation = Random.rotation;

            Undo.AddComponent<NetworkStartPosition>(go);
        }

        RebuildMirrorStartPositions();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"Generated {count} spawnpoints under '{parentName}'.");
    }

    private void Clear()
    {
        Transform parent = GameObject.Find(parentName)?.transform;
        if (parent == null)
        {
            Debug.LogWarning($"Parent '{parentName}' not found.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        ClearChildren(parent);
        RebuildMirrorStartPositions();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"Cleared spawnpoints under '{parentName}'.");
    }

    private static Transform GetOrCreateParent(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing.transform;

        var parent = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(parent, "Create SpawnPoints Parent");
        parent.transform.position = Vector3.zero;
        return parent.transform;
    }

    private static void ClearChildren(Transform parent)
    {
        var children = parent.Cast<Transform>().ToArray();
        foreach (var c in children)
        {
            Undo.DestroyObjectImmediate(c.gameObject);
        }
    }

    private static void RebuildMirrorStartPositions()
    {
        var nm = Object.FindAnyObjectByType<NetworkManager>();
        if (nm == null)
        {
            Debug.LogWarning("NetworkManager not found in scene. startPositions not rebuilt.");
            return;
        }

        NetworkManager.startPositions.Clear();
        foreach (var sp in Object.FindObjectsByType<NetworkStartPosition>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            NetworkManager.RegisterStartPosition(sp.transform);
        }

        Debug.Log($"Mirror startPositions rebuilt. Count={NetworkManager.startPositions.Count}");
    }

    private static Vector3 SamplePosition(float radius, float innerRadius, float surfaceBias, bool fullSphere)
    {
        float u = Random.value;
        float rVol = Mathf.Lerp(innerRadius, radius, Mathf.Pow(u, 1f / 3f));
        float rSurf = Mathf.Lerp(innerRadius, radius, u);
        float r = Mathf.Lerp(rVol, rSurf, surfaceBias);

        if (fullSphere)
        {
            return Random.onUnitSphere * r;
        }
        else
        {
            Vector2 v = Random.insideUnitCircle.normalized * r;
            return new Vector3(v.x, 0f, v.y);
        }
    }
}
#endif
