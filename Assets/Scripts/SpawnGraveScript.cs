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

    [Header("Layout Replication")]
    [Tooltip("If true, treat the objects in 'sceneObjectsToCopy' as a layout group and spawn the whole layout at each grave.")]
    public bool spawnLayoutAsGroup = false;

    [Tooltip("Root Transform of the source layout in the scene. Positions/rotations of sceneObjectsToCopy are taken relative to this root.")]
    public Transform layoutRoot;

    [Tooltip("If true, preserve each layout object's rotation relative to the layout root when replicating at each grave. If false, spawned objects will align to the grave's forward.")]
    public bool preserveLayoutRotation = true;

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
    private GameObject[] _parentContainers;

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

        if (spawnLayoutAsGroup && layoutRoot == null)
        {
            Debug.LogWarning("SpawnGraveScript: 'spawnLayoutAsGroup' is enabled but no 'layoutRoot' assigned.");
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


        // If replicating a whole layout: for each grave spawn all sourceList items preserving their local positions relative to layoutRoot
        if (spawnLayoutAsGroup)
        {
            for (int gi = 0; gi < graves.Length; gi++)
            {
                var grave = graves[gi];
                if (grave == null) continue;

                GameObject _parentContainer = new GameObject("SpawnedLayout_" + grave.name);
                _parentContainer.transform.SetParent(grave.transform, true);
                _parentContainers[gi] = _parentContainer;

                Vector3 forwardWorld = grave.TransformDirection(Vector3.forward);
                Vector3 offsetWorld = grave.TransformDirection(localOffset);

                for (int si = 0; si < sourceList.Length; si++)
                {
                    var source = sourceList[si];
                    if (source == null) continue;

                    // compute source local position relative to layoutRoot (world space -> layout local)
                    Vector3 relativePos = layoutRoot.InverseTransformPoint(source.transform.position);
                    Vector3 spawnPos = grave.TransformPoint(relativePos) + forwardWorld * distanceInFront + offsetWorld;

                    // compute rotation relative to layoutRoot, then apply grave rotation
                    Quaternion relativeRot = Quaternion.Inverse(layoutRoot.rotation) * source.transform.rotation;
                    Quaternion spawnRot = preserveLayoutRotation ? (grave.rotation * relativeRot) : Quaternion.LookRotation(forwardWorld, grave.up);

                    GameObject go = null;

#if UNITY_EDITOR
                    if (!Application.isPlaying && copySceneObjects)
                    {
                        if (PrefabUtility.IsPartOfPrefabAsset(source))
                        {
                            var obj = PrefabUtility.InstantiatePrefab(source) as GameObject;
                            if (obj != null)
                            {
                                go = obj;
                                go.transform.position = spawnPos;
                                go.transform.rotation = spawnRot;
                                go.transform.SetParent(_parentContainer.transform, true);
                                Undo.RegisterCreatedObjectUndo(go, "Spawn layout copy at grave");
                                EditorUtility.SetDirty(go);
                            }
                        }
                        else
                        {
                            go = Object.Instantiate(source, spawnPos, spawnRot);
                            go.transform.SetParent(_parentContainer.transform, true);
                            Undo.RegisterCreatedObjectUndo(go, "Spawn layout copy at grave");
                            EditorUtility.SetDirty(go);
                        }
                    }
                    else
#endif
                    {
                        go = Instantiate(source, spawnPos, spawnRot, _parentContainer.transform);
                    }

                    if (go == null) continue;
                    go.name = source.name + "_at_" + grave.name;
                    _spawned.Add(go);
                }
            }

            return;
        }

        // Default behavior: spawn one object per grave
        for (int i = 0; i < graves.Length; i++)
        {
            var grave = graves[i];
            if (grave == null) continue;

            GameObject _parentContainer = new GameObject("SpawnedLayout_" + grave.name);
            _parentContainer.transform.SetParent(grave.transform, true);
            _parentContainers[i] = _parentContainer;

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

#if UNITY_EDITOR
            if (!Application.isPlaying && copySceneObjects)
            {
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

        if (_parentContainers != null)
        {
            for (int i = 0; i < _parentContainers.Length; i++)
            {
                var _parentContainer = _parentContainers[i];

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
        }
    }  

    private Transform[] ResolveGraves()
    {
        if (useTag)
        {
            var gos = GameObject.FindGameObjectsWithTag(graveTag);
            Transform[] result = new Transform[gos.Length];
            for (int i = 0; i < gos.Length; i++) result[i] = gos[i].transform;

            if (_parentContainers == null || _parentContainers.Length != result.Length)
            {
                _parentContainers = new GameObject[result.Length];
            }

            return result;
        }
        else
        {
            return manualGraves ?? new Transform[0];
        }
    }
}
