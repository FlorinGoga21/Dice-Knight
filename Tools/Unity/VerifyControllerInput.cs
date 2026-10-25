using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using HeyHeyThere.SceneTransitionsFree;

/// <summary>Repeatable Play mode smoke test using real queued gamepad events and isolated save fixtures.</summary>
public sealed class VerifyControllerInput : MonoBehaviour
{
    private Gamepad pad;
    private string originalSaveDirectory;
    private readonly List<string> passed = new List<string>();
    private readonly List<string> failed = new List<string>();
    private const string ReportPath = "output/controller-verification.json";
    private string phase;

    public static object Main()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().name != "MainMenu")
            throw new InvalidOperationException("Start Play mode in MainMenu first.");
        var go = new GameObject("Controller Verification (temporary)");
        DontDestroyOnLoad(go);
        go.AddComponent<VerifyControllerInput>();
        return new { started = true, report = ReportPath };
    }

    private void Start()
    {
        originalSaveDirectory = SaveManager.Instance.SaveDirectory;
        string fixture = Path.GetFullPath("Temp/ControllerVerification/" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fixture);
        typeof(SaveManager).GetField("<SaveDirectory>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(SaveManager.Instance, fixture);
        // An occupied slot exercises Delete/Cancel without touching any user saves.
        SaveFileStore.TryWrite(fixture, new SaveGameData { slot = 2, savedUtc = DateTime.UtcNow.ToString("O") }, out _);
        pad = InputSystem.AddDevice<Gamepad>("ControllerVerificationPad");
        StartCoroutine(Guard(Run()));
    }

    private IEnumerator Guard(IEnumerator test)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(test);
        while (stack.Count > 0)
        {
            object next;
            try
            {
                if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                next = stack.Peek().Current;
            }
            catch (Exception error) { failed.Add(phase + ": " + error.Message); break; }
            if (next is IEnumerator nested) stack.Push(nested);
            else yield return next;
        }
        if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
        Directory.CreateDirectory("output");
        File.WriteAllText(ReportPath, Newtonsoft.Json.JsonConvert.SerializeObject(new { passed, failed, phase, frame = Time.frameCount }, Newtonsoft.Json.Formatting.Indented));
        Debug.Log("Controller verification: " + passed.Count + " passed, " + failed.Count + " failed. " + ReportPath);
    }

    private void Check(bool condition, string description)
    {
        phase = description;
        if (!condition) throw new InvalidOperationException(description);
        passed.Add(description);
    }

    private IEnumerator Wait(Func<bool> predicate, string description, float timeout = 12f)
    {
        phase = description;
        float end = Time.realtimeSinceStartup + timeout;
        while (!predicate())
        {
            if (Time.realtimeSinceStartup > end) throw new TimeoutException(description);
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.4f);
    }

    private IEnumerator Press(GamepadButton button)
    {
        InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button));
        yield return new WaitForSecondsRealtime(0.12f);
        InputSystem.QueueStateEvent(pad, new GamepadState());
        yield return new WaitForSecondsRealtime(0.6f);
        // The authored pause fade is 0.4s per group, so submenu swaps take two fades.
        float end = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < end)
        {
            var pause = Find<PauseMenuController>();
            var slots = Find<SaveSlotPanel>();
            if ((pause == null || !Get<bool>(pause, "pauseTransitionPending"))
                && (slots == null || !slots.IsTransitioning)) break;
            yield return null;
        }
    }

    private string Selected => EventSystem.current?.currentSelectedGameObject?.name;
    private T Find<T>() where T : UnityEngine.Object => FindAnyObjectByType<T>(FindObjectsInactive.Include);
    private bool PanelOpen => Find<SaveSlotPanel>() != null && Find<SaveSlotPanel>().IsOpen;
    private bool ConfirmationOpen => Find<SaveSlotPanel>() != null && (bool)Get<GameObject>(Find<SaveSlotPanel>(), "confirmationRoot").activeSelf;
    private T Get<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    private IEnumerator NavigateTo(string name)
    {
        for (int i = 0; i < 8 && Selected != name; i++) yield return Press(GamepadButton.DpadDown);
        Check(Selected == name, "Controller can reach " + name);
    }

    private IEnumerator Run()
    {
        int frame = Time.frameCount;
        yield return new WaitForSecondsRealtime(0.3f);
        Check(Time.frameCount > frame, "Live game frames advance");
        Check(Selected == "StartButton", "Main menu has initial focus");
        yield return Press(GamepadButton.DpadDown);
        Check(Selected == "ExitButton", "D-pad navigates main menu");
        yield return Press(GamepadButton.DpadUp);
        yield return Press(GamepadButton.South);
        Check(PanelOpen && Selected == "Slot 1", "South opens save slots and selects first slot");
        yield return Press(GamepadButton.DpadRight);
        // Traverse toward the occupied second slot and its Delete button using the authored layout.
        if (Selected != "Slot 2") yield return NavigateTo("Slot 2");
        yield return Press(GamepadButton.DpadRight);
        Check(Selected == "Delete Save", "Controller reaches occupied slot delete action");
        yield return Capture("delete-controller-focus");
        yield return Press(GamepadButton.South);
        Check(ConfirmationOpen && Selected == "Cancel", "Destructive confirmation defaults to Cancel");
        yield return Press(GamepadButton.East);
        Check(PanelOpen && !ConfirmationOpen, "East dismisses only the confirmation");
        yield return Press(GamepadButton.East);
        Check(!PanelOpen && Selected == "StartButton", "East returns from slots to main menu");
        yield return Press(GamepadButton.South);
        yield return Press(GamepadButton.South);
        yield return Wait(() => SceneManager.GetActiveScene().name == "MapScene" && !SceneTransition.IsPlaying, "New slot transitions to map");
        Check(SceneManager.GetActiveScene().name == "MapScene", "Controller starts a new run");
        Check(EventSystem.current.currentSelectedGameObject?.GetComponent<UnityEngine.UI.Button>()?.IsInteractable() == true,
            "Map selects a reachable room");
        yield return Capture("map-controller-focus");
        var selectedRoom = EventSystem.current.currentSelectedGameObject;
        var roomButton = selectedRoom.GetComponent<UnityEngine.UI.Button>();
        if (roomButton.navigation.selectOnUp != null)
        {
            yield return Press(GamepadButton.DpadUp);
            Check(EventSystem.current.currentSelectedGameObject != selectedRoom, "D-pad switches reachable map rooms");
        }
        EventSystem.current.SetSelectedGameObject(null);
        yield return null;
        yield return null;
        Check(EventSystem.current.currentSelectedGameObject != null, "Map recovers lost focus");
        yield return Press(GamepadButton.Start);
        Check(Time.timeScale == 0f, "Start opens pause on map");
        yield return NavigateTo(Get<UnityEngine.UI.Button>(Find<PauseMenuController>(), "saveLoadButton").name);
        yield return Press(GamepadButton.South);
        var saveActions = Get<CanvasGroup>(Find<PauseMenuController>(), "saveLoadActions");
        string saveButton = saveActions.GetComponentsInChildren<UnityEngine.UI.Button>().First(b =>
            Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == "SaveGame")).name;
        yield return NavigateTo(saveButton);
        yield return Press(GamepadButton.South);
        Check(PanelOpen && Time.timeScale == 0f, "Controller opens pause save slots");
        yield return Press(GamepadButton.South);
        Check(ConfirmationOpen && Selected == "Cancel", "Overwrite confirmation defaults to Cancel");
        yield return Press(GamepadButton.East);
        Check(PanelOpen && !ConfirmationOpen && Time.timeScale == 0f, "East closes only overwrite confirmation");
        yield return Press(GamepadButton.East);
        Check(!PanelOpen && Get<bool>(Find<PauseMenuController>(), "showingSaveLoad") && Time.timeScale == 0f,
            "East returns from slots to pause save submenu");
        yield return Press(GamepadButton.East);
        Check(Time.timeScale == 0f && !Get<bool>(Find<PauseMenuController>(), "showingSaveLoad"), "East returns one pause submenu level");
        yield return Press(GamepadButton.East);
        Check(Time.timeScale == 1f, "East resumes from pause");
        // Choose a reachable combat room for the remaining checks; random/shop rooms may be empty.
        for (int i = 0; i < 6 && Selected != "Enemy Node"; i++) yield return Press(GamepadButton.DpadUp);
        for (int i = 0; i < 6 && Selected != "Enemy Node"; i++) yield return Press(GamepadButton.DpadDown);
        Check(Selected == "Enemy Node", "Controller reaches a combat map node");
        yield return Press(GamepadButton.South);
        yield return Wait(() => SceneManager.GetActiveScene().name == "MainScene" && !SceneTransition.IsPlaying, "Enter selected room");
        Check(SceneManager.GetActiveScene().name == "MainScene", "Controller enters selected room");
        Check(GameObject.Find("Controller Controls") == null, "Rooms have no control instruction text");
        var player = Find<PlayerController>();
        float startX = player.transform.position.x;
        InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.right });
        yield return new WaitForSecondsRealtime(0.3f);
        InputSystem.QueueStateEvent(pad, new GamepadState());
        yield return new WaitForSecondsRealtime(0.2f);
        Check(player.transform.position.x > startX, "Left stick moves player");
        yield return Press(GamepadButton.North);
        Check(SceneManager.GetActiveScene().name == "MainScene", "North cannot skip living enemies");
        var combat = Find<CombatController>();
        yield return Wait(() => combat.IsInCombat, "Wait for turn combat", 30f);
        yield return Capture("combat-controller");
        var dice = Find<DiceRollUI>();
        yield return Press(GamepadButton.Start);
        Check(Time.timeScale == 0f, "Start pauses during combat");
        int potions = Get<int>(combat, "healingPotionsRemaining");
        bool blocked = Get<bool>(combat, "blockChosenThisTurn");
        yield return Press(GamepadButton.West);
        Check(potions == Get<int>(combat, "healingPotionsRemaining") && !dice.IsRolling, "Paused controller input cannot heal or roll");
        yield return Press(GamepadButton.East);
        Check(Time.timeScale == 1f && blocked == Get<bool>(combat, "blockChosenThisTurn"), "East resumes without spending a block");
        yield return Press(GamepadButton.East);
        Check(Get<int>(combat, "blockUsesRemaining") < 3 && Time.timeScale == 1f, "East blocks without opening pause");
        yield return Wait(() => Get<bool>(combat, "waitingForPlayerRoll") && !Get<bool>(combat, "blockChosenThisTurn"), "Next player turn", 20f);
        // Keep the fixture alive and injured to verify one potion per press.
        var stats = Find<PlayerStats>();
        stats.TakeDamage(10);
        potions = Get<int>(combat, "healingPotionsRemaining");
        yield return Press(GamepadButton.West);
        Check(Get<int>(combat, "healingPotionsRemaining") == potions - 1, "West consumes exactly one healing potion");
        yield return Press(GamepadButton.South);
        Check(dice.IsRolling || !Get<bool>(combat, "waitingForPlayerRoll"), "South rolls combat dice");
        Check(!Get<bool>(combat, "blockChosenThisTurn"), "South cannot submit a different combat action");
        yield return new WaitForSecondsRealtime(3f);
        foreach (var enemy in FindObjectsByType<EnemyStats>())
            if (!enemy.IsDead) enemy.TakeDamage(10000);
        yield return Wait(() => !combat.IsInCombat && FindAnyObjectByType<CoinPickup>() == null
            && FindObjectsByType<EnemyStats>().All(enemy => !enemy.DeathRewardsPending), "Resolve fixture room and rewards", 20f);
        yield return Press(GamepadButton.North);
        yield return Wait(() => SceneManager.GetActiveScene().name == "MapScene" && !SceneTransition.IsPlaying, "Return to map with North");
        var mapState = RunMapState.Capture();
        Check(mapState.currentRoom >= 0 && !mapState.roomInProgress, "North completes cleared room and returns to map");
    }

    private void OnDestroy()
    {
        if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
        if (SaveManager.Instance != null && originalSaveDirectory != null)
            typeof(SaveManager).GetField("<SaveDirectory>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(SaveManager.Instance, originalSaveDirectory);
    }

    private IEnumerator Capture(string name)
    {
        Directory.CreateDirectory("output/previews");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot("output/previews/" + name + ".png");
        yield return new WaitForSecondsRealtime(0.2f);
    }
}
