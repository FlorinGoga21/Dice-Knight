using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>Versioned JSON files with atomic replacement and a previous-save backup.</summary>
public static class SaveFileStore
{
    public const int SlotCount = 3;

    public static string SlotPath(string directory, int slot)
    {
        if (slot < 1 || slot > SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(directory, "slot_" + slot + ".json");
    }

    public static bool Exists(string directory, int slot) =>
        File.Exists(SlotPath(directory, slot)) || File.Exists(SlotPath(directory, slot) + ".bak");

    public static bool TryDelete(string directory, int slot, out string message)
    {
        try
        {
            string path = SlotPath(directory, slot);
            File.Delete(path + ".tmp");
            File.Delete(path + ".bak");
            File.Delete(path);
            message = "Slot " + slot + " deleted.";
            return true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
        {
            message = "Could not delete the save: " + error.Message;
            return false;
        }
    }

    public static bool TryRead(string directory, int slot, out SaveGameData data, out string message)
    {
        string path = SlotPath(directory, slot);
        if (ReadFile(path, slot, out data, out message)) return true;
        if (ReadFile(path + ".bak", slot, out data, out _))
        {
            message = "Recovered the previous save backup.";
            return true;
        }
        if (!Exists(directory, slot)) message = "This slot is empty.";
        return false;
    }

    private static bool ReadFile(string path, int slot, out SaveGameData data, out string message)
    {
        data = null;
        message = "This save could not be read.";
        try
        {
            if (!File.Exists(path)) return false;
            if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException("Save file is too large.");
            data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path, Encoding.UTF8));
            // Unity serializes null inline classes as empty objects, so presence is explicit.
            if (data != null && !data.hasPlayer) data.player = null;
            if (data != null && !data.hasRoom) data.room = null;
            Validate(data, slot);
            message = "";
            return true;
        }
        catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is ArgumentException)
        {
            data = null;
            message = "Save unavailable: " + error.Message;
            return false;
        }
    }

    public static bool TryWrite(string directory, SaveGameData data, out string message)
    {
        string temporary = null;
        try
        {
            if (data != null) { data.hasPlayer = data.player != null; data.hasRoom = data.room != null; }
            Validate(data, data != null ? data.slot : 0);
            Directory.CreateDirectory(directory);
            string path = SlotPath(directory, data.slot);
            temporary = path + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path,
                ReadFile(path, data.slot, out _, out _) ? path + ".bak" : null);
            else File.Move(temporary, path);
            message = "Saved in Slot " + data.slot + ".";
            return true;
        }
        catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is ArgumentException)
        {
            message = "Could not save: " + error.Message;
            return false;
        }
        finally
        {
            try { if (temporary != null && File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void Validate(SaveGameData data, int slot)
    {
        if (data == null || data.version != 1) throw new InvalidDataException("Unsupported save version.");
        if (slot < 1 || slot > SlotCount || data.slot != slot) throw new InvalidDataException("Invalid save slot.");
        if (data.sceneName != "MapScene" && data.sceneName != "MainScene") throw new InvalidDataException("Unknown scene.");
        if (float.IsNaN(data.playSeconds) || float.IsInfinity(data.playSeconds) || data.playSeconds < 0f) throw new InvalidDataException("Invalid play time.");
        if (!DateTime.TryParse(data.savedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out _)) throw new InvalidDataException("Invalid save date.");
        var map = data.map;
        if (map == null || map.rooms == null || map.rooms.Count > 160) throw new InvalidDataException("Invalid map.");
        if (map.rooms.Count == 0)
        {
            if (map.currentRoom != -1 || map.roomInProgress) throw new InvalidDataException("Invalid map progress.");
        }
        else
        {
            if (map.columns < 2 || map.columns > 16 || map.rows < 2 || map.rows > 10 || map.startingRow < 0 || map.startingRow >= map.rows || map.currentRoom < -1 || map.currentRoom >= map.rooms.Count)
                throw new InvalidDataException("Invalid map dimensions or progress.");
            var positions = new HashSet<int>();
            foreach (var room in map.rooms)
            {
                if (room == null || room.column < 0 || room.column >= map.columns || room.row < 0 || room.row >= map.rows || !positions.Add(room.column * 10 + room.row) || !Enum.IsDefined(typeof(RunMapState.RoomType), room.type) || room.next == null)
                    throw new InvalidDataException("Invalid map room.");
                foreach (int next in room.next)
                    if (next < 0 || next >= map.rooms.Count || map.rooms[next] == null || map.rooms[next].column != room.column + 1) throw new InvalidDataException("Invalid map route.");
            }
            if (map.roomInProgress && map.currentRoom < 0) throw new InvalidDataException("Missing current room.");
        }
        var player = data.player;
        if (player != null && (player.maxHealth <= 0 || player.health <= 0 || player.health > player.maxHealth || player.gold < 0 || player.potions < 0 || !Finite(player.position) || !Finite(player.cameraPosition) || !Finite(player.velocity) || !Finite(player.rotation)))
            throw new InvalidDataException("Invalid player state.");
        if (data.sceneName == "MainScene" && (player == null || data.room == null)) throw new InvalidDataException("Missing room state.");
        if (data.room != null)
        {
            if (data.room.mapRoom != map.currentRoom || data.room.enemies == null || data.room.enemies.Count > 256) throw new InvalidDataException("Room progress does not match the map.");
            if (data.room.hasCombatBounds && (!Finite(new Vector3(data.room.combatLeftEdge, data.room.combatRightEdge, 0f))
                || data.room.combatRightEdge <= data.room.combatLeftEdge))
                throw new InvalidDataException("Invalid combat room boundaries.");
            var identities = new HashSet<string>();
            foreach (var enemy in data.room.enemies)
            {
                if (enemy == null || string.IsNullOrEmpty(enemy.id) || !identities.Add(enemy.id) || enemy.health < 0 || enemy.maxHealth <= 0 || enemy.health > enemy.maxHealth || !Finite(enemy.position) || !Finite(enemy.rotation)) throw new InvalidDataException("Invalid enemy state.");
                if (string.IsNullOrEmpty(enemy.spawnerId) != string.IsNullOrEmpty(enemy.prefabId))
                    throw new InvalidDataException("Incomplete spawned enemy identity.");
                if (enemy.hasFlightState && !Finite(new Vector3(enemy.flightCenterY, enemy.flightPhase, 0f)))
                    throw new InvalidDataException("Invalid enemy flight state.");
            }
        }
    }

    private static bool Finite(Vector3 value) =>
        !float.IsNaN(value.x) && !float.IsInfinity(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.z);

    private static bool Finite(Quaternion value) => Finite(new Vector3(value.x, value.y, value.z)) && !float.IsNaN(value.w) && !float.IsInfinity(value.w);
}
