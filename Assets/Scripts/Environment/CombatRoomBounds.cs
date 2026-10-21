using System.Collections.Generic;
using HeyHeyThere.SceneTransitionsFree;
using UnityEngine;

/// <summary>Owns MainScene room edges and walk-off exits; combat rooms unlock after the encounter.</summary>
[DefaultExecutionOrder(50)]
[RequireComponent(typeof(RoomSceneController))]
public sealed class CombatRoomBounds : MonoBehaviour
{
    [Header("---Scene References---")]
    [SerializeField] private Camera roomCamera;
    [SerializeField] private PlayerController player;

    [Header("---Room Boundaries---")]
    [Tooltip("World-space distance beyond the farthest enemy reached by this encounter.")]
    [SerializeField, Min(0.5f)] private float exitPadding = 3f;
    [SerializeField, Min(0.1f)] private float wallThickness = 0.5f;
    [Tooltip("Vertical wall coverage. Extends above and below the player's starting height to block jumping around it.")]
    [SerializeField, Min(10f)] private float wallHeight = 40f;

    private readonly List<EnemyStats> enemies = new List<EnemyStats>();
    private RoomSceneController roomController;
    private CombatController combat;
    private PlayerStats playerStats;
    private Rigidbody2D playerBody;
    private Collider2D playerCollider;
    private SpriteRenderer[] playerSprites;
    private BoxCollider2D leftWall;
    private bool configured;
    private bool exitRequested;
    private float wallCenterY;

    public bool IsInitialized { get; private set; }
    public bool ExitUnlocked { get; private set; }
    public float LeftEdge { get; private set; }
    public float RightEdge { get; private set; }

    private float CameraHalfWidth => roomCamera.orthographicSize * roomCamera.aspect;

    private void Awake()
    {
        // Empty room types use the initial viewport. Menu/map scenes never use these boundaries.
        if (gameObject.scene.name != "MainScene")
        {
            enabled = false;
            return;
        }

        roomCamera = roomCamera != null ? roomCamera : Camera.main;
        player = player != null ? player : FindAnyObjectByType<PlayerController>();
        if (roomCamera == null || !roomCamera.orthographic || player == null)
        {
            enabled = false;
            return;
        }

        roomController = GetComponent<RoomSceneController>();
        combat = player.GetComponent<CombatController>();
        playerStats = player.GetComponent<PlayerStats>();
        playerBody = player.GetComponent<Rigidbody2D>();
        playerCollider = player.GetComponent<Collider2D>();
        playerSprites = player.GetComponentsInChildren<SpriteRenderer>(true);
        // Awake runs before SaveManager restores a saved camera/player position.
        LeftEdge = roomCamera.transform.position.x - CameraHalfWidth;
        RightEdge = roomCamera.transform.position.x + CameraHalfWidth;
        wallCenterY = player.transform.position.y;
        leftWall = CreateWall("Left Room Boundary");
        configured = true;
        PositionWalls();
        player.SetRoomCameraBounds(this);
    }

    private void Start()
    {
        // SaveManager normally initializes us after spawning/restoring enemies.
        // This later execution order also supports opening MainScene directly.
        if (!IsInitialized) Initialize(null);
    }

    public void Initialize(RoomSaveData savedRoom)
    {
        if (!configured || IsInitialized) return;
        if (savedRoom != null && savedRoom.hasCombatBounds)
        {
            LeftEdge = savedRoom.combatLeftEdge;
            RightEdge = Mathf.Max(LeftEdge + CameraHalfWidth * 2f, savedRoom.combatRightEdge);
        }

        foreach (var enemy in FindObjectsByType<EnemyStats>(FindObjectsInactive.Include))
        {
            if (enemy.gameObject.scene != gameObject.scene) continue;
            enemies.Add(enemy);
            if (savedRoom == null || !savedRoom.hasCombatBounds)
                RightEdge = Mathf.Max(RightEdge, enemy.transform.position.x + exitPadding);
        }

        IsInitialized = true;
        UpdateExitState();
        PositionWalls();
        ConstrainPlayer();
        ClampCamera();
    }

    private void LateUpdate()
    {
        if (!IsInitialized) return;
        if (!ExitUnlocked)
        {
            foreach (var enemy in enemies)
                if (enemy != null && !enemy.IsDead)
                    RightEdge = Mathf.Max(RightEdge, enemy.transform.position.x + exitPadding);
            UpdateExitState();
        }

        // A larger window must still fit between the two room edges.
        RightEdge = Mathf.Max(RightEdge, LeftEdge + CameraHalfWidth * 2f);
        PositionWalls();
        ClampCamera();
        TryExit();
    }

    private void UpdateExitState()
    {
        foreach (var enemy in enemies)
            if (enemy != null && !enemy.IsDead) return;

        if (ExitUnlocked) return;
        ExitUnlocked = true;
    }

    public float ClampCameraX(float x)
    {
        if (!configured) return x;
        float minimum = LeftEdge + CameraHalfWidth;
        return ExitUnlocked
            ? Mathf.Clamp(x, minimum, Mathf.Max(minimum, RightEdge - CameraHalfWidth))
            : Mathf.Max(x, minimum);
    }

    private void ClampCamera()
    {
        Vector3 position = roomCamera.transform.position;
        position.x = ClampCameraX(position.x);
        roomCamera.transform.position = position;
    }

    public void CaptureSave(RoomSaveData data)
    {
        if (!IsInitialized) return;
        data.hasCombatBounds = true;
        data.combatLeftEdge = LeftEdge;
        data.combatRightEdge = RightEdge;
    }

    public void TryExit()
    {
        if (!ExitUnlocked || exitRequested || Time.timeScale <= 0f
            || playerStats == null || playerStats.IsDead || !player.CanStartTurnCombat
            || (combat != null && combat.IsInCombat) || SceneTransition.IsPlaying
            || !HasPlayerLeftScreen())
            return;

        foreach (var enemy in enemies)
            if (enemy != null && enemy.DeathRewardsPending) return;
        // Walking out early should not discard coins still travelling to the HUD.
        if (FindAnyObjectByType<CoinPickup>() != null) return;

        roomController.CompleteRoom();
        exitRequested = roomController.IsReturningToMap;
    }

    private bool HasPlayerLeftScreen()
    {
        if (playerCollider.bounds.min.x < RightEdge + 0.05f) return false;
        foreach (var sprite in playerSprites)
            if (sprite != null && sprite.enabled && sprite.gameObject.activeInHierarchy
                && sprite.sprite != null && sprite.bounds.min.x < RightEdge + 0.05f)
                return false;
        return true;
    }

    public float ClampMovementVelocity(float velocity, Collider2D body, float stepDuration)
    {
        if (!configured || velocity >= 0f) return velocity;
        // Stop just before contact rather than repeatedly pushing into the physics solver.
        float skin = Mathf.Max(0.03f, Physics2D.defaultContactOffset * 3f);
        float distance = Mathf.Max(0f, body.bounds.min.x - LeftEdge - skin);
        // Ground friction can absorb the final tiny step; treat that contact tolerance as settled.
        if (distance <= Mathf.Max(0.001f, Physics2D.defaultContactOffset)) return 0f;
        return Mathf.Max(velocity, -distance / Mathf.Max(0.0001f, stepDuration));
    }

    private BoxCollider2D CreateWall(string wallName)
    {
        var wall = new GameObject(wallName);
        wall.transform.SetParent(transform, false);
        return wall.AddComponent<BoxCollider2D>();
    }

    private void PositionWalls()
    {
        if (leftWall == null) return;
        Vector3 position = new Vector3(LeftEdge - wallThickness * 0.5f, wallCenterY, 0f);
        Vector2 size = new Vector2(wallThickness, wallHeight);
        // Avoid rebuilding an unchanged static collider every rendered frame.
        if (leftWall.transform.position != position) leftWall.transform.position = position;
        if (leftWall.size != size) leftWall.size = size;
    }

    private void ConstrainPlayer()
    {
        Physics2D.SyncTransforms();
        Bounds bounds = playerCollider.bounds;
        float correction = Mathf.Max(0f, LeftEdge - bounds.min.x);
        if (Mathf.Approximately(correction, 0f)) return;

        Vector3 position = player.transform.position + Vector3.right * correction;
        player.transform.position = position;
        playerBody.position = position;
        playerBody.linearVelocity = new Vector2(0f, playerBody.linearVelocity.y);
    }

    private void OnDestroy()
    {
        if (player != null) player.SetRoomCameraBounds(null);
    }
}
