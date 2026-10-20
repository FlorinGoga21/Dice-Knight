using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds a combat room's enemy lineup, or recreates its exact saved lineup.</summary>
public sealed class EnemySpawner : MonoBehaviour
{
    [Serializable]
    private sealed class EnemyOption
    {
        public string id;
        public EnemyController prefab;
    }

    [Header("Enemy Pool")]
    [SerializeField] private EnemyOption[] enemyPool;
    [Tooltip("Keep this ID unchanged so saved enemies can find this spawner.")]
    [SerializeField] private string spawnerId = "main-room";
    [Tooltip("Prefab ID to use when loading saves made before the spawner existed.")]
    [SerializeField] private string legacyEnemyId = "skeleton";

    [Header("Room Size")]
    [SerializeField, Range(1, 20)] private int minimumEnemies = 2;
    [SerializeField, Range(1, 20)] private int maximumEnemies = 3;
    [Tooltip("Skip shops, treasure rooms, and random rooms. Opening the scene directly still spawns enemies.")]
    [SerializeField] private bool combatRoomsOnly = true;

    [Header("Placement")]
    [Tooltip("The spawner's position is the first enemy position; remaining enemies are placed to its right.")]
    [SerializeField, Min(1f)] private float enemySpacing = 4f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0.1f)] private float groundProbeHeight = 5f;
    [SerializeField, Min(0.1f)] private float groundProbeDistance = 15f;

    private readonly List<EnemyStats> spawnedEnemies = new List<EnemyStats>();
    private bool initialized;

    public IReadOnlyList<EnemyStats> SpawnedEnemies => spawnedEnemies;
    public string SpawnerId => spawnerId;

    private void Start()
    {
        // SaveManager normally initializes us before restoring the room.
        if (!initialized) SpawnRoom(null);
    }

    public void SpawnRoom(RoomSaveData savedRoom)
    {
        if (initialized) return;
        ValidateEnemyPool();
        initialized = true;

        if (savedRoom != null)
        {
            RestoreLineup(savedRoom);
            return;
        }

        var roomType = RunMapState.CurrentRoomType;
        if (combatRoomsOnly && roomType.HasValue
            && roomType != RunMapState.RoomType.Enemy && roomType != RunMapState.RoomType.Boss)
            return;

        int count = UnityEngine.Random.Range(minimumEnemies, maximumEnemies + 1);
        var choices = new List<EnemyOption>(enemyPool);
        Shuffle(choices);
        for (int i = 0; i < count; i++)
        {
            EnemyOption option = choices[i % choices.Count];
            Vector3 position = transform.position + Vector3.right * (i * enemySpacing);
            EnemyStats enemy = Spawn(option, position, Quaternion.identity, spawnerId + ":" + i);
            enemy.GetComponent<EnemyWeaponVariation>()?.RandomizeWeapon();
            PlaceOnGround(enemy);
            enemy.GetComponent<EnemyFlight>()?.BeginHover();
        }
    }

    private void RestoreLineup(RoomSaveData savedRoom)
    {
        foreach (EnemySaveData saved in savedRoom.enemies)
        {
            bool legacy = string.IsNullOrEmpty(saved.spawnerId);
            if (!legacy && saved.spawnerId != spawnerId) continue;
            string prefabId = legacy ? legacyEnemyId : saved.prefabId;
            EnemyOption option = Array.Find(enemyPool, item => item.id == prefabId);
            if (option == null)
                throw new InvalidOperationException($"Enemy spawner '{spawnerId}' cannot restore missing prefab ID '{prefabId}'.");

            EnemyStats enemy = Spawn(option, saved.position, saved.rotation, saved.id);
            enemy.GetComponent<EnemyWeaponVariation>()?.RestoreWeapon(saved.weaponId);
        }
    }

    private EnemyStats Spawn(EnemyOption option, Vector3 position, Quaternion rotation, string saveId)
    {
        EnemyController controller = Instantiate(option.prefab, position, rotation, transform);
        controller.name = option.prefab.name + " " + (spawnedEnemies.Count + 1);
        EnemyStats enemy = controller.GetComponent<EnemyStats>();
        enemy.SetSpawnIdentity(saveId, spawnerId, option.id);
        spawnedEnemies.Add(enemy);
        return enemy;
    }

    private void PlaceOnGround(EnemyStats enemy)
    {
        if (groundLayer.value == 0) return;
        Vector3 origin = enemy.transform.position + Vector3.up * groundProbeHeight;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundProbeDistance, groundLayer);
        if (!hit) return;

        Physics2D.SyncTransforms();
        Collider2D body = enemy.GetComponent<Collider2D>();
        float bottomOffset = enemy.transform.position.y - body.bounds.min.y;
        Vector3 position = enemy.transform.position;
        position.y = hit.point.y + bottomOffset + 0.02f;
        enemy.transform.position = position;
        if (enemy.TryGetComponent(out Rigidbody2D rigidbody)) rigidbody.position = position;
    }

    private void ValidateEnemyPool()
    {
        if (string.IsNullOrWhiteSpace(spawnerId) || enemyPool == null || enemyPool.Length == 0)
            throw new InvalidOperationException($"{name}: assign a spawner ID and at least one enemy prefab.");

        var ids = new HashSet<string>();
        foreach (EnemyOption option in enemyPool)
        {
            if (option == null || string.IsNullOrWhiteSpace(option.id) || option.prefab == null || !ids.Add(option.id))
                throw new InvalidOperationException($"{name}: each enemy needs a unique ID and a prefab.");
        }
    }

    private static void Shuffle(List<EnemyOption> choices)
    {
        for (int i = choices.Count - 1; i > 0; i--)
        {
            int other = UnityEngine.Random.Range(0, i + 1);
            EnemyOption swap = choices[i];
            choices[i] = choices[other];
            choices[other] = swap;
        }
    }

    private void OnValidate()
    {
        minimumEnemies = Mathf.Clamp(minimumEnemies, 1, 20);
        maximumEnemies = Mathf.Clamp(maximumEnemies, minimumEnemies, 20);
        enemySpacing = Mathf.Max(1f, enemySpacing);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.8f);
        for (int i = 0; i < maximumEnemies; i++)
            Gizmos.DrawWireSphere(transform.position + Vector3.right * (i * enemySpacing), 0.3f);
    }
}
