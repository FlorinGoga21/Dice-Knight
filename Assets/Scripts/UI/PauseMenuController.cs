using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls toggling the pause menu UI and manages game time scaling.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Material pauseBlurMaterial;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
    [SerializeField] private SaveSlotPanel saveSlots;
    [SerializeField] private CanvasGroup primaryActions;
    [SerializeField] private CanvasGroup saveLoadActions;
    [SerializeField] private UnityEngine.UI.Button saveLoadButton;
    [SerializeField] private TMPro.TMP_Text feedback;
    [SerializeField] private float primaryHeight = 78.03f;
    [SerializeField] private float saveLoadHeight = 58.02f;

    private PlayerInputActions inputActions;
    private UnityEngine.UI.RawImage blurImage;
    private Texture2D pausedScreenTexture;
    private Canvas menuCanvas;
    private CanvasGroup menuCanvasGroup;
    private bool originalOverrideSorting;
    private int originalSortingOrder;
    private float originalCanvasAlpha;
    private bool pauseTransitionPending;
    private bool showingSaveLoad;
    private bool leavingScene;
    private bool saveBeforeExit;
    private UnityEngine.UI.Selectable[] primaryChoices;
    private UnityEngine.UI.Selectable[] saveLoadChoices;
    private GameObject selectionBeforePause;

    private bool InCombat => FindAnyObjectByType<CombatController>() is CombatController combat && combat.IsInCombat;

    private void Awake()
    {
        pauseMenu = pauseMenu != null ? pauseMenu : gameObject;
        menuCanvas = GetComponent<Canvas>();
        menuCanvasGroup = GetComponent<CanvasGroup>();
        if (menuCanvasGroup == null)
        {
            menuCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        originalCanvasAlpha = menuCanvasGroup.alpha;
        if (menuCanvas != null)
        {
            originalOverrideSorting = menuCanvas.overrideSorting;
            originalSortingOrder = menuCanvas.sortingOrder;
        }
        CreateBlurImage();
        primaryChoices = primaryActions.GetComponentsInChildren<UnityEngine.UI.Selectable>(true);
        saveLoadChoices = saveLoadActions.GetComponentsInChildren<UnityEngine.UI.Selectable>(true);
        ResetActions();
        saveSlots.Closed += OnSlotsClosed;
        saveSlots.Saved += OnSlotSaved;
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        inputActions ??= new PlayerInputActions();
        inputActions.Player.Pause.Enable();
        inputActions.UI.Cancel.Enable();
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.Pause.Disable();
        inputActions.UI.Cancel.Disable();
    }

    private void OnDestroy()
    {
        if (saveSlots != null) { saveSlots.Closed -= OnSlotsClosed; saveSlots.Saved -= OnSlotSaved; }
        inputActions?.Dispose();
        ReleasePausedScreenTexture();
    }

    public void ReturnToGame()
    {
        StartResume(quitAfterResume: false);
    }

    public void ExitGame()
    {
        if (pauseTransitionPending || leavingScene) return;
        if (!InCombat)
        {
            var manager = SaveManager.Instance;
            if (manager.ActiveSlot == 0)
            {
                saveBeforeExit = true;
                StartCoroutine(OpenSlots(save: true));
                return;
            }
            if (!manager.TrySave(manager.ActiveSlot, out string error)) { ShowFeedback(error); return; }
        }
        leavingScene = true;
        StartResume(quitAfterResume: true);
    }

    private void Update()
    {
        saveLoadButton.interactable = !InCombat && !pauseTransitionPending;
        // Slot panels own Cancel, so one press cannot close two menu levels.
        if ((inputActions.Player.Pause.WasPressedThisFrame() && !inputActions.UI.Cancel.WasPressedThisFrame())
            || (inputActions.UI.Cancel.WasPressedThisFrame() && pauseMenu.activeSelf
                && (saveSlots == null || !saveSlots.IsOpen)))
            HandlePauseInput();
        if (!pauseTransitionPending && pauseMenu.activeSelf && (saveSlots == null || !saveSlots.IsOpen))
            UISelection.Ensure(showingSaveLoad ? saveLoadActions.transform : primaryActions.transform,
                showingSaveLoad ? saveLoadChoices : primaryChoices);
    }

    public void ShowSaveLoad()
    {
        if (InCombat || pauseTransitionPending) return;
        StartCoroutine(SwitchActions(true));
    }

    public void SaveGame()
    {
        if (InCombat || pauseTransitionPending) return;
        StartCoroutine(OpenSlots(save: true));
    }

    public void LoadGame()
    {
        if (InCombat || pauseTransitionPending) return;
        StartCoroutine(OpenSlots(save: false));
    }

    private IEnumerator SwitchActions(bool showSaveLoad)
    {
        pauseTransitionPending = true;
        CanvasGroup from = showingSaveLoad ? saveLoadActions : primaryActions;
        CanvasGroup to = showSaveLoad ? saveLoadActions : primaryActions;
        from.interactable = false;
        from.blocksRaycasts = false;
        yield return FadeGroup(from, 0f);
        from.gameObject.SetActive(false);
        showingSaveLoad = showSaveLoad;
        ((RectTransform)pauseMenu.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, showSaveLoad ? saveLoadHeight : primaryHeight);
        to.interactable = false;
        to.blocksRaycasts = false;
        to.gameObject.SetActive(true);
        to.alpha = 0f;
        yield return FadeGroup(to, 1f);
        to.interactable = true;
        to.blocksRaycasts = true;
        SelectFirst(to);
        feedback.gameObject.SetActive(false);
        pauseTransitionPending = false;
    }

    private IEnumerator OpenSlots(bool save)
    {
        pauseTransitionPending = true;
        CanvasGroup actions = showingSaveLoad ? saveLoadActions : primaryActions;
        actions.interactable = false;
        actions.blocksRaycasts = false;
        yield return FadeGroup(actions, 0f);
        pauseMenu.SetActive(false);
        if (save) saveSlots.ShowSave(this); else saveSlots.ShowLoad(this);
        while (saveSlots.IsTransitioning) yield return null;
        pauseTransitionPending = false;
    }

    private void OnSlotsClosed()
    {
        saveBeforeExit = false;
        if (leavingScene || Time.timeScale > 0f) return;
        StartCoroutine(RestoreActions());
    }

    private IEnumerator RestoreActions()
    {
        pauseTransitionPending = true;
        pauseMenu.SetActive(true);
        CanvasGroup actions = showingSaveLoad ? saveLoadActions : primaryActions;
        yield return FadeGroup(actions, 1f);
        actions.interactable = true;
        actions.blocksRaycasts = true;
        SelectFirst(actions);
        pauseTransitionPending = false;
    }

    private void OnSlotSaved(int slot)
    {
        if (!saveBeforeExit) return;
        leavingScene = true;
        saveSlots.CloseImmediately();
        pauseMenu.SetActive(true);
        StartResume(quitAfterResume: true);
    }

    private void ResetActions()
    {
        showingSaveLoad = false;
        primaryActions.gameObject.SetActive(true);
        primaryActions.alpha = 1f;
        primaryActions.interactable = true;
        primaryActions.blocksRaycasts = true;
        saveLoadActions.gameObject.SetActive(false);
        saveLoadActions.alpha = 0f;
        ((RectTransform)pauseMenu.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, primaryHeight);
        feedback.gameObject.SetActive(false);
    }

    private void ShowFeedback(string message)
    {
        feedback.text = message;
        feedback.gameObject.SetActive(true);
    }

    private static void SelectFirst(CanvasGroup group)
    {
        foreach (var button in group.GetComponentsInChildren<UnityEngine.UI.Button>())
            if (button.interactable) { button.Select(); return; }
    }

    public void PrepareForSceneLoad()
    {
        StopAllCoroutines();
        leavingScene = true;
        saveSlots?.CloseImmediately();
        SetPaused(false);
        pauseTransitionPending = false;
    }

    private void HandlePauseInput()
    {
        if (pauseTransitionPending || leavingScene) return;
        if (saveSlots != null && saveSlots.IsOpen) { saveSlots.Back(); return; }
        if (HeyHeyThere.SceneTransitionsFree.SceneTransition.IsPlaying || (SaveManager.Instance != null && SaveManager.Instance.IsLoading)) return;
        var map = FindAnyObjectByType<RunMapController>();
        if (map != null && map.IsBusy) return;
        if (pauseMenu.activeSelf)
        {
            if (showingSaveLoad) StartCoroutine(SwitchActions(false));
            else StartResume(quitAfterResume: false);
        }
        else
        {
            StartCoroutine(PauseAfterEndOfFrame());
        }
    }

    private void SetPaused(bool paused)
    {
        if (!paused)
        {
            ReleasePausedScreenTexture();
            menuCanvasGroup.alpha = originalCanvasAlpha;
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }

        pauseMenu.SetActive(paused);
        if (menuCanvas != null)
        {
            if (paused)
            {
                menuCanvas.overrideSorting = true;
                menuCanvas.sortingOrder = 900;
            }
            else
            {
                menuCanvas.overrideSorting = originalOverrideSorting;
                menuCanvas.sortingOrder = originalSortingOrder;
            }
        }
        if (blurImage != null)
        {
            blurImage.gameObject.SetActive(paused && pausedScreenTexture != null);
        }
        Time.timeScale = paused ? 0f : 1f;
        GameAudio.Instance?.SetPaused(paused);
    }

    private IEnumerator PauseAfterEndOfFrame()
    {
        pauseTransitionPending = true;
        selectionBeforePause = UnityEngine.EventSystems.EventSystem.current != null
            ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
        yield return new WaitForEndOfFrame();

        CapturePausedScreen();
        menuCanvasGroup.alpha = 0f;
        ResetActions();
        SetPaused(true);
        menuCanvasGroup.interactable = false;
        menuCanvasGroup.blocksRaycasts = false;
        yield return FadeCanvasGroup(originalCanvasAlpha);
        menuCanvasGroup.interactable = true;
        menuCanvasGroup.blocksRaycasts = true;
        SelectFirst(primaryActions);
        pauseTransitionPending = false;
    }

    private void StartResume(bool quitAfterResume)
    {
        if (pauseTransitionPending || !pauseMenu.activeSelf)
        {
            return;
        }

        StartCoroutine(ResumeAfterFade(quitAfterResume));
    }

    private IEnumerator ResumeAfterFade(bool quitAfterResume)
    {
        pauseTransitionPending = true;
        menuCanvasGroup.interactable = false;
        menuCanvasGroup.blocksRaycasts = false;
        yield return FadeCanvasGroup(0f);

        SetPaused(false);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(
                selectionBeforePause != null && selectionBeforePause.activeInHierarchy ? selectionBeforePause : null);
        pauseTransitionPending = false;

        if (quitAfterResume)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha)
    {
        yield return FadeGroup(menuCanvasGroup, targetAlpha);
    }

    private IEnumerator FadeGroup(CanvasGroup group, float targetAlpha)
    {
        float startAlpha = group.alpha;
        if (fadeDuration <= 0f)
        {
            group.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration));
            yield return null;
        }

        group.alpha = targetAlpha;
    }

    private void CreateBlurImage()
    {
        if (pauseBlurMaterial == null || pauseMenu.transform.parent == null)
        {
            return;
        }

        GameObject blurObject = new GameObject("Pause Blur", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.RawImage));
        blurObject.layer = pauseMenu.layer;
        blurObject.transform.SetParent(pauseMenu.transform.parent, false);
        blurObject.transform.SetAsFirstSibling();

        RectTransform rect = (RectTransform)blurObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        blurImage = blurObject.GetComponent<UnityEngine.UI.RawImage>();
        blurImage.material = pauseBlurMaterial;
        blurImage.raycastTarget = true;
        blurObject.SetActive(false);
    }

    private void CapturePausedScreen()
    {
        if (blurImage == null)
        {
            return;
        }

        ReleasePausedScreenTexture();
        pausedScreenTexture = ScreenCapture.CaptureScreenshotAsTexture();
        if (pausedScreenTexture != null)
        {
            blurImage.texture = pausedScreenTexture;
        }
    }

    private void ReleasePausedScreenTexture()
    {
        if (blurImage != null)
        {
            blurImage.texture = null;
        }

        if (pausedScreenTexture != null)
        {
            Destroy(pausedScreenTexture);
            pausedScreenTexture = null;
        }
    }
}
