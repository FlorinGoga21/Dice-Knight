using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HeyHeyThere.SceneTransitionsFree;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Live physics/save/exit regression check. Uses a unique Temp save directory, never user saves.</summary>
public static class VerifyCombatRoomBounds
{
    private static readonly FieldInfo MoveInput = typeof(PlayerController).GetField("moveInput", BindingFlags.Instance | BindingFlags.NonPublic);
    private static PlayerController player;
    private static Rigidbody2D body;
    private static Collider2D collider;
    private static CombatRoomBounds bounds;
    private static SaveManager saves;
    private static float left, right;
    private static int phase, gold;
    private static double started, phaseStarted;
    private static string reportPath, saveDirectory;
    private static bool rewardWaitVerified;
    private static bool walkOffVerified;
    private static float settledMinX, settledMaxX;
    private static int fixtureAttempt;

    public static object Main()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "MainScene")
            throw new Exception("Enter Play mode in MainScene before running this check.");
        saves = SaveManager.Instance;
        if (saves == null || saves.ActiveSlot != 0)
            throw new Exception("Start a fresh direct MainScene Play session with no active save slot.");

        reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/combat-room-boundary-verification.json"));
        saveDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/BoundaryVerification/" + Guid.NewGuid().ToString("N")));
        typeof(SaveManager).GetProperty("SaveDirectory").SetValue(saves, saveDirectory);
        typeof(SaveManager).GetField("autosaveEnabled", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(saves, false);
        RunMapState.Restore(new MapSaveData
        {
            currentRoom = 0, roomInProgress = true, columns = 2, rows = 2,
            rooms = new System.Collections.Generic.List<MapRoomSaveData>
            {
                new MapRoomSaveData { type = RunMapState.RoomType.Enemy, next = new System.Collections.Generic.List<int> { 1 } },
                new MapRoomSaveData { column = 1, type = RunMapState.RoomType.Treasure }
            }
        });
        BindPlayer();
        Assert(bounds.IsInitialized && !bounds.ExitUnlocked, "Encounter must start locked.");
        Assert(bounds.GetComponentsInChildren<SpriteRenderer>(true).Length == 0, "Walls must be invisible.");
        left = bounds.LeftEdge;
        right = bounds.RightEdge;
        phase = UnityEngine.Object.FindObjectsByType<EnemyStats>().Length >= 2 ? 0 : -1;
        fixtureAttempt = 0;
        rewardWaitVerified = walkOffVerified = false;
        settledMinX = float.PositiveInfinity;
        settledMaxX = float.NegativeInfinity;
        started = phaseStarted = EditorApplication.timeSinceStartup;
        File.WriteAllText(reportPath, "{\"status\":\"running\"}");
        EditorApplication.update += Step;
        return new { status = "running", reportPath, saveDirectory };
    }

    private static void BindPlayer()
    {
        player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        bounds = UnityEngine.Object.FindAnyObjectByType<CombatRoomBounds>();
        if (player == null || bounds == null) return;
        // Drive the real movement/physics path deterministically, without holding the user's keyboard.
        player.enabled = false;
        player.GetComponent<CombatController>().enabled = false;
        body = player.GetComponent<Rigidbody2D>();
        collider = player.GetComponent<Collider2D>();
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>())
        {
            enemy.enabled = false;
            enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }
    }

    private static void Drive(float direction)
    {
        MoveInput.SetValue(player, direction);
        player.SendMessage("FixedUpdate");
        player.SendMessage("HandleAnimation");
    }

    private static void Next(int value)
    {
        phase = value;
        phaseStarted = EditorApplication.timeSinceStartup;
    }

    private static void Step()
    {
        try
        {
            Assert(EditorApplication.isPlaying, "Play mode stopped during verification.");
            Assert(EditorApplication.timeSinceStartup - started < 55, "Verification timed out in phase " + phase);
            double elapsed = EditorApplication.timeSinceStartup - phaseStarted;
            if (phase == -1)
            {
                if (elapsed < 0.3) return;
                BindPlayer();
                if (bounds == null || !bounds.IsInitialized) return;
                if (UnityEngine.Object.FindObjectsByType<EnemyStats>().Length >= 2)
                {
                    left = bounds.LeftEdge;
                    right = bounds.RightEdge;
                    Next(0);
                }
                else
                {
                    Assert(fixtureAttempt < 12, "Could not prepare a multi-enemy fixture.");
                    UnityEngine.Random.InitState(100 + fixtureAttempt++);
                    SceneManager.LoadScene("MainScene");
                    Next(-1);
                }
                return;
            }
            if (phase == 0 || phase == 1)
            {
                Drive(-1f);
                Assert(collider.bounds.min.x >= left - 0.03f, "Player crossed the left wall.");
                Assert(Camera.main.transform.position.x - Camera.main.orthographicSize * Camera.main.aspect >= left - 0.002f,
                    "Camera looked past the left wall.");
                if (phase == 0 && elapsed > 1.2)
                {
                    Assert(collider.bounds.min.x <= left + 0.05f, "Player did not reach the left wall.");
                    Assert(settledMaxX - settledMinX < 0.001f, "Player jitters while holding movement against the left wall.");
                    Assert(Mathf.Abs(body.linearVelocity.x) < 0.01f, "Movement keeps pushing against the left wall.");
                    Assert(player.GetComponent<Animator>().GetFloat("Speed") < 0.01f, "Player keeps walking in place against the wall.");
                    body.linearVelocity = new Vector2(-5f, 10f);
                    Next(1);
                }
                else if (phase == 1 && elapsed > 1.5)
                {
                    Drive(0f);
                    UnityEngine.Object.FindObjectsByType<EnemyStats>()[0].TakeDamage(10000);
                    Next(2);
                }
                else if (phase == 0 && elapsed > 0.7)
                {
                    settledMinX = Mathf.Min(settledMinX, body.position.x);
                    settledMaxX = Mathf.Max(settledMaxX, body.position.x);
                }
            }
            else if (phase == 2 && elapsed > 0.2 && RewardsFinished())
            {
                Assert(!bounds.ExitUnlocked, "Exit unlocked with surviving enemies.");
                Assert(saves.TrySave(1, out var message), "Partial save failed: " + message);
                Assert(SaveFileStore.TryRead(saveDirectory, 1, out var data, out message), message);
                Assert(data.room.hasCombatBounds, "Boundary coordinates missing from saved room.");
                Assert(Mathf.Abs(data.room.combatLeftEdge - left) < 0.001f && Mathf.Abs(data.room.combatRightEdge - right) < 0.001f,
                    "Saved boundary coordinates changed.");
                Assert(saves.TryLoad(1, out message), "Partial load failed: " + message);
                Next(3);
            }
            else if (phase == 3 || phase == 6)
            {
                if (SceneManager.GetActiveScene().name != "MainScene") return;
                BindPlayer();
                if (saves.IsLoading || SceneTransition.IsPlaying || bounds == null || !bounds.IsInitialized) return;
                Assert(Mathf.Abs(bounds.LeftEdge - left) < 0.001f && Mathf.Abs(bounds.RightEdge - right) < 0.001f,
                    "Loading moved the room walls.");
                if (phase == 3)
                {
                    Assert(!bounds.ExitUnlocked, "Partially cleared save unlocked the exit.");
                    foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyStats>())
                        if (!enemy.IsDead) enemy.TakeDamage(10000);
                    Next(4);
                }
                else
                {
                    Assert(bounds.ExitUnlocked, "Cleared room save did not restore the exit.");
                    Assert(player.GetComponent<PlayerStats>().Currency == gold, "Loading a defeat duplicated coin rewards.");
                    Vector3 position = player.transform.position;
                    position.x += right - collider.bounds.max.x - 0.4f;
                    player.transform.position = position;
                    body.position = position;
                    Physics2D.SyncTransforms();
                    var cameraPosition = Camera.main.transform.position;
                    cameraPosition.x = right + 100f;
                    Camera.main.transform.position = cameraPosition;
                    bounds.SendMessage("LateUpdate");
                    Next(7);
                }
            }
            else if (phase == 4)
            {
                if (!bounds.ExitUnlocked) return;
                if (!rewardWaitVerified && UnityEngine.Object.FindAnyObjectByType<CoinPickup>() != null)
                {
                    Vector3 originalPosition = player.transform.position;
                    Vector3 outside = originalPosition;
                    outside.x = right + 5f;
                    player.transform.position = outside;
                    body.position = outside;
                    Physics2D.SyncTransforms();
                    bounds.TryExit();
                    player.transform.position = originalPosition;
                    body.position = originalPosition;
                    Physics2D.SyncTransforms();
                    Assert(!UnityEngine.Object.FindAnyObjectByType<RoomSceneController>().IsReturningToMap,
                        "Exit discarded pending coin rewards.");
                    rewardWaitVerified = true;
                }
                if (elapsed > 0.2 && RewardsFinished())
                {
                    Assert(rewardWaitVerified, "Coin reward gate was not exercised.");
                    gold = player.GetComponent<PlayerStats>().Currency;
                    Assert(saves.TrySave(1, out var message), "Cleared save failed: " + message);
                    Assert(saves.TryLoad(1, out message), "Cleared load failed: " + message);
                    Next(6);
                }
            }
            else if (phase == 7)
            {
                if (SceneManager.GetActiveScene().name == "MapScene")
                {
                    var map = RunMapState.Capture();
                    Assert(walkOffVerified && map.rooms[0].completed && !map.roomInProgress, "Walk-off exit did not complete the map room.");
                    Assert(UnityEngine.Object.FindAnyObjectByType<CombatRoomBounds>() == null, "Room boundaries leaked into the map.");
                    Finish(null);
                    return;
                }
                if (player != null)
                {
                    Drive(1f);
                    float half = Camera.main.orthographicSize * Camera.main.aspect;
                    Assert(Camera.main.transform.position.x + half <= right + 0.002f, "Camera looked past the right exit.");
                    bool returning = UnityEngine.Object.FindAnyObjectByType<RoomSceneController>().IsReturningToMap;
                    bool stillVisible = collider.bounds.min.x < right || player.GetComponentsInChildren<SpriteRenderer>()
                        .Any(s => s.enabled && s.sprite != null && s.bounds.min.x < right);
                    if (returning)
                    {
                        Assert(!stillVisible, "Room completed before the player was fully off screen.");
                        walkOffVerified = true;
                    }
                    else if (stillVisible)
                        Assert(!returning, "Room completed while the player was still visible.");
                }
            }
        }
        catch (Exception error) { Finish(error.ToString()); }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static bool RewardsFinished() => UnityEngine.Object.FindAnyObjectByType<CoinPickup>() == null
        && !UnityEngine.Object.FindObjectsByType<EnemyStats>(FindObjectsInactive.Include).Any(e => e.DeathRewardsPending);

    private static void Finish(string error)
    {
        EditorApplication.update -= Step;
        string result = "{\"status\":\"" + (error == null ? "passed" : "failed") + "\",\"phase\":" + phase
            + ",\"leftEdge\":" + left.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ",\"rightEdge\":" + right.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ",\"walkOffVerified\":" + (walkOffVerified ? "true" : "false")
            + ",\"leftWallPositionVariance\":" + (settledMaxX - settledMinX).ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ",\"error\":\"" + (error ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n") + "\"}";
        File.WriteAllText(reportPath, result);
    }
}
