using System.Collections.Generic;
using TMPro;
using UnityEngine;
using RoomType = RunMapState.RoomType;

/// <summary>Generates and manages the branching route shown in the MapScene Canvas.</summary>
public sealed partial class RunMapController : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] private RectTransform graphRoot;

    [Header("Room Sprites")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite enemySprite;
    [SerializeField] private Sprite shopSprite;
    [SerializeField] private Sprite randomSprite;
    [SerializeField] private Sprite treasureSprite;

    [Header("Map Generation (applied on the next run)")]
    [SerializeField, Range(2, 16)] private int columnCount = 8;
    [SerializeField, Range(2, 10)] private int rowCount = 5;
    [SerializeField, Min(1)] private int minimumRoomsPerColumn = 3;
    [SerializeField, Min(1)] private int maximumRoomsPerColumn = 5;
    [Tooltip("Maximum row gap for optional branches. Required routes may span farther to keep every room connected.")]
    [SerializeField, Range(0, 3)] private int connectionRowRange = 1;
    [Tooltip("Chance to add an extra forward branch when it will not cross an existing route.")]
    [SerializeField, Range(0f, 1f)] private float extraConnectionChance = 0.5f;
    [SerializeField] private bool randomizeSeed = true;
    [Tooltip("Turn off Randomize Seed to reproduce the same map.")]
    [SerializeField] private int seed = 12345;

    [Header("Room Type Weights (applied on the next run)")]
    [SerializeField, Min(0f)] private float enemyWeight = 55f;
    [SerializeField, Min(0f)] private float shopWeight = 15f;
    [SerializeField, Min(0f)] private float randomWeight = 17f;
    [SerializeField, Min(0f)] private float treasureWeight = 13f;

    [Header("Map Layout")]
    [Tooltip("Fraction of the MapGraph width occupied by the room columns.")]
    [SerializeField, Range(0.1f, 0.98f)] private float horizontalSpread = 0.86f;
    [Tooltip("Fraction of the MapGraph height occupied by the room rows.")]
    [SerializeField, Range(0.1f, 0.98f)] private float verticalSpread = 0.72f;
    [SerializeField] private Vector2 mapOffset = Vector2.zero;

    [Header("Node Appearance")]
    [SerializeField, Min(8f)] private float nodeDiameter = 84f;
    [SerializeField, Min(4f)] private float nodeIconSize = 64f;
    [SerializeField, Min(8f)] private float bossDiameter = 126f;
    [SerializeField, Min(4f)] private float bossIconSize = 96f;
    [SerializeField] private Color nodeBackgroundColor = Color.black;
    [SerializeField] private Color roomIconColor = Color.white;

    [Header("Origin Appearance")]
    [SerializeField, Min(8f)] private float originDiameter = 84f;

    [Header("Player Appearance")]
    [SerializeField] private Vector2 playerIconSize = new Vector2(100f, 112f);
    [Tooltip("Player position relative to the room center. Positive Y places the player above the room.")]
    [SerializeField] private Vector2 playerNodeOffset = new Vector2(0f, 78f);
    [SerializeField] private Color playerIconColor = Color.white;

    [Header("Player Movement")]
    [SerializeField, Min(1f)] private float playerMoveSpeed = 500f;
    [SerializeField, Min(0.01f)] private float minimumMoveDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float maximumMoveDuration = 0.9f;
    [SerializeField] private AnimationCurve movementEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Player Sway")]
    [Tooltip("Maximum Z rotation in either direction while idle, in degrees. Set to zero to disable idle sway.")]
    [SerializeField, Range(0f, 90f)] private float idleSwayAngle = 30f;
    [Tooltip("Full back-and-forth cycles per second while idle.")]
    [SerializeField, Min(0f)] private float idleSwaySpeed = 0.5f;
    [Tooltip("Maximum Z rotation in either direction while moving, in degrees.")]
    [SerializeField, Range(0f, 90f)] private float movingSwayAngle = 45f;
    [Tooltip("Full back-and-forth cycles per second while moving.")]
    [SerializeField, Min(0f)] private float movingSwaySpeed = 2.5f;

    [Header("Room Transition")]
    [SerializeField] private string roomSceneName = "MainScene";
    [Tooltip("Time to wait at a room node before covering the map with clouds.")]
    [SerializeField, Min(0f)] private float roomArrivalDelay = 0.5f;
    [SerializeField] private string transitionStyle = "clouds";
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.8f;

    [Header("Map Routes")]
    [SerializeField] private Color routeColor = Color.black;
    [SerializeField, Min(1f)] private float routeDotSize = 8f;
    [Tooltip("Distance between dot centers, in Canvas units.")]
    [SerializeField, Min(2f)] private float routeDotSpacing = 38f;

    [Header("Available Connection Highlight")]
    [Tooltip("Color the existing connections to the next available rooms.")]
    [SerializeField] private bool showAvailableRoutes = true;
    [SerializeField] private Color availableRouteColor = new Color32(255, 202, 91, 255);
    [SerializeField, Min(1f)] private float availableRouteDotSize = 7f;
    [SerializeField, Min(2f)] private float availableRouteDotSpacing = 18f;

    [Header("Fonts")]
    [Tooltip("Default TMP font for all map text. Leave empty to use the project's default font.")]
    [SerializeField] private TMP_FontAsset mapFont;
    [Tooltip("Optional font override for the legend.")]
    [SerializeField] private TMP_FontAsset legendFont;
    [Tooltip("Optional font override for the room status text.")]
    [SerializeField] private TMP_FontAsset statusFont;
    [SerializeField] private FontStyles fontStyle = FontStyles.Normal;
    [SerializeField] private Color textColor = new Color32(232, 221, 191, 255);
    [SerializeField, Min(8f)] private float statusFontSize = 22f;
    [SerializeField, Min(8f)] private float legendFontSize = 18f;
    [SerializeField, Min(8f)] private float legendTitleFontSize = 20f;

    [Header("Legend")]
    [SerializeField] private bool showLegend = true;
    [SerializeField] private LegendCorner legendCorner = LegendCorner.TopRight;
    [Tooltip("Distance inward from the selected Canvas corner, in Canvas units.")]
    [SerializeField] private Vector2 legendInset = new Vector2(48f, 42f);
    [SerializeField, Min(140f)] private float legendWidth = 210f;
    [SerializeField, Min(24f)] private float legendRowHeight = 28f;
    [SerializeField, Min(12f)] private float legendIconSize = 20f;
    [SerializeField] private Color legendBackgroundColor = new Color32(27, 24, 22, 220);

    private readonly List<RoomNode> nodes = new List<RoomNode>();
    private readonly List<RoomConnection> connections = new List<RoomConnection>();
    private readonly List<LegendEntry> legendEntries = new List<LegendEntry>();
    private RectTransform routesRoot;
    private RectTransform startOrigin;
    private RectTransform playerMarker;
    private TMP_Text statusText;
    private RectTransform legendRoot;
    private UnityEngine.UI.Image legendBackground;
    private TMP_Text legendTitle;
    private Sprite nodeCircleSprite;
    private RectTransform canvasRoot;
    private int generatedColumnCount;
    private int generatedRowCount;
    private int currentColumn = -1;
    private int currentRow;
    private int startingRow;
    private bool runFinished;
    private bool isMoving;
    private bool isEnteringRoom;
    private bool settingsDirty;
    private float playerSwayPhase;
    private float playerSwayAngle;

    public bool IsBusy => isMoving || isEnteringRoom;

    private enum LegendCorner { TopLeft, TopRight, BottomLeft, BottomRight }

    private sealed class LegendEntry
    {
        public RectTransform rect;
        public UnityEngine.UI.Image background;
        public UnityEngine.UI.Image icon;
        public TMP_Text label;
        public bool isPlayer;
    }

    private sealed class RoomNode
    {
        public int column;
        public int row;
        public RoomType type;
        public readonly List<RoomNode> next = new List<RoomNode>();
        public UnityEngine.UI.Button button;
        public RectTransform rect;
        public UnityEngine.UI.Image icon;
    }

    private sealed class RoomConnection
    {
        // A null source represents the starting point to the left of the first column.
        public RoomNode from;
        public RoomNode to;
        public RectTransform root;
        public readonly List<UnityEngine.UI.Image> dots = new List<UnityEngine.UI.Image>();
    }

    private void Awake()
    {
        ClampSettings();
        if (graphRoot == null)
            graphRoot = transform as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRoot = canvas.GetComponent<RectTransform>();
        nodeCircleSprite = CreateNodeCircleSprite();
    }

    private void Start()
    {
        BuildMapUI();
        if (!RestoreMap())
        {
            GenerateMap();
            SaveMapLayout();
        }
        DrawConnections();
        DrawRooms();
        DrawStartOrigin();
        DrawPlayerMarker();
        UpdateAvailableRooms();
        if (RunMapState.CurrentRoomIndex >= 0 && statusText != null)
            statusText.text = runFinished ? "Run complete!" : "Room completed";
    }

    private void OnValidate()
    {
        ClampSettings();
        settingsDirty = true;
    }

    private void Update()
    {
        if (!settingsDirty || playerMarker == null)
            return;

        settingsDirty = false;
        foreach (RoomNode node in nodes)
            ApplyNodeAppearance(node);
        playerMarker.sizeDelta = playerIconSize;
        playerMarker.GetComponent<UnityEngine.UI.Image>().color = playerIconColor;
        ApplyStartOriginAppearance();
        if (!isMoving)
            PositionPlayer();
        UpdateAvailableRooms();
        ApplyTextAppearance();
    }

    private void LateUpdate()
    {
        if (playerMarker == null || Time.timeScale <= 0f)
            return;

        float deltaTime = Time.unscaledDeltaTime;
        float targetAngle = isMoving ? movingSwayAngle : idleSwayAngle;
        float speed = isMoving ? movingSwaySpeed : idleSwaySpeed;
        // Keep the phase continuous and ease the amplitude when starting or stopping a walk.
        playerSwayAngle = Mathf.Lerp(playerSwayAngle, targetAngle, 1f - Mathf.Exp(-12f * deltaTime));
        playerSwayPhase = Mathf.Repeat(playerSwayPhase + speed * Mathf.PI * 2f * deltaTime, Mathf.PI * 2f);
        playerMarker.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(playerSwayPhase) * playerSwayAngle);
    }

    private void OnDestroy()
    {
        if (nodeCircleSprite != null)
        {
            Destroy(nodeCircleSprite.texture);
            Destroy(nodeCircleSprite);
        }
    }
}
