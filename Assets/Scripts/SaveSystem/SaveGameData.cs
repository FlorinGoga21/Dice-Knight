using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SaveGameData
{
    public int version = 1;
    public int slot;
    public string savedUtc;
    public string sceneName = "MapScene";
    public float playSeconds;
    public bool hasPlayer;
    public bool hasRoom;
    public MapSaveData map = new MapSaveData();
    public PlayerSaveData player;
    public RoomSaveData room;
}

[Serializable]
public sealed class MapSaveData
{
    public int columns, rows, startingRow;
    public int currentRoom = -1;
    public bool roomInProgress;
    public List<MapRoomSaveData> rooms = new List<MapRoomSaveData>();
}

[Serializable]
public sealed class MapRoomSaveData
{
    public int column, row;
    public RunMapState.RoomType type;
    public bool completed;
    public List<int> next = new List<int>();
}

[Serializable]
public sealed class PlayerSaveData
{
    public int health, maxHealth, gold, potions;
    public bool hasProgression;
    public PlayerLevelState progression;
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public Vector2 velocity;
    public bool facingRight;
    public Vector3 cameraPosition;
}

[Serializable]
public sealed class RoomSaveData
{
    public int mapRoom = -1;
    public bool hasCombatBounds;
    public float combatLeftEdge, combatRightEdge;
    public List<EnemySaveData> enemies = new List<EnemySaveData>();
    public bool inCombat;
    public string combatEnemyId;
    public int turn, blocks;
}

[Serializable]
public sealed class EnemySaveData
{
    public string id;
    public string spawnerId;
    public string prefabId;
    public string weaponId;
    public bool hasFlightState;
    public float flightCenterY, flightPhase;
    public int health, maxHealth;
    public bool defeated;
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
}
