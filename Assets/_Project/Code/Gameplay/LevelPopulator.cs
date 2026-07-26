using System.Collections.Generic;
using System.Linq;
using CoreUtils.AssetBuckets;
using UnityEngine;
using _Project.Code.Gameplay.PlayerController;

/// <summary>
/// Scatters victims and hazards along the level at Start, probing downward for solid ground so
/// nothing lands over a hole. The walkway is not continuous, so a fixed spacing rule alone would
/// drop half the level into empty space.
///
/// Put this on an empty GameObject in the scene. Seeded, so a run can be reproduced exactly when
/// something goes wrong, which matters here because most bugs on this project have traced back to
/// placement rather than code.
/// </summary>
public class LevelPopulator : MonoBehaviour
{
    [Header("Spawn Pool")]
    [Tooltip("CoreUtils Prefab Bucket pointed at a folder of victims. Everything in it is filtered " +
             "to prefabs that actually carry a Victim component, so the source folder can be broad.")]
    [SerializeField] private PrefabBucket _victimBucket;

    [Tooltip("CoreUtils Prefab Bucket for hazards. Filtered to prefabs carrying an Obstacle " +
             "component, so anything without one is skipped rather than spawned as harmless " +
             "scenery. The source folder can be as broad as you like.")]
    [SerializeField] private PrefabBucket _hazardBucket;

    [Header("Range")]
    [Tooltip("Leftmost X to consider.")]
    [SerializeField] private float _startX = -164f;

    [Tooltip("Rightmost X to consider.")]
    [SerializeField] private float _endX = 176f;

    [Tooltip("Distance between candidate slots. Smaller is denser and slower to build.")]
    [SerializeField] private float _step = 4f;

    [Tooltip("The Z plane everything sits on. The player is frozen in Z, so this must match him.")]
    [SerializeField] private float _laneZ = 10.25f;

    [Header("Ground Probe")]
    [Tooltip("Height the probe starts from. Must be above the tallest ground.")]
    [SerializeField] private float _probeFromY = 20f;

    [Tooltip("How far down to probe.")]
    [SerializeField] private float _probeDistance = 40f;

    [Tooltip("What counts as ground. Set this to the Ground layer, not Everything, or props and " +
             "hazards will be treated as floor and things will spawn on top of each other.")]
    [SerializeField] private LayerMask _groundLayers;

    [Tooltip("Lifts spawns slightly off the surface. 0 sits them exactly on it.")]
    [SerializeField] private float _yOffset;

    [Header("Density")]
    [Range(0f, 1f)]
    [Tooltip("Chance a valid slot gets a victim.")]
    [SerializeField] private float _victimChance = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("Chance a valid slot gets a hazard, rolled only if no victim was placed.")]
    [SerializeField] private float _hazardChance = 0.2f;

    [Tooltip("Minimum X gap between any two spawned things, so hazards cannot stack into a wall.")]
    [SerializeField] private float _minSpacing = 6f;

    [Tooltip("Keeps the area around the player's start empty so the run does not open with a hit.")]
    [SerializeField] private float _clearRadiusAroundPlayer = 14f;

    [Header("Determinism")]
    [Tooltip("Off: the same layout every run, which is what you want while tuning.")]
    [SerializeField] private bool _randomiseSeed;

    [SerializeField] private int _seed = 12345;

    [Header("Housekeeping")]
    [Tooltip("Spawns are parented here. Leave empty to parent to this object.")]
    [SerializeField] private Transform _container;

    [Tooltip("Randomly face spawns left or right, for a little variety.")]
    [SerializeField] private bool _randomYFlip = true;

    [Tooltip("Logs a summary of what was placed and how many slots had no ground under them.")]
    [SerializeField] private bool _logSummary = true;

    private List<GameObject> _victims;
    private List<GameObject> _hazards;

    private void Start()
    {
        // Filter by component rather than trusting the folder. A bucket sourced at a whole prefab
        // tree would otherwise happily spawn the player as a victim, or a fence with no Obstacle
        // on it as a hazard, which reads in game as a hazard that does nothing.
        _victims = FilterBucket<Victim>(_victimBucket, "victim");
        _hazards = FilterBucket<Obstacle>(_hazardBucket, "hazard");

        if (_victims.Count == 0 && _hazards.Count == 0)
        {
            Debug.LogWarning("[LevelPopulator] Both pools are empty after filtering, so nothing " +
                             "will spawn. Check the buckets are assigned and populated.", this);
            return;
        }

        if (_groundLayers.value == 0)
        {
            Debug.LogError("[LevelPopulator] Ground Layers is empty, so every probe misses and " +
                           "nothing will spawn. Set it to the Ground layer.", this);
            return;
        }

        var rng = new System.Random(_randomiseSeed ? System.Environment.TickCount : _seed);
        Transform parent = _container != null ? _container : transform;

        Vector3 playerPos = Vector3.positiveInfinity;
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) playerPos = player.transform.position;

        int victims = 0, hazards = 0, noGround = 0, tooClose = 0, nearPlayer = 0;
        float lastSpawnX = float.NegativeInfinity;

        for (float x = _startX; x <= _endX; x += _step)
        {
            Vector3 from = new Vector3(x, _probeFromY, _laneZ);
            if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, _probeDistance,
                                 _groundLayers, QueryTriggerInteraction.Ignore))
            {
                noGround++;
                continue;
            }

            if (x - lastSpawnX < _minSpacing) { tooClose++; continue; }

            if (Mathf.Abs(x - playerPos.x) < _clearRadiusAroundPlayer) { nearPlayer++; continue; }

            // Victims are rolled first, so a slot that could hold either becomes a victim. Hazards
            // are the punishment, and a level that punishes more than it rewards stops being fun.
            GameObject prefab = null;
            bool isVictim = false;
            if (Roll(rng) < _victimChance && _victims.Count > 0)
            {
                prefab = _victims[rng.Next(_victims.Count)];
                isVictim = true;
            }
            else if (Roll(rng) < _hazardChance && _hazards.Count > 0)
            {
                prefab = _hazards[rng.Next(_hazards.Count)];
            }

            if (prefab == null) continue;

            Vector3 pos = new Vector3(x, hit.point.y + _yOffset, _laneZ);
            Quaternion rot = prefab.transform.rotation;
            if (_randomYFlip && rng.Next(2) == 0) rot *= Quaternion.Euler(0f, 180f, 0f);

            Instantiate(prefab, pos, rot, parent);
            lastSpawnX = x;
            if (isVictim) victims++; else hazards++;
        }

        if (_logSummary)
            Debug.Log($"[LevelPopulator] placed {victims} victims and {hazards} hazards. " +
                      $"Skipped: {noGround} slots with no ground, {tooClose} too close together, " +
                      $"{nearPlayer} inside the player's clear radius. Seed {(_randomiseSeed ? "random" : _seed.ToString())}.", this);
    }

    private static float Roll(System.Random rng) => (float)rng.NextDouble();

    /// <summary>
    /// Pulls a bucket's contents and keeps only prefabs carrying the required component. Searches
    /// children too, because on these prefab variants the gameplay component often sits on a child
    /// rather than the root.
    /// </summary>
    private List<GameObject> FilterBucket<T>(PrefabBucket bucket, string label) where T : Component
    {
        var kept = new List<GameObject>();

        if (bucket == null)
        {
            Debug.LogWarning($"[LevelPopulator] No {label} bucket assigned.", this);
            return kept;
        }

        var skipped = new List<string>();
        foreach (GameObject go in bucket.Items)
        {
            if (go == null) continue;
            if (go.GetComponentInChildren<T>(true) == null) { skipped.Add(go.name); continue; }
            kept.Add(go);
        }

        if (_logSummary)
        {
            Debug.Log($"[LevelPopulator] {label} pool: {kept.Count} of {bucket.Items.Length} " +
                      $"prefabs in '{bucket.name}' carry {typeof(T).Name}.", this);
            if (skipped.Count > 0)
                Debug.Log($"[LevelPopulator] skipped {skipped.Count} without {typeof(T).Name}: " +
                          string.Join(", ", skipped.Take(12)) + (skipped.Count > 12 ? ", ..." : ""), this);
        }

        return kept;
    }

    // Draws the lane and the probe range so the range can be checked against the level without
    // entering play mode.
    private void OnDrawGizmosSelected()
    {
        Vector3 a = new Vector3(_startX, _probeFromY - _probeDistance, _laneZ);
        Vector3 b = new Vector3(_endX, _probeFromY - _probeDistance, _laneZ);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(new Vector3(_startX, _probeFromY, _laneZ), a);
        Gizmos.DrawLine(new Vector3(_endX, _probeFromY, _laneZ), b);

        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        for (float x = _startX; x <= _endX; x += _step)
            Gizmos.DrawLine(new Vector3(x, _probeFromY, _laneZ),
                            new Vector3(x, _probeFromY - _probeDistance, _laneZ));
    }
}
