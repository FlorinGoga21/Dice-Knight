using System.Collections.Generic;
using UnityEngine;
using RoomType = RunMapState.RoomType;

public sealed partial class RunMapController
{
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
        idleSwayAngle = Mathf.Clamp(idleSwayAngle, 0f, 90f);
        movingSwayAngle = Mathf.Clamp(movingSwayAngle, 0f, 90f);
        idleSwaySpeed = Mathf.Max(0f, idleSwaySpeed);
        movingSwaySpeed = Mathf.Max(0f, movingSwaySpeed);
        roomArrivalDelay = Mathf.Max(0f, roomArrivalDelay);
        transitionDuration = Mathf.Max(0.01f, transitionDuration);
        originDiameter = Mathf.Max(8f, originDiameter);
        legendWidth = Mathf.Max(140f, legendWidth);
        legendRowHeight = Mathf.Max(24f, legendRowHeight);
        legendIconSize = Mathf.Max(12f, legendIconSize);
    }

    private bool RestoreMap()
    {
        if (RunMapState.Rooms.Count == 0)
            return false;

        generatedColumnCount = RunMapState.ColumnCount;
        generatedRowCount = RunMapState.RowCount;
        startingRow = RunMapState.StartingRow;
        foreach (RunMapState.RoomData room in RunMapState.Rooms)
            nodes.Add(new RoomNode { column = room.column, row = room.row, type = room.type });
        for (int i = 0; i < nodes.Count; i++)
        {
            foreach (int next in RunMapState.Rooms[i].next)
                nodes[i].next.Add(nodes[next]);
        }

        currentRow = startingRow;
        if (RunMapState.CurrentRoomIndex >= 0)
        {
            RoomNode current = nodes[RunMapState.CurrentRoomIndex];
            currentColumn = current.column;
            currentRow = current.row;
            runFinished = current.type == RoomType.Boss && RunMapState.Rooms[RunMapState.CurrentRoomIndex].completed;
        }
        return true;
    }

    private void SaveMapLayout()
    {
        RunMapState.ResetRun();
        RunMapState.ColumnCount = generatedColumnCount;
        RunMapState.RowCount = generatedRowCount;
        RunMapState.StartingRow = startingRow;
        foreach (RoomNode node in nodes)
        {
            var room = new RunMapState.RoomData { column = node.column, row = node.row, type = node.type };
            foreach (RoomNode next in node.next)
                room.next.Add(nodes.IndexOf(next));
            RunMapState.Rooms.Add(room);
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
}
