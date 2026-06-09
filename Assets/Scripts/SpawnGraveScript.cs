using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpawnGraveScript : MonoBehaviour
{
    [Tooltip("Prefabs that will be spawned in front of each grave.")]
    public GameObject[] prefabsToSpawn;

    [Tooltip("Alternatively, copy these scene objects (exact copies) instead of using prefabs.")]
    public GameObject[] sceneObjectsToCopy;

    [Tooltip("If true the script will copy the scene objects in 'sceneObjectsToCopy' instead of using 'prefabsToSpawn'.")]
    public bool copySceneObjects = false;

    [Tooltip("Distance in local 'forward' direction of the grave where the prefab will be placed.")]
    public float distanceInFront = 1.0f;

    [Tooltip("Extra local-space offset from the grave (applied after the forward distance).")]
    public Vector3 localOffset = Vector3.zero;

    [Header("Grave Lookup")]
    [Tooltip("If true the script will find graves by tag. Otherwise assign grave Transforms manually.")]
    public bool useTag = true;

    [Tooltip("Tag used to identify graves when 'useTag' is true.")]
    public string graveTag = "Grave";

    [Tooltip("Optional: assign grave transforms manually when not using tag.")]
    public Transform[] manualGraves;

    [Header("Behavior")]
    [Tooltip("Spawn automatically at Start.")]
    public bool spawnOnStart = true;

    [Tooltip("Clear previously spawned objects created by this component before spawning.")]
    public bool clearExistingBeforeSpawn = true;

    [Tooltip("If true, each grave gets a randomly chosen source object from the list. Otherwise the first is used.")]
    public bool randomizePerGrave = true;

    // Keep references to spawned objects so we can remove them later if needed.
    private List<GameObject> _spawned = new List<GameObject>();
    private GameObject _parentContainer;

    void Start()
    {
        if (spawnOnStart)
            SpawnAll();
    }

    // Call from code or use the context menu in the inspector.
    [ContextMenu("Spawn All In Front Of Graves")]
    public void SpawnAll()
    {
        // Determine source lists
        GameObject[] sourceList = null;
        if (copySceneObjects)
            sourceList = sceneObjectsToCopy;
        else
            sourceList = prefabsToSpawn;

        if (sourceList == null || sourceList.Length == 0)
        {
            Debug.LogWarning("SpawnGraveScript: no source objects assigned to spawn/copy.");
            return;
        }

        Transform[] graves = ResolveGraves();
        if (graves == null || graves.Length == 0)
        {
            Debug.LogWarning("SpawnGraveScript: no grave Transforms found.");
            return;
        }

        if (clearExistingBeforeSpawn)
            ClearSpawned();

        // Create a parent container to keep hierarchy tidy
        if (_parentContainer == null)
        {
            _parentContainer = new GameObject("SpawnedAtGraves");
            _parentContainer.transform.SetParent(this.transform, false);
        }

        for (int i = 0; i < graves.Length; i++)
        {
            var grave = graves[i];
            if (grave == null) continue;

            // Compute spawn position: grave position + grave.forward * distance + local offset transformed into grave space
            Vector3 forwardWorld = grave.TransformDirection(Vector3.forward);
            Vector3 offsetWorld = grave.TransformDirection(localOffset);
            Vector3 spawnPos = grave.position + forwardWorld * distanceInFront + offsetWorld;

            // Align spawned object with the grave's forward direction by default
            Quaternion spawnRot = Quaternion.LookRotation(forwardWorld, grave.up);

            // Choose source object
            GameObject source = randomizePerGrave && sourceList.Length > 1
                ? sourceList[Random.Range(0, sourceList.Length)]
                : sourceList[0];

            if (source == null) continue;

            GameObject go = null;

            // If copying scene objects in the Editor (not playing), create scene duplicates with Undo support.
#if UNITY_EDITOR
            if (!Application.isPlaying && copySceneObjects)
            {
                // If the source is a prefab asset, instantiate using PrefabUtility so prefab connection is preserved
                if (PrefabUtility.IsPartOfPrefabAsset(source))
                {
                    var obj = PrefabUtility.InstantiatePrefab(source) as GameObject;
                    if (obj != null)
                    {
                        go = obj;
                        go.transform.position = spawnPos;
                        go.transform.rotation = spawnRot;
                        go.transform.SetParent(_parentContainer.transform, true);
                        Undo.RegisterCreatedObjectUndo(go, "Spawn copy at grave");
                        EditorUtility.SetDirty(go);
                    }
                }
                else
                {
                    // Duplicate the scene object instance
                    go = Object.Instantiate(source, spawnPos, spawnRot);
                    go.transform.SetParent(_parentContainer.transform, true);
                    Undo.RegisterCreatedObjectUndo(go, "Spawn copy at grave");
                    EditorUtility.SetDirty(go);
                }
            }
            else
#endif
            {
                // Runtime or using prefabs: normal Instantiate
                go = Instantiate(source, spawnPos, spawnRot, _parentContainer.transform);
            }

            if (go == null) continue;

            go.name = source.name + "_at_" + grave.name;
            _spawned.Add(go);
        }
    }

    [ContextMenu("Clear Spawned")]
    public void ClearSpawned()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.DestroyObjectImmediate(_spawned[i]);
                else
                    Destroy(_spawned[i]);
#else
                Destroy(_spawned[i]);
#endif
            }
        }
        _spawned.Clear();

        if (_parentContainer != null)
        {
            // If parent container is empty, remove it
            if (_parentContainer.transform.childCount == 0)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.DestroyObjectImmediate(_parentContainer);
                else
                    Destroy(_parentContainer);
#else
                Destroy(_parentContainer);
#endif
                _parentContainer = null;
            }
        }
    }

    private Transform[] ResolveGraves()
    {
        if (useTag)
        {
            var gos = GameObject.FindGameObjectsWithTag(graveTag);
            Transform[] result = new Transform[gos.Length];
            for (int i = 0; i < gos.Length; i++) result[i] = gos[i].transform;
            return result;
        }
        else
        {
            return manualGraves ?? new Transform[0];
        }
    }
}
