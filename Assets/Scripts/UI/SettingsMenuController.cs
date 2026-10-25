using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Owns the main menu settings overlay and keeps navigation inside its controls.</summary>
public sealed class SettingsMenuController : MonoBehaviour
{
    [Header("---Settings Overlay---")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private CanvasGroup mainButtons;
    [SerializeField] private UnityEngine.UI.Slider musicSlider;
    [SerializeField] private UnityEngine.UI.Slider sfxSlider;
    [SerializeField] private UnityEngine.UI.Toggle fullscreenToggle;
    [SerializeField] private UnityEngine.UI.Button closeButton;
    [SerializeField] private TMP_Text musicValue;
    [SerializeField] private TMP_Text sfxValue;

    private PlayerInputActions input;
    private UnityEngine.UI.Selectable[] choices;
    private GameObject previousSelection;
    private bool previousInteractable;
    private bool previousBlocksRaycasts;
    public bool IsOpen => overlay != null && overlay.activeSelf;

    private void Awake()
    {
        choices = new UnityEngine.UI.Selectable[] { musicSlider, sfxSlider, fullscreenToggle, closeButton };
        foreach (var choice in choices)
        {
            var focus = choice.transform.Find("Selection Focus");
            if (focus != null) choice.GetComponent<ButtonHoverFocus>()?.Initialize(choice, focus.GetComponent<UnityEngine.UI.Graphic>());
        }
        musicSlider.onValueChanged.AddListener(SetMusic);
        sfxSlider.onValueChanged.AddListener(SetSfx);
        fullscreenToggle.onValueChanged.AddListener(GamePreferences.SetFullscreen);
        closeButton.onClick.AddListener(Close);
        overlay.SetActive(false);
    }

    private void OnEnable()
    {
        input ??= new PlayerInputActions();
        input.UI.Cancel.Enable();
    }

    private void OnDisable()
    {
        input?.UI.Cancel.Disable();
        if (IsOpen) Close();
    }

    private void OnDestroy()
    {
        input?.Dispose();
        musicSlider.onValueChanged.RemoveListener(SetMusic);
        sfxSlider.onValueChanged.RemoveListener(SetSfx);
        fullscreenToggle.onValueChanged.RemoveListener(GamePreferences.SetFullscreen);
        closeButton.onClick.RemoveListener(Close);
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (input.UI.Cancel.WasPressedThisFrame()) { Close(); return; }
        UISelection.Ensure(overlay.transform, choices);
    }

    public void Show()
    {
        if (IsOpen) return;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        previousInteractable = mainButtons.interactable;
        previousBlocksRaycasts = mainButtons.blocksRaycasts;
        mainButtons.interactable = false;
        mainButtons.blocksRaycasts = false;
        musicSlider.SetValueWithoutNotify(GamePreferences.MusicVolume);
        sfxSlider.SetValueWithoutNotify(GamePreferences.SfxVolume);
        fullscreenToggle.SetIsOnWithoutNotify(GamePreferences.Fullscreen);
        RefreshValues();
        overlay.SetActive(true);
        musicSlider.Select();
    }

    public void Close()
    {
        if (!IsOpen) return;
        overlay.SetActive(false);
        mainButtons.interactable = previousInteractable;
        GameAudio.Play(GameAudio.Cue.UiBack);
        mainButtons.blocksRaycasts = previousBlocksRaycasts;
        GamePreferences.Save();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy
                ? previousSelection : null);
    }

    private void SetMusic(float value) { GamePreferences.SetMusicVolume(value); RefreshValues(); }
    private void SetSfx(float value) { GamePreferences.SetSfxVolume(value); RefreshValues(); }

    private void RefreshValues()
    {
        musicValue.text = Mathf.RoundToInt(musicSlider.value * 100f) + "%";
        sfxValue.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
    }
}
