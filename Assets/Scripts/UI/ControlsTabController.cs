using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Shows bindings for the last device used and scrolls the read-only controls list.</summary>
public sealed class ControlsTabController : MonoBehaviour
{
    [Serializable]
    public sealed class BindingRow
    {
        public string actionPath;
        public string additionalActionPath;
        public TMP_Text glyphs;
    }

    [Header("---Controls Page---")]
    [SerializeField] private GameObject page;
    [Tooltip("The same action asset used by PlayerInputActions and the UI input module.")]
    [SerializeField] private InputActionAsset actions;
    [SerializeField] private TMP_SpriteAsset inputSprites;
    [SerializeField] private TMP_Text schemeLabel;
    [SerializeField] private UnityEngine.UI.ScrollRect scrollRect;
    [SerializeField] private BindingRow[] rows;

    [Header("---Scrolling---")]
    [Tooltip("Controls-list canvas units scrolled per unscaled second while navigating vertically.")]
    [SerializeField, Min(1f)] private float scrollSpeed = 100f;

    private PlayerInputActions input;
    private InputDevice activeDevice;
    private bool refreshPending = true;
    private bool wasVisible;

    private void OnEnable()
    {
        input ??= new PlayerInputActions();
        input.UI.Navigate.Enable();
        InputSystem.onEvent += OnInputEvent;
        InputSystem.onDeviceChange += OnDeviceChange;
        refreshPending = true;
    }

    private void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
        InputSystem.onDeviceChange -= OnDeviceChange;
        input?.UI.Navigate.Disable();
    }

    private void OnDestroy() => input?.Dispose();

    private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (eventPtr.type != StateEvent.Type && eventPtr.type != DeltaStateEvent.Type) return;
        if (!(device is Gamepad) && !(device is Keyboard) && !(device is Mouse)) return;
        bool meaningful = false;
        if (device is Mouse mouse)
        {
            meaningful = eventPtr.HasButtonPress()
                || (mouse.delta.ReadValueFromEvent(eventPtr, out Vector2 delta) && delta.sqrMagnitude > 4f)
                || (mouse.scroll.ReadValueFromEvent(eventPtr, out Vector2 wheel) && wheel.sqrMagnitude > 0.1f);
        }
        else
        {
            // Changed, actuated controls ignore releases, resting sticks, and noisy sensors.
            foreach (var control in eventPtr.EnumerateChangedControls(device, 0.5f))
            {
                meaningful = true;
                break;
            }
        }
        if (!meaningful || activeDevice == device) return;
        bool changedScheme = activeDevice is Gamepad || device is Gamepad;
        activeDevice = device;
        refreshPending |= changedScheme;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device != activeDevice || (change != InputDeviceChange.Removed
            && change != InputDeviceChange.Disconnected && change != InputDeviceChange.Disabled)) return;
        activeDevice = null;
        refreshPending = true;
    }

    private void Update()
    {
        bool visible = page.activeInHierarchy;
        if (visible && (!wasVisible || refreshPending))
        {
            RefreshBindings(activeDevice as Gamepad);
            if (!wasVisible) scrollRect.verticalNormalizedPosition = 1f;
        }
        wasVisible = visible;
        if (!visible) return;
        float direction = input.UI.Navigate.ReadValue<Vector2>().y;
        // Page keys supplement normal arrows/WASD and gamepad menu navigation.
        if (!(activeDevice is Gamepad) && Keyboard.current != null)
        {
            if (Keyboard.current.pageUpKey.isPressed) direction = 1f;
            else if (Keyboard.current.pageDownKey.isPressed) direction = -1f;
        }
        float overflow = scrollRect.content.rect.height - scrollRect.viewport.rect.height;
        if (overflow > 0f && Mathf.Abs(direction) > 0.2f)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition
                + direction * scrollSpeed * Time.unscaledDeltaTime / overflow);
        }
    }

    public void RefreshBindings(Gamepad gamepad)
    {
        schemeLabel.text = ControlGlyphs.SchemeName(gamepad);
        foreach (var row in rows)
        {
            row.glyphs.spriteAsset = inputSprites;
            string text = ControlGlyphs.Bindings(actions.FindAction(row.actionPath), gamepad, inputSprites);
            if (!string.IsNullOrEmpty(row.additionalActionPath))
            {
                string extra = ControlGlyphs.Bindings(actions.FindAction(row.additionalActionPath), gamepad, inputSprites);
                if (extra != "Unbound" && extra != text) text += "  " + extra;
            }
            row.glyphs.text = text;
        }
        // Rebuild only when the scheme/page changes, never for every input event or frame.
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        refreshPending = false;
    }
}
