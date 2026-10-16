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

    private PlayerInputActions inputActions;
    private UnityEngine.UI.RawImage blurImage;
    private Texture2D pausedScreenTexture;
    private Canvas menuCanvas;
    private CanvasGroup menuCanvasGroup;
    private bool originalOverrideSorting;
    private int originalSortingOrder;
    private float originalCanvasAlpha;
    private bool pauseTransitionPending;

    private void Awake()
    {
        pauseMenu = pauseMenu != null ? pauseMenu : gameObject;
        inputActions = new PlayerInputActions();
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
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        inputActions.Player.Pause.performed += OnPausePerformed;
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Pause.performed -= OnPausePerformed;
        inputActions.Disable();
    }

    private void OnDestroy()
    {
        inputActions.Dispose();
        ReleasePausedScreenTexture();
    }

    public void ReturnToGame()
    {
        StartResume(quitAfterResume: false);
    }

    public void ExitGame()
    {
        StartResume(quitAfterResume: true);
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (pauseTransitionPending)
        {
            return;
        }

        if (pauseMenu.activeSelf)
        {
            StartResume(quitAfterResume: false);
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
                menuCanvas.sortingOrder = short.MaxValue;
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
    }

    private IEnumerator PauseAfterEndOfFrame()
    {
        pauseTransitionPending = true;
        yield return new WaitForEndOfFrame();

        CapturePausedScreen();
        menuCanvasGroup.alpha = 0f;
        SetPaused(true);
        menuCanvasGroup.interactable = false;
        menuCanvasGroup.blocksRaycasts = false;
        yield return FadeCanvasGroup(originalCanvasAlpha);
        menuCanvasGroup.interactable = true;
        menuCanvasGroup.blocksRaycasts = true;
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
        float startAlpha = menuCanvasGroup.alpha;
        if (fadeDuration <= 0f)
        {
            menuCanvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        menuCanvasGroup.alpha = targetAlpha;
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
