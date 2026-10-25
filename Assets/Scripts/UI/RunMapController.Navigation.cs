using System.Collections;
using HeyHeyThere.SceneTransitionsFree;
using UnityEngine;
using RoomType = RunMapState.RoomType;

public sealed partial class RunMapController
{
    private UnityEngine.UI.Selectable[] mapChoices;
    private void SelectRoom(RoomNode node)
    {
        if (runFinished || isMoving || isEnteringRoom || RunMapState.RoomInProgress || SceneTransition.IsPlaying || !IsReachable(node))
            return;
        if (!Application.CanStreamedLevelBeLoaded(roomSceneName))
        {
            Debug.LogError($"RunMapController: Scene '{roomSceneName}' is not in Build Settings.", this);
            return;
        }

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
        isMoving = false;
        isEnteringRoom = true;
        RunMapState.CurrentRoomIndex = nodes.IndexOf(node);
        RunMapState.RoomInProgress = true;
        UpdateAvailableRooms();
        yield return new WaitForSecondsRealtime(roomArrivalDelay);
        while (SceneTransition.IsPlaying)
            yield return null;
        yield return SceneTransition.ChangeScene(roomSceneName, transitionStyle,
            new SceneTransition.Options { Duration = transitionDuration });
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
            node.button.interactable = !runFinished && !isMoving && !isEnteringRoom && !RunMapState.RoomInProgress && IsReachable(node);
        // Only the next reachable column participates in directional navigation.
        var available = nodes.FindAll(node => node.button.interactable);
        available.Sort((a, b) => a.row.CompareTo(b.row));
        for (int i = 0; i < available.Count; i++)
        {
            var navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit };
            navigation.selectOnDown = i > 0 ? available[i - 1].button : null;
            navigation.selectOnUp = i + 1 < available.Count ? available[i + 1].button : null;
            available[i].button.navigation = navigation;
        }
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
}
