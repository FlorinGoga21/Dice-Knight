using UnityEngine;
using RoomType = RunMapState.RoomType;

public sealed partial class RunMapController
{
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
        bool available = showAvailableRoutes && !isMoving && !isEnteringRoom && !RunMapState.RoomInProgress && !runFinished && IsReachable(connection.to) &&
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
            room.GetComponent<UnityEngine.UI.Button>().transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            node.button = room.GetComponent<UnityEngine.UI.Button>();
            node.button.onClick.AddListener(() => SelectRoom(node));

        }
        mapChoices = nodes.ConvertAll(node => (UnityEngine.UI.Selectable)node.button).ToArray();
    }

    private void ApplyNodeAppearance(RoomNode node)
    {
        float diameter = node.type == RoomType.Boss ? bossDiameter : nodeDiameter;
        float iconSize = node.type == RoomType.Boss ? bossIconSize : nodeIconSize;
        node.rect.sizeDelta = Vector2.one * diameter;
        node.rect.anchoredPosition = PositionFor(node);
        node.rect.GetComponent<UnityEngine.UI.Image>().color = Color.white;
        var colors = node.rect.GetComponent<UnityEngine.UI.Button>().colors;
        colors.normalColor = nodeBackgroundColor;
        colors.disabledColor = nodeBackgroundColor;
        colors.highlightedColor = colors.selectedColor = new Color32(255, 215, 64, 255);
        colors.pressedColor = new Color32(255, 240, 160, 255);
        colors.fadeDuration = 0.08f;
        node.rect.GetComponent<UnityEngine.UI.Button>().colors = colors;
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
}
