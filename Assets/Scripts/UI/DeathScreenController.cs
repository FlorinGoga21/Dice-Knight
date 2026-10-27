using System.Collections;
using HeyHeyThere.SceneTransitionsFree;
using TMPro;
using UnityEngine;

/// <summary>Unscaled death cinematic and the two exits from an ended run.</summary>
public sealed class DeathScreenController : MonoBehaviour
{
    [Header("---Presentation---")]
    [SerializeField] private CanvasGroup backdropGroup;
    [SerializeField] private UnityEngine.UI.Image backdrop;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private RectTransform title;
    [SerializeField] private CanvasGroup actionsGroup;
    [SerializeField] private UnityEngine.UI.Button newRunButton;
    [SerializeField] private UnityEngine.UI.Button mainMenuButton;
    [SerializeField] private TMP_Text feedback;

    [Header("---Timing---")]
    [Tooltip("Real seconds allowed for the player death animation before the world freezes.")]
    [SerializeField, Min(0f)] private float deathAnimationDelay = 1.8f;
    [SerializeField, Min(0.01f)] private float titleFadeDuration = 1.5f;
    [SerializeField, Min(0f)] private float titleHoldDuration = 0.65f;
    [SerializeField, Min(0.01f)] private float actionsFadeDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float exitFadeDuration = 0.4f;
    [Tooltip("Real seconds for each half of the cloud transition when leaving the death screen.")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.8f;

    private UnityEngine.UI.Selectable[] choices;
    private bool showing;
    private bool ready;
    private bool leaving;

    private void Awake()
    {
        choices = new UnityEngine.UI.Selectable[] { newRunButton, mainMenuButton };
        backdropGroup.alpha = titleGroup.alpha = actionsGroup.alpha = 0f;
        actionsGroup.interactable = false;
        actionsGroup.blocksRaycasts = false;
        feedback.gameObject.SetActive(false);
        newRunButton.onClick.AddListener(StartNewRun);
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    public void Show()
    {
        if (showing) return;
        showing = true;
        foreach (var pause in FindObjectsByType<PauseMenuController>(FindObjectsInactive.Include))
            pause.PrepareForSceneLoad();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        if (SaveManager.Instance != null && !SaveManager.Instance.TryEndRun(out string error))
            ShowError(error);
        GameAudio.Instance?.EndRun();
        StartCoroutine(Reveal());
    }

    private IEnumerator Reveal()
    {
        yield return new WaitForSecondsRealtime(deathAnimationDelay);
        Time.timeScale = 0f;
        GameAudio.Instance?.PlayDeathMusic();
        for (float elapsed = 0f; elapsed < titleFadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / titleFadeDuration);
            backdropGroup.alpha = titleGroup.alpha = t;
            title.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, t);
            yield return null;
        }
        backdropGroup.alpha = titleGroup.alpha = 1f;
        title.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(titleHoldDuration);
        for (float elapsed = 0f; elapsed < actionsFadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            actionsGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / actionsFadeDuration);
            yield return null;
        }
        actionsGroup.alpha = 1f;
        actionsGroup.interactable = actionsGroup.blocksRaycasts = true;
        ready = true;
        newRunButton.Select();
    }

    private void Update()
    {
        if (ready && !leaving) UISelection.Ensure(actionsGroup.transform, choices);
    }

    public void StartNewRun()
    {
        if (!ready || leaving || SceneTransition.IsPlaying) return;
        var saves = SaveManager.Instance;
        if (saves == null) { ShowError("The save system is unavailable."); return; }
        if (!saves.TryStartNewRun(out string error)) { ShowError(error); return; }
        StartCoroutine(Leave("MapScene"));
    }

    public void ReturnToMainMenu()
    {
        if (!ready || leaving || SceneTransition.IsPlaying) return;
        if (!Application.CanStreamedLevelBeLoaded("MainMenu")) { ShowError("The main menu is unavailable."); return; }
        if (SaveManager.Instance != null && !SaveManager.Instance.TryEndRun(out string error))
        { ShowError(error); return; }
        StartCoroutine(Leave("MainMenu"));
    }

    private void ShowError(string message)
    {
        feedback.text = message;
        feedback.gameObject.SetActive(true);
    }

    private IEnumerator Leave(string scene)
    {
        leaving = true;
        actionsGroup.interactable = false;
        actionsGroup.blocksRaycasts = false;
        for (float elapsed = 0f; elapsed < exitFadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / exitFadeDuration);
            titleGroup.alpha = actionsGroup.alpha = 1f - t;
            yield return null;
        }
        titleGroup.alpha = actionsGroup.alpha = 0f;
        Time.timeScale = 1f;
        yield return SceneTransition.ChangeScene(scene, "clouds",
            new SceneTransition.Options { Duration = transitionDuration });
    }

    private void OnDestroy()
    {
        newRunButton.onClick.RemoveListener(StartNewRun);
        mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        if (showing && !leaving) Time.timeScale = 1f;
    }
}
