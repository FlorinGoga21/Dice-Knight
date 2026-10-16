using System.Collections;
using HeyHeyThere.SceneTransitionsFree;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private CanvasGroup uiGroup;
    [SerializeField] private RectTransform logo;
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;
    [SerializeField, Min(0f)] private float fadeDuration = 1.3f;
    [SerializeField, Min(0f)] private float logoBobHeight = 8f;
    [SerializeField, Min(0.01f)] private float logoBobSpeed = 0.55f;

    [Header("Start transition")]
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private string transitionStyle = "clouds";
    [SerializeField, Min(0.01f)] private float cameraOffsetDropSpeed = 3f;
    [SerializeField, Min(0f)] private float cameraFollowOffsetAtMenu = 2.5f;

    private DummyPlayerWalk dummyPlayer;
    private Camera menuCamera;
    private Vector3 logoStartPosition;
    private bool isLeavingMenu;

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
    }

    private void Start()
    {
        if (dummyPlayer != null)
            dummyPlayer.SetCameraOffset(cameraFollowOffsetAtMenu);

        StartCoroutine(FadeUI(1f));
    }

    private void Update()
    {
        if (logo == null || isLeavingMenu)
            return;

        float bob = Mathf.Sin(Time.unscaledTime * logoBobSpeed * Mathf.PI * 2f) * logoBobHeight;
        logo.anchoredPosition3D = logoStartPosition + Vector3.up * bob;
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartPressed);
        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitPressed);
    }

    private void OnStartPressed()
    {
        if (isLeavingMenu)
            return;

        isLeavingMenu = true;
        SetButtonsInteractable(false);
        StartCoroutine(LeaveMenuForMainScene());
    }

    private void OnExitPressed()
    {
        if (isLeavingMenu)
            return;

        isLeavingMenu = true;
        SetButtonsInteractable(false);
        StartCoroutine(FadeOutAndExit());
    }

    private IEnumerator LeaveMenuForMainScene()
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

        if (!Application.CanStreamedLevelBeLoaded(mainSceneName))
        {
            Debug.LogError($"MainMenu: Scene '{mainSceneName}' is not in Build Settings.", this);
            isLeavingMenu = false;
            yield break;
        }

        // ChangeScene covers with Cloud, loads the scene while covered, then reveals backwards.
        yield return SceneTransition.ChangeScene(
            mainSceneName,
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
