using System.Collections.Generic;
using UnityEngine;

/// <summary>Map layout and room progress shared between scenes for the current run.</summary>
public static class RunMapState
{
    public enum RoomType { Enemy, Shop, Random, Treasure, Boss }

    internal sealed class RoomData
    {
        internal int column;
        internal int row;
        internal RoomType type;
        internal bool completed;
        internal readonly List<int> next = new List<int>();
    }

    internal static readonly List<RoomData> Rooms = new List<RoomData>();
    internal static int ColumnCount;
    internal static int RowCount;
    internal static int StartingRow;
    internal static int CurrentRoomIndex = -1;
    internal static bool RoomInProgress;

    // Future room setup can use this to choose enemies, shops, chests, and backgrounds.
    public static RoomType? CurrentRoomType => CurrentRoomIndex >= 0 && CurrentRoomIndex < Rooms.Count
        ? Rooms[CurrentRoomIndex].type : (RoomType?)null;

    // Branches in the same map column share a room number; direct scene play starts at 1.
    public static int CurrentRoomNumber => CurrentRoomIndex >= 0 && CurrentRoomIndex < Rooms.Count
        ? Rooms[CurrentRoomIndex].column + 1 : 1;

    public static void CompleteCurrentRoom()
    {
        if (!RoomInProgress || CurrentRoomIndex < 0 || CurrentRoomIndex >= Rooms.Count)
            return;

        Rooms[CurrentRoomIndex].completed = true;
        RoomInProgress = false;
    }

    public static MapSaveData Capture()
    {
        var data = new MapSaveData { columns = ColumnCount, rows = RowCount, startingRow = StartingRow,
            currentRoom = CurrentRoomIndex, roomInProgress = RoomInProgress };
        foreach (var room in Rooms)
            data.rooms.Add(new MapRoomSaveData { column = room.column, row = room.row, type = room.type,
                completed = room.completed, next = new List<int>(room.next) });
        return data;
    }

    public static void Restore(MapSaveData data)
    {
        ResetRun();
        if (data == null) return;
        ColumnCount = data.columns;
        RowCount = data.rows;
        StartingRow = data.startingRow;
        CurrentRoomIndex = data.currentRoom;
        RoomInProgress = data.roomInProgress;
        foreach (var saved in data.rooms)
        {
            var room = new RoomData { column = saved.column, row = saved.row, type = saved.type, completed = saved.completed };
            room.next.AddRange(saved.next);
            Rooms.Add(room);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetRun()
    {
        Rooms.Clear();
        ColumnCount = RowCount = StartingRow = 0;
        CurrentRoomIndex = -1;
        RoomInProgress = false;
    }
}
