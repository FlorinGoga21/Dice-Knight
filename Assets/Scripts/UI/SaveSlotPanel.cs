using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Controls the authored three-slot dialog; no UI objects are generated at runtime.</summary>
public sealed class SaveSlotPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text message;
    [SerializeField] private UnityEngine.UI.Button[] slotButtons;
    [SerializeField] private UnityEngine.UI.Button[] deleteButtons;
    [Tooltip("Each Label's Text Input is its runtime template. Use {slot}, {action}, {details}, {health}, {maxHealth}, {gold}, {playtime}, {location}, {date}, or {active}. Font, rich text, and layout remain controlled by the Label.")]
    [SerializeField] private TMP_Text[] slotLabels;
    [SerializeField] private UnityEngine.UI.Button backButton;
    [SerializeField] private GameObject confirmationRoot;
    [SerializeField] private TMP_Text confirmationText;
    [SerializeField] private UnityEngine.UI.Button confirmButton;
    [SerializeField] private UnityEngine.UI.Button cancelButton;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private CanvasGroup confirmationGroup;
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
    [SerializeField] private bool inlineStartMenu;

    [Header("Slot Text")]
    [SerializeField] private string newSlotAction = "New Game";
    [SerializeField] private string loadSlotAction = "Load";
    [SerializeField] private string unavailableSlotAction = "Unavailable";
    [SerializeField, TextArea(2, 5)] private string emptyDetailsFormat = "Empty slot";
    [SerializeField, TextArea(2, 5)] private string occupiedDetailsFormat = "{stats}\nTotal time {playtime}  |  {location}\n{date}{backup}";
    [SerializeField, TextArea(2, 5)] private string unavailableDetailsFormat = "This save could not be read";
    [SerializeField] private string playerStatsFormat = "HP {health}/{maxHealth}   Gold {gold}";
    [SerializeField] private string newAdventureText = "New adventure";
    [Tooltip("Standard .NET date format used by the {date} placeholder.")]
    [SerializeField] private string saveDateFormat = "dd.MM.yy";

    private enum Mode { Start, Save, Load }
    private Mode mode;
    private Action<int> startSelected;
    private PauseMenuController pauseOwner;
    private int pendingSlot;
    private bool pendingDelete;
    private GameObject previousSelection;
    private string[] labelTemplates;
    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public bool IsTransitioning { get; private set; }
    public event Action Closed;
    public event Action<int> Saved;

    private void Awake()
    {
        labelTemplates = new string[slotLabels.Length];
        for (int i = 0; i < slotLabels.Length; i++) labelTemplates[i] = slotLabels[i].text;
        foreach (var pixelButton in panelRoot.GetComponentsInChildren<HeyHeyThere.PixelUIFree.PixelButton>(true))
            pixelButton.GetComponent<ButtonHoverFocus>()?.Initialize(pixelButton, pixelButton.focus);
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i + 1;
            slotButtons[i].onClick.AddListener(() => SelectSlot(slot));
            deleteButtons[i].onClick.AddListener(() => RequestDelete(slot));
        }
        backButton.onClick.AddListener(Close);
        confirmButton.onClick.AddListener(ConfirmOverwrite);
        cancelButton.onClick.AddListener(HideConfirmation);
        panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (IsOpen && pauseOwner == null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Back();
    }

    public void ShowStart(Action<int> onSelected) { startSelected = onSelected; pauseOwner = null; Show(Mode.Start); }
    public void ShowSave(PauseMenuController owner) { pauseOwner = owner; Show(Mode.Save); }
    public void ShowLoad(PauseMenuController owner) { pauseOwner = owner; Show(Mode.Load); }

    private void Show(Mode nextMode)
    {
        mode = nextMode;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        StopAllCoroutines();
        panelRoot.SetActive(true);
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        confirmationRoot.SetActive(false);
        pendingDelete = false;
        title.text = mode == Mode.Start ? "Choose a Save Slot" : mode == Mode.Save ? "Save Game" : "Load Game";
        Refresh();
        backButton.gameObject.SetActive(false);
        message.gameObject.SetActive(!inlineStartMenu);
        message.text = mode == Mode.Start ? "Choose a save to continue, or an empty slot to begin." :
            mode == Mode.Save ? "Choose a slot. Press pause to go back." : "Choose a save. Press pause to go back.";
        if (mode == Mode.Save && !SaveManager.Instance.CanSave(out string reason)) message.text = reason;
        StartCoroutine(FadeIn());
    }

    private void Refresh()
    {
        var manager = SaveManager.Instance;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i + 1;
            bool occupied = SaveFileStore.Exists(manager.SaveDirectory, slot);
            bool valid = SaveFileStore.TryRead(manager.SaveDirectory, slot, out var data, out string detail);
            deleteButtons[i].gameObject.SetActive(occupied);
            deleteButtons[i].interactable = occupied;
            slotLabels[i].text = SlotText(labelTemplates[i], slot, occupied, valid ? data : null, detail);
            bool inCombat = FindAnyObjectByType<CombatController>() is CombatController combat && combat.IsInCombat;
            slotButtons[i].interactable = mode == Mode.Save ? manager.CanSave(out _) : mode == Mode.Start ? !occupied || valid : valid && !inCombat;
        }
    }

    private string SlotText(string template, int slot, bool occupied, SaveGameData data, string detail)
    {
        string location = "";
        if (data != null)
        {
            location = data.sceneName == "MapScene" ? "Map" : "Room";
            if (data.map.currentRoom >= 0) location += " " + (data.map.rooms[data.map.currentRoom].column + 1);
        }
        var tokens = new Dictionary<string, string>
        {
            ["slot"] = slot.ToString(),
            ["action"] = !occupied ? newSlotAction : data != null ? loadSlotAction : unavailableSlotAction,
            ["active"] = SaveManager.Instance.ActiveSlot == slot ? "  (Active)" : "",
            ["health"] = data?.player != null ? data.player.health.ToString() : "-",
            ["maxHealth"] = data?.player != null ? data.player.maxHealth.ToString() : "-",
            ["gold"] = data?.player != null ? data.player.gold.ToString() : "-",
            ["playtime"] = data != null ? Playtime(data.playSeconds) : "0h 00m",
            ["location"] = location,
            ["date"] = data != null ? DateTime.Parse(data.savedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime().ToString(saveDateFormat) : "",
            ["backup"] = data != null && detail.Length > 0 ? " (backup)" : ""
        };
        tokens["stats"] = data?.player != null ? Expand(playerStatsFormat, tokens) : newAdventureText;
        tokens["details"] = Expand(data != null ? occupiedDetailsFormat : occupied ? unavailableDetailsFormat : emptyDetailsFormat, tokens);
        return Expand(template, tokens);
    }

    private static string Expand(string format, Dictionary<string, string> tokens)
    {
        string result = format ?? "";
        foreach (var token in tokens) result = result.Replace("{" + token.Key + "}", token.Value);
        return result;
    }

    private static string Playtime(float seconds)
    {
        double minutes = Math.Floor(seconds / 60d);
        return Math.Floor(minutes / 60d).ToString("0") + "h " + ((int)(minutes % 60d)).ToString("00") + "m";
    }

    private void SelectSlot(int slot)
    {
        if (IsTransitioning || confirmationRoot.activeSelf) return;
        if (mode == Mode.Start) { startSelected?.Invoke(slot); return; }
        if (mode == Mode.Load)
        {
            if (SaveManager.Instance.TryLoad(slot, out string error)) Close();
            else message.text = error;
            return;
        }
        if (SaveFileStore.Exists(SaveManager.Instance.SaveDirectory, slot))
        {
            pendingSlot = slot;
            pendingDelete = false;
            confirmationText.text = "Overwrite Slot " + slot + "?\nThe current save will become the backup.";
            ShowConfirmation("Overwrite");
        }
        else Save(slot);
    }

    private void ConfirmOverwrite()
    {
        if (IsTransitioning || !confirmationRoot.activeSelf) return;
        StartCoroutine(CloseConfirmation(true));
    }

    private IEnumerator CloseConfirmation(bool confirmed)
    {
        IsTransitioning = true;
        confirmationGroup.interactable = false;
        yield return Fade(0f, confirmationGroup);
        confirmationRoot.SetActive(false);
        IsTransitioning = false;
        bool delete = pendingDelete;
        pendingDelete = false;
        if (!confirmed)
        {
            Refresh();
            SelectFirstButton();
        }
        else if (delete)
        {
            SaveManager.Instance.TryDeleteSlot(pendingSlot, out string result);
            Refresh();
            ShowMessage(result);
            SelectFirstButton();
        }
        else Save(pendingSlot);
    }

    private void RequestDelete(int slot)
    {
        if (IsTransitioning || confirmationRoot.activeSelf || !SaveFileStore.Exists(SaveManager.Instance.SaveDirectory, slot)) return;
        pendingSlot = slot;
        pendingDelete = true;
        confirmationText.text = "Delete Slot " + slot + "?\nAre you sure?\nThis will delete your save permanently.";
        ShowConfirmation("Delete");
    }

    private void ShowConfirmation(string action)
    {
        if (confirmButton is HeyHeyThere.PixelUIFree.PixelButton pixelButton) pixelButton.text = action;
        foreach (var button in slotButtons) button.interactable = false;
        foreach (var button in deleteButtons) button.interactable = false;
        confirmationRoot.SetActive(true);
        confirmationGroup.alpha = 0f;
        confirmationGroup.interactable = false;
        confirmationGroup.blocksRaycasts = true;
        StartCoroutine(FadeConfirmationIn());
    }

    private IEnumerator FadeConfirmationIn()
    {
        IsTransitioning = true;
        yield return Fade(1f, confirmationGroup);
        IsTransitioning = false;
        confirmationGroup.interactable = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(cancelButton.gameObject);
    }

    private void Save(int slot)
    {
        bool success = SaveManager.Instance.TrySave(slot, out string result);
        Refresh();
        message.text = result;
        if (success) { SelectFirstButton(); Saved?.Invoke(slot); }
    }

    public void ShowMessage(string text) { message.gameObject.SetActive(true); message.text = text; }

    public void Back()
    {
        if (IsTransitioning) return;
        if (confirmationRoot.activeSelf) HideConfirmation();
        else Close();
    }

    private void HideConfirmation()
    {
        if (!IsTransitioning && confirmationRoot.activeSelf) StartCoroutine(CloseConfirmation(false));
    }

    public void Close()
    {
        if (!IsOpen || IsTransitioning) return;
        StartCoroutine(FadeOut());
    }

    public void CloseImmediately()
    {
        if (!IsOpen) return;
        StopAllCoroutines();
        FinishClose();
    }

    private IEnumerator FadeIn()
    {
        IsTransitioning = true;
        yield return Fade(1f);
        IsTransitioning = false;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
        SelectFirstButton();
    }

    private IEnumerator FadeOut()
    {
        IsTransitioning = true;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        yield return Fade(0f);
        FinishClose();
    }

    private IEnumerator Fade(float target, CanvasGroup group = null)
    {
        if (group == null) group = panelGroup;
        float initial = group.alpha;
        for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(initial, target, Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration));
            yield return null;
        }
        group.alpha = target;
    }

    private void FinishClose()
    {
        IsTransitioning = false;
        pendingDelete = false;
        confirmationRoot.SetActive(false);
        panelRoot.SetActive(false);
        if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        Closed?.Invoke();
    }

    private void SelectFirstButton()
    {
        if (EventSystem.current == null) return;
        foreach (var button in slotButtons)
            if (button.interactable) { EventSystem.current.SetSelectedGameObject(button.gameObject); return; }
        EventSystem.current.SetSelectedGameObject(null);
    }
}
