using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Generates and manages the branching route shown in the MapScene Canvas.</summary>
public sealed class RunMapController : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] private RectTransform graphRoot;
    [SerializeField] private Texture2D iconSource;

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
    private Sprite playerSprite;
    private Sprite enemySprite;
    private Sprite shopSprite;
    private Sprite randomSprite;
    private Sprite treasureSprite;
    private Sprite nodeCircleSprite;
    private RectTransform canvasRoot;
    private int generatedColumnCount;
    private int generatedRowCount;
    private int currentColumn = -1;
    private int currentRow;
    private int startingRow;
    private bool runFinished;
    private bool isMoving;
    private bool settingsDirty;

    private enum RoomType { Enemy, Shop, Random, Treasure, Boss }
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
        public Button button;
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
        CreateRoomSprites();
    }

    private void Start()
    {
        BuildMapUI();
        GenerateMap();
        DrawConnections();
        DrawRooms();
        DrawStartOrigin();
        DrawPlayerMarker();
        UpdateAvailableRooms();
    }

    private void OnValidate()
    {
        ClampSettings();
        settingsDirty = true;
    }

    private void ClampSettings()
    {
        columnCount = Mathf.Clamp(columnCount, 2, 16);
        rowCount = Mathf.Clamp(rowCount, 2, 10);
        minimumRoomsPerColumn = Mathf.Clamp(minimumRoomsPerColumn, 1, rowCount);
        maximumRoomsPerColumn = Mathf.Clamp(maximumRoomsPerColumn, minimumRoomsPerColumn, rowCount);
        playerIconSize.x = Mathf.Max(4f, playerIconSize.x);
        playerIconSize.y = Mathf.Max(4f, playerIconSize.y);
        minimumMoveDuration = Mathf.Max(0.01f, minimumMoveDuration);
        maximumMoveDuration = Mathf.Max(minimumMoveDuration, maximumMoveDuration);
        originDiameter = Mathf.Max(8f, originDiameter);
        legendWidth = Mathf.Max(140f, legendWidth);
        legendRowHeight = Mathf.Max(24f, legendRowHeight);
        legendIconSize = Mathf.Max(12f, legendIconSize);
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

    private void CreateRoomSprites()
    {
        if (iconSource == null)
        {
            Debug.LogError("MapScene is missing the icon sheet reference.", this);
            return;
        }

        playerSprite = ExtractIcon("Player", 76, 369, 405, 752);
        enemySprite = ExtractIcon("Enemy", 455, 386, 795, 741);
        shopSprite = ExtractIcon("Shop", 845, 382, 1183, 748);
        randomSprite = ExtractIcon("Random", 1255, 380, 1585, 751);
        treasureSprite = ExtractIcon("Treasure", 1635, 386, 1988, 746);
    }

    private Sprite ExtractIcon(string name, int xMin, int yMin, int xMax, int yMax)
    {
        Color32[] sourcePixels = iconSource.GetPixels32();
        int width = xMax - xMin;
        int height = yMax - yMin;
        Color32[] outputPixels = new Color32[width * height];
        int left = width, right = 0, bottom = height, top = 0;
        bool found = false;
        Color32 key = sourcePixels[(iconSource.height - yMin - 1) * iconSource.width + xMin + 8];

        for (int y = 0; y < height; y++)
        {
            // Screenshot crop coordinates start at the top; Unity texture rows start at the bottom.
            int sourceY = iconSource.height - yMax + y;
            for (int x = 0; x < width; x++)
            {
                Color32 color = sourcePixels[sourceY * iconSource.width + xMin + x];
                int luma = (color.r * 30 + color.g * 59 + color.b * 11) / 100;
                int keyDistance = Mathf.Abs(color.r - key.r) + Mathf.Abs(color.g - key.g) + Mathf.Abs(color.b - key.b);
                bool darkOutline = color.r < 18 && color.g < 18 && color.b < 26;
                bool ivoryArtwork = color.r > 175 && color.g > 170 && color.b > 135;
                bool blueGrayBackdrop = color.b > color.r + 7 && color.g >= color.r - 3 && luma < 160;
                bool background = (keyDistance < 54 && luma > 18) || blueGrayBackdrop ||
                    (luma > 18 && luma < 130 && color.r < 105 && color.g < 110 && color.b < 125);

                color.a = (byte)((darkOutline || ivoryArtwork || !background) ? 255 : 0);
                outputPixels[y * width + x] = color;
                if (color.a != 0)
                {
                    found = true;
                    left = Mathf.Min(left, x);
                    right = Mathf.Max(right, x);
                    bottom = Mathf.Min(bottom, y);
                    top = Mathf.Max(top, y);
                }
            }
        }

        if (!found)
        {
            Debug.LogError("Could not extract the " + name + " icon from the map icon sheet.", this);
            return null;
        }

        int trimmedWidth = right - left + 1;
        int trimmedHeight = top - bottom + 1;
        Color32[] trimmed = new Color32[trimmedWidth * trimmedHeight];
        for (int y = 0; y < trimmedHeight; y++)
        {
            for (int x = 0; x < trimmedWidth; x++)
                trimmed[y * trimmedWidth + x] = outputPixels[(bottom + y) * width + left + x];
        }

        Texture2D texture = new Texture2D(trimmedWidth, trimmedHeight, TextureFormat.RGBA32, false, false);
        texture.name = name + " Map Icon";
        texture.filterMode = FilterMode.Point;
        texture.SetPixels32(trimmed);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, trimmedWidth, trimmedHeight), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
    }

    private void BuildMapUI()
    {
        if (canvasRoot == null)
            return;

        UnityEngine.UI.Image statusPanel = CreatePanel("Room Status Panel", canvasRoot, new Color32(27, 24, 22, 215), new Vector2(0.32f, 0.052f), new Vector2(0.68f, 0.112f));
        statusText = CreateText("Room Status", statusPanel.transform, "Choose a connected room", statusFont, statusFontSize, TextAlignmentOptions.Center);

        legendBackground = CreatePanel("Map Legend", canvasRoot, legendBackgroundColor, Vector2.one, Vector2.one);
        legendRoot = legendBackground.rectTransform;
        legendTitle = CreateText("Legend Title", legendRoot, "Legend", legendFont, legendTitleFontSize, TextAlignmentOptions.MidlineLeft);
        CreateLegendEntry("Player", playerSprite, true);
        CreateLegendEntry("Enemy", enemySprite);
        CreateLegendEntry("Shop", shopSprite);
        CreateLegendEntry("Random", randomSprite);
        CreateLegendEntry("Treasure", treasureSprite);
        ApplyTextAppearance();
    }

    private void CreateLegendEntry(string label, Sprite sprite, bool isPlayer = false)
    {
        RectTransform row = CreateRect(label + " Legend Entry", legendRoot);
        RectTransform backing = CreateRect("Icon Background", row);
        UnityEngine.UI.Image background = backing.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.sprite = nodeCircleSprite;
        background.raycastTarget = false;
        RectTransform iconRect = CreateRect("Icon", row);
        UnityEngine.UI.Image icon = iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        legendEntries.Add(new LegendEntry
        {
            rect = row,
            background = background,
            icon = icon,
            label = CreateText("Label", row, label, legendFont, legendFontSize, TextAlignmentOptions.MidlineLeft),
            isPlayer = isPlayer
        });
    }

    private TMP_FontAsset ResolveFont(TMP_FontAsset fontOverride)
    {
        return fontOverride != null ? fontOverride : mapFont != null ? mapFont : TMP_Settings.defaultFontAsset;
    }

    private void StyleText(TMP_Text text, TMP_FontAsset fontOverride, float size)
    {
        text.font = ResolveFont(fontOverride);
        text.fontSize = size;
        text.fontStyle = fontStyle;
        text.color = textColor;
    }

    private void ApplyTextAppearance()
    {
        if (statusText != null)
            StyleText(statusText, statusFont, statusFontSize);
        if (legendRoot == null)
            return;

        legendRoot.gameObject.SetActive(showLegend);
        Vector2 corner;
        switch (legendCorner)
        {
            case LegendCorner.TopLeft: corner = new Vector2(0f, 1f); break;
            case LegendCorner.BottomLeft: corner = Vector2.zero; break;
            case LegendCorner.BottomRight: corner = new Vector2(1f, 0f); break;
            default: corner = Vector2.one; break;
        }
        const float padding = 12f;
        const float titleGap = 6f;
        float titleHeight = legendTitleFontSize + 8f;
        float backingSize = legendIconSize + 8f;
        float rowHeight = Mathf.Max(legendRowHeight, Mathf.Max(backingSize, legendFontSize + 6f));
        legendRoot.anchorMin = legendRoot.anchorMax = legendRoot.pivot = corner;
        legendRoot.anchoredPosition = new Vector2(corner.x == 0f ? legendInset.x : -legendInset.x,
            corner.y == 0f ? legendInset.y : -legendInset.y);
        legendRoot.sizeDelta = new Vector2(legendWidth, padding * 2f + titleHeight + titleGap + legendEntries.Count * rowHeight);
        legendBackground.color = legendBackgroundColor;
        StyleText(legendTitle, legendFont, legendTitleFontSize);
        legendTitle.fontStyle |= FontStyles.Bold;
        legendTitle.rectTransform.anchorMin = legendTitle.rectTransform.anchorMax = legendTitle.rectTransform.pivot = new Vector2(0f, 1f);
        legendTitle.rectTransform.anchoredPosition = new Vector2(padding, -padding);
        legendTitle.rectTransform.sizeDelta = new Vector2(legendWidth - padding * 2f, titleHeight);

        for (int i = 0; i < legendEntries.Count; i++)
        {
            LegendEntry entry = legendEntries[i];
            entry.rect.anchorMin = entry.rect.anchorMax = entry.rect.pivot = new Vector2(0f, 1f);
            entry.rect.anchoredPosition = new Vector2(padding, -padding - titleHeight - titleGap - i * rowHeight);
            entry.rect.sizeDelta = new Vector2(legendWidth - padding * 2f, rowHeight);
            entry.background.enabled = !entry.isPlayer;
            entry.background.color = nodeBackgroundColor;
            entry.background.rectTransform.anchorMin = entry.background.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            entry.background.rectTransform.anchoredPosition = new Vector2(backingSize * 0.5f, 0f);
            entry.background.rectTransform.sizeDelta = Vector2.one * backingSize;
            entry.icon.rectTransform.anchorMin = entry.icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            entry.icon.rectTransform.anchoredPosition = new Vector2(backingSize * 0.5f, 0f);
            entry.icon.rectTransform.sizeDelta = Vector2.one * legendIconSize;
            entry.icon.color = entry.isPlayer ? playerIconColor : roomIconColor;
            StyleText(entry.label, legendFont, legendFontSize);
            entry.label.rectTransform.offsetMin = new Vector2(backingSize + 10f, 0f);
            entry.label.rectTransform.offsetMax = Vector2.zero;
        }
    }

    private void GenerateMap()
    {
        nodes.Clear();
        generatedColumnCount = columnCount;
        generatedRowCount = rowCount;
        var random = new System.Random(randomizeSeed ? Random.Range(0, int.MaxValue) : seed);
        currentColumn = -1;
        currentRow = startingRow = random.Next(generatedRowCount);

        for (int column = 0; column < generatedColumnCount; column++)
        {
            int count = column == generatedColumnCount - 1 ? 1 : random.Next(minimumRoomsPerColumn, maximumRoomsPerColumn + 1);
            var rows = new HashSet<int>();
            if (column == 0)
                rows.Add(currentRow);
            if (column == generatedColumnCount - 1)
                rows.Add(generatedRowCount / 2);
            while (rows.Count < count)
                rows.Add(random.Next(generatedRowCount));

            foreach (int row in rows)
                nodes.Add(new RoomNode { column = column, row = row, type = PickRoomType(column, random) });
        }

        for (int column = 0; column < generatedColumnCount - 1; column++)
        {
            List<RoomNode> from = nodes.FindAll(n => n.column == column);
            List<RoomNode> to = nodes.FindAll(n => n.column == column + 1);
            from.Sort((a, b) => a.row.CompareTo(b.row));
            to.Sort((a, b) => a.row.CompareTo(b.row));
            bool[,] links = RunMapConnections.Build(from.ConvertAll(n => n.row), to.ConvertAll(n => n.row),
                connectionRowRange, extraConnectionChance, random);
            for (int source = 0; source < from.Count; source++)
            {
                for (int target = 0; target < to.Count; target++)
                {
                    if (links[source, target])
                        from[source].next.Add(to[target]);
                }
            }
        }
    }

    private RoomType PickRoomType(int column, System.Random random)
    {
        if (column == generatedColumnCount - 1)
            return RoomType.Boss;
        float total = enemyWeight + randomWeight + shopWeight + treasureWeight;
        if (total <= 0f)
            return RoomType.Enemy;
        float roll = (float)random.NextDouble() * total;
        if (roll < enemyWeight) return RoomType.Enemy;
        if (roll < enemyWeight + randomWeight) return RoomType.Random;
        if (roll < enemyWeight + randomWeight + shopWeight) return RoomType.Shop;
        return RoomType.Treasure;
    }

    private void DrawConnections()
    {
        if (routesRoot == null)
            routesRoot = CreateRect("Map Routes", graphRoot);
        foreach (RoomNode node in nodes)
        {
            if (node.column == 0)
                CreateConnection(null, node);
            foreach (RoomNode next in node.next)
                CreateConnection(node, next);
        }
    }

    private void CreateConnection(RoomNode from, RoomNode to)
    {
        string sourceName = from == null ? "Start" : "Room " + from.column + ":" + from.row;
        var connection = new RoomConnection
        {
            from = from,
            to = to,
            root = CreateRect(sourceName + " To " + to.column + ":" + to.row, routesRoot)
        };
        connections.Add(connection);
        RefreshConnection(connection);
    }

    private void RefreshConnection(RoomConnection connection)
    {
        bool available = showAvailableRoutes && !isMoving && !runFinished && IsReachable(connection.to) &&
            (currentColumn < 0 ? connection.from == null :
                connection.from != null && connection.from.column == currentColumn && connection.from.row == currentRow);
        Vector2 from = connection.from == null ? StartPosition() : PositionFor(connection.from);
        Vector2 to = PositionFor(connection.to);
        float distance = Vector2.Distance(from, to);
        float spacing = Mathf.Max(2f, available ? availableRouteDotSpacing : routeDotSpacing);
        float size = available ? availableRouteDotSize : routeDotSize;
        Color color = available ? availableRouteColor : routeColor;
        int dotCount = Mathf.FloorToInt(distance / spacing) + 1;

        while (connection.dots.Count < dotCount)
        {
            RectTransform dot = CreateRect("Route Dot", connection.root);
            UnityEngine.UI.Image image = dot.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;
            connection.dots.Add(image);
        }

        for (int i = 0; i < connection.dots.Count; i++)
        {
            UnityEngine.UI.Image dot = connection.dots[i];
            dot.gameObject.SetActive(i < dotCount);
            if (i >= dotCount)
                continue;
            dot.rectTransform.sizeDelta = Vector2.one * size;
            dot.rectTransform.anchoredPosition = Vector2.Lerp(from, to, distance > 0f ? i * spacing / distance : 0f);
            dot.sprite = nodeCircleSprite;
            dot.color = color;
        }
    }

    private void DrawRooms()
    {
        foreach (RoomNode node in nodes)
        {
            var room = new GameObject(node.type + " Node", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            room.transform.SetParent(graphRoot, false);
            node.rect = room.GetComponent<RectTransform>();
            node.rect.anchorMin = node.rect.anchorMax = new Vector2(0.5f, 0.5f);
            UnityEngine.UI.Image background = room.GetComponent<UnityEngine.UI.Image>();
            background.sprite = nodeCircleSprite;
            background.alphaHitTestMinimumThreshold = 0.5f;

            RectTransform iconRect = CreateRect("Room Icon", room.transform);
            node.icon = iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            node.icon.sprite = SpriteFor(node.type);
            node.icon.preserveAspect = true;
            node.icon.raycastTarget = false;
            ApplyNodeAppearance(node);

            room.GetComponent<UnityEngine.UI.Button>().targetGraphic = background;
            room.GetComponent<UnityEngine.UI.Button>().transition = UnityEngine.UI.Selectable.Transition.None;
            node.button = room.GetComponent<UnityEngine.UI.Button>();
            node.button.onClick.AddListener(() => SelectRoom(node));

        }
    }

    private void ApplyNodeAppearance(RoomNode node)
    {
        float diameter = node.type == RoomType.Boss ? bossDiameter : nodeDiameter;
        float iconSize = node.type == RoomType.Boss ? bossIconSize : nodeIconSize;
        node.rect.sizeDelta = Vector2.one * diameter;
        node.rect.anchoredPosition = PositionFor(node);
        node.rect.GetComponent<UnityEngine.UI.Image>().color = nodeBackgroundColor;
        node.icon.rectTransform.sizeDelta = Vector2.one * iconSize;
        node.icon.color = roomIconColor;
    }

    private static Sprite CreateNodeCircleSprite()
    {
        const int size = 128;
        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radiusSquared = center * center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                pixels[y * size + x] = dx * dx + dy * dy <= radiusSquared
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Map Node Circle",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
    }

    private void OnDestroy()
    {
        if (nodeCircleSprite != null)
        {
            Destroy(nodeCircleSprite.texture);
            Destroy(nodeCircleSprite);
        }
    }

    private Sprite SpriteFor(RoomType type)
    {
        switch (type)
        {
            case RoomType.Shop: return shopSprite;
            case RoomType.Random: return randomSprite;
            case RoomType.Treasure: return treasureSprite;
            default: return enemySprite;
        }
    }

    private void DrawStartOrigin()
    {
        startOrigin = CreateRect("Start Origin", graphRoot);
        UnityEngine.UI.Image circle = startOrigin.gameObject.AddComponent<UnityEngine.UI.Image>();
        circle.sprite = nodeCircleSprite;
        circle.color = Color.black;
        circle.raycastTarget = false;
        ApplyStartOriginAppearance();
    }

    private void ApplyStartOriginAppearance()
    {
        if (startOrigin == null)
            return;
        startOrigin.sizeDelta = Vector2.one * originDiameter;
        startOrigin.anchoredPosition = StartPosition();
    }

    private void DrawPlayerMarker()
    {
        var marker = new GameObject("Player Marker", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        marker.transform.SetParent(graphRoot, false);
        playerMarker = marker.GetComponent<RectTransform>();
        playerMarker.anchorMin = playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
        playerMarker.sizeDelta = playerIconSize;
        UnityEngine.UI.Image playerImage = marker.GetComponent<UnityEngine.UI.Image>();
        playerImage.sprite = playerSprite;
        playerImage.preserveAspect = true;
        playerImage.raycastTarget = false;
        playerImage.color = playerIconColor;

        PositionPlayer();
    }

    private void SelectRoom(RoomNode node)
    {
        if (runFinished || isMoving || !IsReachable(node))
            return;

        isMoving = true;
        UpdateAvailableRooms();
        if (statusText != null)
            statusText.text = "Moving to " + node.type + " room...";
        StartCoroutine(MovePlayerToRoom(node));
    }

    private IEnumerator MovePlayerToRoom(RoomNode node)
    {
        Vector2 start = playerMarker.anchoredPosition;
        Vector2 destination = PlayerPositionFor(node);
        float duration = Mathf.Clamp(Vector2.Distance(start, destination) / Mathf.Max(1f, playerMoveSpeed), minimumMoveDuration, maximumMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float progress = movementEasing != null && movementEasing.length > 0 ? movementEasing.Evaluate(t) : t;
            destination = PlayerPositionFor(node);
            playerMarker.anchoredPosition = Vector2.Lerp(start, destination, progress);
            yield return null;
        }

        playerMarker.anchoredPosition = PlayerPositionFor(node);
        currentColumn = node.column;
        currentRow = node.row;
        if (statusText != null)
            statusText.text = node.type == RoomType.Boss ? "The Boss awaits..." : "You entered a " + node.type + " room";
        runFinished = node.type == RoomType.Boss;
        isMoving = false;
        UpdateAvailableRooms();
    }

    private bool IsReachable(RoomNode node)
    {
        if (currentColumn < 0)
            return node.column == 0;
        if (node.column != currentColumn + 1)
            return false;
        RoomNode current = nodes.Find(n => n.column == currentColumn && n.row == currentRow);
        return current != null && current.next.Contains(node);
    }

    private void UpdateAvailableRooms()
    {
        foreach (RoomNode node in nodes)
            node.button.interactable = !runFinished && !isMoving && IsReachable(node);
        foreach (RoomConnection connection in connections)
            RefreshConnection(connection);
    }

    private void PositionPlayer()
    {
        if (playerMarker == null)
            return;

        if (currentColumn < 0)
        {
            playerMarker.anchoredPosition = StartPosition();
            return;
        }

        RoomNode current = nodes.Find(n => n.column == currentColumn && n.row == currentRow);
        if (current != null)
            playerMarker.anchoredPosition = PlayerPositionFor(current);
    }

    private Vector2 StartPosition()
    {
        float firstColumnX = -graphRoot.rect.width * horizontalSpread * 0.5f;
        float startX = Mathf.Min(-graphRoot.rect.width * 0.49f, firstColumnX - playerIconSize.x * 0.75f);
        return new Vector2(startX + mapOffset.x, RowPosition(startingRow));
    }

    private Vector2 PlayerPositionFor(RoomNode node)
    {
        float extraRadius = Mathf.Max(0f, (node.rect.rect.height - nodeDiameter) * 0.5f);
        return PositionFor(node) + playerNodeOffset + Vector2.up * extraRadius;
    }

    private Vector2 PositionFor(RoomNode node)
    {
        float halfWidth = graphRoot.rect.width * horizontalSpread * 0.5f;
        float x = Mathf.Lerp(-halfWidth, halfWidth, node.column / (float)(generatedColumnCount - 1)) + mapOffset.x;
        return new Vector2(x, RowPosition(node.row));
    }

    private float RowPosition(int row)
    {
        float halfHeight = graphRoot.rect.height * verticalSpread * 0.5f;
        return Mathf.Lerp(-halfHeight, halfHeight, row / (float)(generatedRowCount - 1)) + mapOffset.y;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private UnityEngine.UI.Image CreatePanel(string name, RectTransform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text CreateText(string name, Transform parent, string value, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        TMP_Text text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        StyleText(text, font, size);
        text.alignment = alignment;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }
}
