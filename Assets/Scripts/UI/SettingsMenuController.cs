using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Owns the settings overlay and keeps navigation inside its controls.</summary>
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
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private GameObject settingsTitle;
    [SerializeField] private GameObject controlsPage;
    [SerializeField] private UnityEngine.UI.Button controlsButton;
    [SerializeField] private UnityEngine.UI.Button previousResolutionButton;
    [SerializeField] private UnityEngine.UI.Button nextResolutionButton;
    [SerializeField] private TMP_Text resolutionValue;

    private PlayerInputActions input;
    private UnityEngine.UI.Selectable[] choices;
    private UnityEngine.UI.Selectable[] controlsChoices;
    private Vector2Int[] resolutions;
    private int resolutionIndex;
    private Vector2 backButtonPosition;
    private GameObject previousSelection;
    private bool previousInteractable;
    private bool previousBlocksRaycasts;
    public bool IsOpen => overlay != null && overlay.activeSelf;
    public int LastClosedFrame { get; private set; } = -1;
    public event System.Action Closed;

    public void SetOwner(CanvasGroup owner) => mainButtons = owner;

    private void Awake()
    {
        backButtonPosition = ((RectTransform)closeButton.transform).anchoredPosition;
        choices = new UnityEngine.UI.Selectable[] { musicSlider, sfxSlider, previousResolutionButton,
            nextResolutionButton, fullscreenToggle, closeButton, controlsButton };
        controlsChoices = new UnityEngine.UI.Selectable[] { closeButton };
        foreach (var choice in choices)
        {
            var focus = choice.transform.Find("Selection Focus");
            if (focus != null) choice.GetComponent<ButtonHoverFocus>()?.Initialize(choice, focus.GetComponent<UnityEngine.UI.Graphic>());
        }
        musicSlider.onValueChanged.AddListener(SetMusic);
        sfxSlider.onValueChanged.AddListener(SetSfx);
        fullscreenToggle.onValueChanged.AddListener(GamePreferences.SetFullscreen);
        closeButton.onClick.AddListener(Back);
        controlsButton.onClick.AddListener(ShowControls);
        previousResolutionButton.onClick.AddListener(PreviousResolution);
        nextResolutionButton.onClick.AddListener(NextResolution);
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
        closeButton.onClick.RemoveListener(Back);
        controlsButton.onClick.RemoveListener(ShowControls);
        previousResolutionButton.onClick.RemoveListener(PreviousResolution);
        nextResolutionButton.onClick.RemoveListener(NextResolution);
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (input.UI.Cancel.WasPressedThisFrame()) { Back(); return; }
        UISelection.Ensure(overlay.transform, controlsPage.activeSelf ? controlsChoices : choices);
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
        resolutions = GamePreferences.GetAvailableResolutions();
        resolutionIndex = System.Array.IndexOf(resolutions, GamePreferences.Resolution);
        if (resolutionIndex < 0)
        {
            // The window can resize after preferences load, particularly in the Editor.
            resolutionIndex = resolutions.Length;
            System.Array.Resize(ref resolutions, resolutions.Length + 1);
            resolutions[resolutionIndex] = GamePreferences.Resolution;
        }
        RefreshResolution();
        RefreshValues();
        overlay.SetActive(true);
        SetControlsPage(false);
        musicSlider.Select();
    }

    public void ShowControls()
    {
        if (!IsOpen) return;
        SetControlsPage(true);
        closeButton.Select();
    }

    public void Back()
    {
        if (!IsOpen) return;
        if (controlsPage.activeSelf)
        {
            SetControlsPage(false);
            controlsButton.Select();
            GameAudio.Play(GameAudio.Cue.UiBack);
        }
        else Close();
    }

    private void SetControlsPage(bool showControls)
    {
        settingsPage.SetActive(!showControls);
        settingsTitle.SetActive(!showControls);
        controlsPage.SetActive(showControls);
        controlsButton.gameObject.SetActive(!showControls);
        ((RectTransform)closeButton.transform).anchoredPosition = showControls
            ? new Vector2(0f, backButtonPosition.y) : backButtonPosition;
        var navigation = closeButton.navigation;
        navigation.selectOnUp = showControls ? null : fullscreenToggle;
        navigation.selectOnDown = showControls ? null : musicSlider;
        navigation.selectOnRight = showControls ? null : controlsButton;
        closeButton.navigation = navigation;
    }

    private void PreviousResolution() => ChangeResolution(-1);
    private void NextResolution() => ChangeResolution(1);

    private void ChangeResolution(int direction)
    {
        resolutionIndex = (resolutionIndex + direction + resolutions.Length) % resolutions.Length;
        GamePreferences.SetResolution(resolutions[resolutionIndex]);
        RefreshResolution();
    }

    private void RefreshResolution()
    {
        var size = resolutions[resolutionIndex];
        resolutionValue.text = size.x + " x " + size.y;
        previousResolutionButton.interactable = nextResolutionButton.interactable = resolutions.Length > 1;
    }

    public void Close()
    {
        if (!IsOpen) return;
        LastClosedFrame = Time.frameCount;
        overlay.SetActive(false);
        mainButtons.interactable = previousInteractable;
        GameAudio.Play(GameAudio.Cue.UiBack);
        mainButtons.blocksRaycasts = previousBlocksRaycasts;
        GamePreferences.Save();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy
                ? previousSelection : null);
        Closed?.Invoke();
    }

    private void SetMusic(float value) { GamePreferences.SetMusicVolume(value); RefreshValues(); }
    private void SetSfx(float value) { GamePreferences.SetSfxVolume(value); RefreshValues(); }

    private void RefreshValues()
    {
        musicValue.text = Mathf.RoundToInt(musicSlider.value * 100f) + "%";
        sfxValue.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
    }
}
