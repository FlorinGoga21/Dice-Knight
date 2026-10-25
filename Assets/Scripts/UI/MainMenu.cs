using System.Collections;
using HeyHeyThere.SceneTransitionsFree;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("---Menu---")]
    [SerializeField] private CanvasGroup uiGroup;
    [SerializeField] private RectTransform logo;
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private SaveSlotPanel saveSlots;
    [SerializeField] private CanvasGroup mainButtons;
    [SerializeField, Min(0f)] private float buttonSwapDuration = 0.25f;
    [SerializeField, Min(0f)] private float fadeDuration = 1.3f;
    [SerializeField, Min(0f)] private float logoBobHeight = 8f;
    [SerializeField, Min(0.01f)] private float logoBobSpeed = 0.55f;

    [Header("Start transition")]
    [SerializeField] private string transitionStyle = "clouds";
    [SerializeField, Min(0.01f)] private float cameraOffsetDropSpeed = 3f;
    [SerializeField, Min(0f)] private float cameraFollowOffsetAtMenu = 2.5f;

    private DummyPlayerWalk dummyPlayer;
    private Camera menuCamera;
    private Vector3 logoStartPosition;
    private bool isLeavingMenu;
    private bool isSwitchingButtons;
    private bool choosingSlot;
    private UnityEngine.UI.Selectable[] menuChoices;

    private void Awake()
    {
        if (uiGroup == null)
        {
            uiGroup = GetComponent<CanvasGroup>();
            if (uiGroup == null)
                uiGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (logo == null)
            logo = FindInChildren<RectTransform>("Logo");
        if (startButton == null)
            startButton = FindInChildren<Button>("StartButton");
        if (exitButton == null)
            exitButton = FindInChildren<Button>("ExitButton");
        menuChoices = new UnityEngine.UI.Selectable[] { startButton, exitButton };

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            dummyPlayer = playerObject.GetComponentInChildren<DummyPlayerWalk>(true);

        menuCamera = Camera.main;

        if (logo != null)
            logoStartPosition = logo.anchoredPosition3D;

        if (startButton != null)
            startButton.onClick.AddListener(OnStartPressed);
        else
            Debug.LogError("MainMenu: Could not find a Button named StartButton under the UI object.", this);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitPressed);
        else
            Debug.LogError("MainMenu: Could not find a Button named ExitButton under the UI object.", this);

        uiGroup.alpha = 0f;
        uiGroup.interactable = false;
        uiGroup.blocksRaycasts = false;
        if (saveSlots != null) saveSlots.Closed += OnSlotsClosed;
    }

    private void Start()
    {
        if (dummyPlayer != null)
            dummyPlayer.SetCameraOffset(cameraFollowOffsetAtMenu);

        StartCoroutine(FadeUI(1f));
    }

    private void Update()
    {
        if (!isLeavingMenu && !isSwitchingButtons && !choosingSlot && uiGroup.interactable)
            UISelection.Ensure(mainButtons.transform, menuChoices);

        if (logo == null || isLeavingMenu)
            return;

        float bob = Mathf.Sin(Time.unscaledTime * logoBobSpeed * Mathf.PI * 2f) * logoBobHeight;
        logo.anchoredPosition3D = logoStartPosition + Vector3.up * bob;
    }

    private void OnDestroy()
    {
        if (saveSlots != null) saveSlots.Closed -= OnSlotsClosed;
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartPressed);
        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitPressed);
    }

    private void OnStartPressed()
    {
        if (isLeavingMenu || isSwitchingButtons || choosingSlot)
            return;

        if (saveSlots == null) { Debug.LogError("MainMenu: Save slot panel is not assigned.", this); return; }
        StartCoroutine(ShowSaveSlots());
    }

    private void OnSlotsClosed()
    {
        if (!isLeavingMenu) StartCoroutine(RestoreMainButtons());
    }

    private IEnumerator ShowSaveSlots()
    {
        isSwitchingButtons = true;
        choosingSlot = true;
        yield return FadeButtons(0f);
        mainButtons.gameObject.SetActive(false);
        saveSlots.ShowStart(SelectSaveSlot);
        while (saveSlots.IsTransitioning) yield return null;
        isSwitchingButtons = false;
    }

    private IEnumerator RestoreMainButtons()
    {
        isSwitchingButtons = true;
        mainButtons.gameObject.SetActive(true);
        yield return FadeButtons(1f);
        choosingSlot = false;
        isSwitchingButtons = false;
        startButton.Select();
    }

    private IEnumerator FadeButtons(float target)
    {
        mainButtons.interactable = false;
        mainButtons.blocksRaycasts = false;
        float initial = mainButtons.alpha;
        for (float elapsed = 0f; elapsed < buttonSwapDuration; elapsed += Time.unscaledDeltaTime)
        {
            mainButtons.alpha = Mathf.Lerp(initial, target, Mathf.SmoothStep(0f, 1f, elapsed / buttonSwapDuration));
            yield return null;
        }
        mainButtons.alpha = target;
        mainButtons.interactable = target > 0f;
        mainButtons.blocksRaycasts = target > 0f;
    }

    private void SelectSaveSlot(int slot)
    {
        if (!SaveManager.Instance.TryStartSlot(slot, out string scene, out string error))
        {
            saveSlots.ShowMessage(error);
            return;
        }
        isLeavingMenu = true;
        saveSlots.CloseImmediately();
        SetButtonsInteractable(false);
        StartCoroutine(LeaveMenuForMainScene(scene));
    }

    private void OnExitPressed()
    {
        if (isLeavingMenu)
            return;

        isLeavingMenu = true;
        SetButtonsInteractable(false);
        StartCoroutine(FadeOutAndExit());
    }

    private IEnumerator LeaveMenuForMainScene(string destination)
    {
        Coroutine fadeOut = StartCoroutine(FadeUI(0f));

        if (dummyPlayer != null && menuCamera != null)
        {
            float targetOffset = -(menuCamera.orthographicSize * menuCamera.aspect + 0.5f);

            while (!IsDummyOffscreenToRight())
            {
                float nextOffset = Mathf.MoveTowards(
                    dummyPlayer.CameraFollowOffset,
                    targetOffset,
                    cameraOffsetDropSpeed * Time.deltaTime);
                dummyPlayer.SetCameraOffset(nextOffset);
                yield return null;
            }
        }

        if (fadeOut != null)
            yield return fadeOut;

        if (!Application.CanStreamedLevelBeLoaded(destination))
        {
            Debug.LogError($"MainMenu: Scene '{destination}' is not in Build Settings.", this);
            isLeavingMenu = false;
            yield break;
        }

        // ChangeScene covers with Cloud, loads the scene while covered, then reveals backwards.
        yield return SceneTransition.ChangeScene(
            destination,
            transitionStyle,
            new SceneTransition.Options { Duration = 0.8f });
    }

    private IEnumerator FadeOutAndExit()
    {
        yield return FadeUI(0f);
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private IEnumerator FadeUI(float targetAlpha)
    {
        float startAlpha = uiGroup.alpha;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            uiGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        uiGroup.alpha = targetAlpha;
        bool visible = targetAlpha > 0f;
        uiGroup.interactable = visible && !isLeavingMenu;
        uiGroup.blocksRaycasts = visible && !isLeavingMenu;
    }

    private bool IsDummyOffscreenToRight()
    {
        Vector3 viewportPosition = menuCamera.WorldToViewportPoint(dummyPlayer.transform.position);
        return viewportPosition.x > 1.05f;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (startButton != null)
            startButton.interactable = interactable;
        if (exitButton != null)
            exitButton.interactable = interactable;
    }

    private T FindInChildren<T>(string objectName) where T : Component
    {
        foreach (T component in GetComponentsInChildren<T>(true))
        {
            if (component.gameObject.name == objectName)
                return component;
        }

        return null;
    }
}
