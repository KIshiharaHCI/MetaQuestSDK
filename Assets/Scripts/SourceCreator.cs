using System;
using System.Collections.Generic;
using UnityEngine;

public class SourceCreator : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject prefab;

    [Tooltip("Minimum allowed distance between spawned objects.")]
    [Min(0f)]
    [SerializeField] private float minimumSpacing = 0.5f;

    [Tooltip("If > 0, spawns periodically at this component's position.")]
    [Min(0f)]
    [SerializeField] private float spawnIntervalSeconds = 0f;

    [Tooltip("Optional parent for spawned instances; leave empty for world-space parenting.")]
    [SerializeField] private Transform spawnParent;

    private readonly List<GameObject> _spawnedSources = new List<GameObject>();
    private float _minimumSpacingSquared;
    private float _timeSinceLastSpawn;

    /// <summary>
    /// Exposes a callback that attempts to create a source at the requested world position.
    /// Returns the spawned GameObject or null when no spawn occurs.
    /// </summary>
    public Func<Vector3, GameObject> CreateSource => TryCreateAt;

    /// <summary>
    /// Returns the list of spawned sources for external inspection.
    /// </summary>
    public IReadOnlyList<GameObject> SpawnedSources => _spawnedSources;

    private void Awake()
    {
        // Clamp and cache
        minimumSpacing = Mathf.Max(0f, minimumSpacing);
        spawnIntervalSeconds = Mathf.Max(0f, spawnIntervalSeconds);
        _minimumSpacingSquared = minimumSpacing * minimumSpacing;
        _timeSinceLastSpawn = 0f;

        if (prefab == null)
        {
            Debug.LogWarning($"{nameof(SourceCreator)} on {name} has no prefab assigned.");
        }
    }

    private void OnValidate()
    {
        minimumSpacing = Mathf.Max(0f, minimumSpacing);
        spawnIntervalSeconds = Mathf.Max(0f, spawnIntervalSeconds);
        _minimumSpacingSquared = minimumSpacing * minimumSpacing;
    }

    private void Update()
    {
        if (spawnIntervalSeconds <= 0f)
            return;

        _timeSinceLastSpawn += Time.deltaTime;
        if (_timeSinceLastSpawn < spawnIntervalSeconds)
            return;

        _timeSinceLastSpawn = 0f;
        // Periodically attempt to spawn at this component's world position
        TryCreateAt(transform.position);
    }

    /// <summary>
    /// Attempts to create a new instance of the prefab at the given world position.
    /// Respects the minimum spacing to existing instances.
    /// Returns the new instance or null when blocked.
    /// </summary>
    public GameObject TryCreateAt(Vector3 worldPosition)
    {
        if (prefab == null)
            return null;

        CleanupSpawnedList();

        // Reject if too close to an existing spawned instance
        for (int i = 0; i < _spawnedSources.Count; i++)
        {
            var existing = _spawnedSources[i];
            if (existing == null) continue;

            if ((existing.transform.position - worldPosition).sqrMagnitude <= _minimumSpacingSquared)
                return null;
        }

        // Respect optional parenting preference
        var instance = spawnParent == null
            ? Instantiate(prefab, worldPosition, Quaternion.identity)
            : Instantiate(prefab, worldPosition, Quaternion.identity, spawnParent);

        // Ensure the instance is active as requested
        if (instance != null)
            instance.SetActive(true);

        Debug.Log($"Spawned new source at {worldPosition}.");

        _spawnedSources.Add(instance);
        return instance;
    }

    private void CleanupSpawnedList()
    {
        for (int i = _spawnedSources.Count - 1; i >= 0; i--)
        {
            if (_spawnedSources[i] == null)
                _spawnedSources.RemoveAt(i);
        }
    }
}
