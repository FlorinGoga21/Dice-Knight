using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HeyHeyThere.SceneTransitionsFree;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Exercises each placeholder room's fixed first screen and walk-off completion in Play mode.</summary>
public static class VerifyEmptyRoomBounds
{
    private static readonly RunMapState.RoomType[] Types =
        { RunMapState.RoomType.Shop, RunMapState.RoomType.Random, RunMapState.RoomType.Treasure };
    private static readonly FieldInfo MoveInput = typeof(PlayerController).GetField("moveInput", BindingFlags.Instance | BindingFlags.NonPublic);
    private static PlayerController player;
    private static CombatRoomBounds bounds;
    private static int roomIndex;
    private static float startingCameraX;
    private static double started;
    private static string reportPath;
    private static bool returning;
    private static bool loading;
    private static int loadFrame;

    public static object Main()
    {
        if (!EditorApplication.isPlaying || SceneTransition.IsPlaying) throw new Exception("Run in Play mode between transitions.");
        var saves = SaveManager.Instance;
        string fixtureRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/BoundaryVerification/"));
        if (saves != null && saves.ActiveSlot != 0 && !saves.SaveDirectory.StartsWith(fixtureRoot, StringComparison.OrdinalIgnoreCase))
            throw new Exception("Use a fresh direct Play session or the combat regression's isolated saves.");

        reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/empty-room-boundary-verification.json"));
        File.WriteAllText(reportPath, "{\"status\":\"running\"}");
        roomIndex = 0;
        started = EditorApplication.timeSinceStartup;
        LoadRoom();
        EditorApplication.update += Step;
        return new { status = "running", reportPath };
    }

    private static void LoadRoom()
    {
        var map = new MapSaveData { columns = 3, rows = 2, currentRoom = roomIndex, roomInProgress = true };
        for (int i = 0; i < Types.Length; i++) map.rooms.Add(new MapRoomSaveData { column = i, type = Types[i] });
        RunMapState.Restore(map);
        player = null;
        returning = false;
        loading = true;
        loadFrame = Time.frameCount;
        SceneManager.LoadScene("MainScene");
    }

    private static void Step()
    {
        try
        {
            Assert(EditorApplication.isPlaying && EditorApplication.timeSinceStartup - started < 40, "Placeholder verification stopped or timed out.");
            if (loading)
            {
                if (SceneManager.GetActiveScene().name != "MainScene" || Time.frameCount <= loadFrame) return;
                bounds = UnityEngine.Object.FindAnyObjectByType<CombatRoomBounds>();
                if (bounds == null || !bounds.IsInitialized) return;
                loading = false;
            }
            if (SceneManager.GetActiveScene().name == "MapScene")
            {
                if (SceneTransition.IsPlaying) return;
                Assert(returning && RunMapState.Capture().rooms[roomIndex].completed, "Placeholder room was not completed by walking off screen.");
                roomIndex++;
                if (roomIndex == Types.Length) { Finish(null); return; }
                LoadRoom();
                return;
            }

            if (player == null)
            {
                bounds = UnityEngine.Object.FindAnyObjectByType<CombatRoomBounds>();
                if (bounds == null || !bounds.IsInitialized) return;
                player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                player.enabled = false;
                player.GetComponent<CombatController>().enabled = false;
                Assert(UnityEngine.Object.FindObjectsByType<EnemyStats>().Length == 0, "Placeholder room spawned combat enemies.");
                Assert(bounds.ExitUnlocked, "Empty room exit did not unlock.");
                float half = Camera.main.orthographicSize * Camera.main.aspect;
                Assert(Mathf.Abs(bounds.RightEdge - bounds.LeftEdge - half * 2f) < 0.002f, "Empty room extends past its initial screen.");
                startingCameraX = Camera.main.transform.position.x;
            }

            MoveInput.SetValue(player, 1f);
            player.SendMessage("FixedUpdate");
            Assert(Mathf.Abs(Camera.main.transform.position.x - startingCameraX) < 0.002f, "Placeholder room camera moved beyond its first screen.");
            if (UnityEngine.Object.FindAnyObjectByType<RoomSceneController>().IsReturningToMap)
            {
                Assert(player.GetComponent<Collider2D>().bounds.min.x >= bounds.RightEdge, "Placeholder completed before player left the screen.");
                Assert(!player.GetComponentsInChildren<SpriteRenderer>().Any(s => s.enabled && s.sprite != null && s.bounds.min.x < bounds.RightEdge),
                    "Placeholder completed with character artwork still visible.");
                returning = true;
            }
        }
        catch (Exception error) { Finish(error.ToString()); }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Finish(string error)
    {
        EditorApplication.update -= Step;
        File.WriteAllText(reportPath, "{\"status\":\"" + (error == null ? "passed" : "failed") + "\",\"roomsChecked\":" + roomIndex
            + ",\"error\":\"" + (error ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n") + "\"}");
    }
}
