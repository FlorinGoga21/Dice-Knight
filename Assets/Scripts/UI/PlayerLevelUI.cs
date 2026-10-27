using System.Collections;
using HeyHeyThere.SceneTransitionsFree;
using TMPro;
using UnityEngine;

/// <summary>Overhead XP presentation and icon-only upgrade choices after each fight.</summary>
public sealed class PlayerLevelUI : MonoBehaviour
{
    [Header("---References---")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private CanvasGroup progressGroup;
    [SerializeField] private CanvasGroup upgradeGroup;
    [SerializeField] private UnityEngine.UI.Image experienceFill;
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private GameObject upgradeRoot;
    [SerializeField] private UnityEngine.UI.Button healthButton;
    [SerializeField] private UnityEngine.UI.Button luckButton;
    [SerializeField] private UnityEngine.UI.Button damageButton;
    [SerializeField] private TMP_Text upgradeLabel;
    [SerializeField] private Sprite luckIcon;

    [Header("---Presentation---")]
    [Tooltip("World-space distance above the player's overhead hearts.")]
    [SerializeField, Min(0f)] private float heightAboveHearts = 0.42f;
    [SerializeField, Min(0.01f)] private float progressDuration = 0.65f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;
    [SerializeField, Min(0f)] private float holdDuration = 1.4f;

    private PlayerProgression owner;
    private PlayerStats stats;
    private CombatController combat;
    private UnityEngine.UI.Selectable[] choices;
    private bool choicesReady;
    public bool IsPresenting { get; private set; }
    public Sprite LuckIcon => luckIcon;
    public TMP_FontAsset LabelFont => levelLabel.font;

    private void Awake()
    {
        group.alpha = 0f;
        group.interactable = group.blocksRaycasts = false;
        upgradeRoot.SetActive(false);
        healthButton.onClick.AddListener(ChooseHealth);
        luckButton.onClick.AddListener(ChooseLuck);
        damageButton.onClick.AddListener(ChooseDamage);
        choices = new UnityEngine.UI.Selectable[] { healthButton, luckButton, damageButton };
    }

    public void Bind(PlayerProgression progression, PlayerStats playerStats)
    {
        owner = progression;
        stats = playerStats;
        combat = playerStats.GetComponent<CombatController>();
        GetComponent<Canvas>().worldCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (owner == null) return;
        transform.position = owner.transform.position + owner.HealthOffset + Vector3.up * heightAboveHearts;
        transform.rotation = Quaternion.identity;
        if (stats.IsDead && IsPresenting)
        {
            StopAllCoroutines();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            IsPresenting = choicesReady = false;
            upgradeRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (choicesReady && Time.timeScale > 0f && !SceneTransition.IsPlaying
            && (SaveManager.Instance == null || !SaveManager.Instance.IsLoading))
            UISelection.Ensure(upgradeRoot.transform, choices);
    }

    public void ShowProgress(int fromLevel, int fromExperience)
    {
        if (IsPresenting) return;
        IsPresenting = true;
        StartCoroutine(Present(fromLevel, fromExperience));
    }

    private IEnumerator Present(int fromLevel, int fromExperience)
    {
        progressGroup.gameObject.SetActive(true);
        progressGroup.alpha = 1f;
        upgradeGroup.alpha = 0f;
        stats.SetCombatHeartsVisible(true, fadeDuration);
        Display(fromLevel, fromExperience);
        yield return Fade(1f);
        while (fromLevel < owner.State.Level)
        {
            yield return AnimateExperience(fromLevel, fromExperience, fromLevel * 100);
            fromLevel++;
            fromExperience = 0;
            Display(fromLevel, 0);
        }
        yield return AnimateExperience(fromLevel, fromExperience, owner.State.Experience);
        if (owner.State.PendingUpgrades > 0)
        {
            yield return new WaitForSeconds(0.4f);
            yield return FadeGroup(progressGroup, 0f);
            progressGroup.gameObject.SetActive(false);
            RefreshChoices();
            yield return FadeGroup(upgradeGroup, 1f);
            choicesReady = true;
            group.interactable = group.blocksRaycasts = true;
            healthButton.Select();
            while (owner.State.PendingUpgrades > 0) yield return null;
            choicesReady = false;
            group.interactable = group.blocksRaycasts = false;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
        yield return new WaitForSeconds(upgradeRoot.activeSelf ? 0.4f : holdDuration);
        yield return Fade(0f);
        upgradeRoot.SetActive(false);
        stats.SetCombatHeartsVisible(combat != null && combat.IsInCombat, fadeDuration);
        IsPresenting = false;
    }

    private IEnumerator AnimateExperience(int level, int from, int to)
    {
        if (from == to) { Display(level, to); yield break; }
        for (float elapsed = 0f; elapsed < progressDuration; elapsed += Time.deltaTime)
        {
            Display(level, Mathf.RoundToInt(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / progressDuration))));
            yield return null;
        }
        Display(level, to);
    }

    private void Display(int level, int experience)
    {
        int needed = level * 100;
        levelLabel.text = $"Lv.{level}   {experience}/{needed} XP";
        experienceFill.fillAmount = Mathf.Clamp01(experience / (float)needed);
    }

    private void RefreshChoices()
    {
        upgradeRoot.SetActive(true);
        damageButton.gameObject.SetActive(owner.State.CanUpgradeDamage);
        luckButton.gameObject.SetActive(owner.State.CanUpgradeLuck);
        luckButton.interactable = owner.State.CanUpgradeLuck;
        upgradeLabel.text = $"LEVEL {owner.State.UpgradeLevel}";
        UnityEngine.UI.Button last = damageButton.gameObject.activeSelf ? damageButton : luckButton.interactable ? luckButton : healthButton;
        UnityEngine.UI.Button middle = luckButton.interactable ? luckButton : last;
        SetNavigation(healthButton, last, middle);
        SetNavigation(luckButton, healthButton, damageButton.gameObject.activeSelf ? damageButton : healthButton);
        SetNavigation(damageButton, luckButton.interactable ? luckButton : healthButton, healthButton);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)upgradeRoot.transform);
    }

    private static void SetNavigation(UnityEngine.UI.Button button, UnityEngine.UI.Button left, UnityEngine.UI.Button right)
    {
        button.navigation = new UnityEngine.UI.Navigation
        {
            mode = UnityEngine.UI.Navigation.Mode.Explicit,
            selectOnLeft = left, selectOnRight = right, selectOnUp = left, selectOnDown = right
        };
    }

    private void ChooseHealth() => Choose(PlayerUpgrade.Health);
    private void ChooseLuck() => Choose(PlayerUpgrade.Luck);
    private void ChooseDamage() => Choose(PlayerUpgrade.Damage);

    private void Choose(PlayerUpgrade upgrade)
    {
        if (!choicesReady || !owner.TryChooseUpgrade(upgrade)) return;
        if (owner.State.PendingUpgrades > 0) RefreshChoices();
        else
        {
            choicesReady = false;
            group.interactable = group.blocksRaycasts = false;
        }
    }

    private IEnumerator Fade(float target) => FadeGroup(group, target);

    private IEnumerator FadeGroup(CanvasGroup fadingGroup, float target)
    {
        float from = fadingGroup.alpha;
        for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.deltaTime)
        {
            fadingGroup.alpha = Mathf.Lerp(from, target, elapsed / fadeDuration);
            yield return null;
        }
        fadingGroup.alpha = target;
    }

    private void OnDestroy()
    {
        healthButton.onClick.RemoveListener(ChooseHealth);
        luckButton.onClick.RemoveListener(ChooseLuck);
        damageButton.onClick.RemoveListener(ChooseDamage);
    }
}
