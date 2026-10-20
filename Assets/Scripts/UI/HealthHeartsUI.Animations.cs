using System.Collections;
using UnityEngine;

public partial class HealthHeartsUI
{
    private IEnumerator HealingRoutine(int targetHealth, int maxHealth)
    {
        while (displayedHealth < targetHealth)
        {
            int nextHealth = Mathf.Min(targetHealth, displayedHealth + HealthPerHalfHeart);
            RenderHearts(nextHealth, maxHealth, animateEmptyHearts: false);

            int heartIndex = Mathf.Clamp((nextHealth - 1) / HealthPerFullHeart, 0, heartSlotRects.Count - 1);
            RectTransform heartSlot = heartSlotRects[heartIndex];
            float duration = Mathf.Max(0.01f, healingStepDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float pulse = progress < 0.4f
                    ? Mathf.SmoothStep(0f, 1f, progress / 0.4f)
                    : Mathf.SmoothStep(1f, 0f, (progress - 0.4f) / 0.6f);
                heartSlot.localScale = Vector3.one * Mathf.Lerp(1f, healingPulseScale, pulse);
                yield return null;
            }

            heartSlot.localScale = Vector3.one;
        }

        healingCoroutine = null;
    }

    private void StopHealingAnimation()
    {
        if (healingCoroutine != null)
        {
            StopCoroutine(healingCoroutine);
            healingCoroutine = null;
        }

        for (int i = 0; i < heartSlotRects.Count; i++)
        {
            if (heartSlotRects[i] != null)
            {
                heartSlotRects[i].localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// Instantly sets the visibility of the overhead heart bar.
    /// </summary>
    public void SetVisible(bool visible)
    {
        EnsureStructure();

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Smoothly fades the overhead heart bar in or out.
    /// </summary>
    public void Fade(bool fadeIn, float duration = -1f)
    {
        EnsureStructure();

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        float fadeDuration = duration > 0f ? duration : defaultFadeDuration;
        fadeCoroutine = StartCoroutine(FadeRoutine(fadeIn ? 1f : 0f, fadeDuration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        gameObject.SetActive(true);
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            }
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = targetAlpha;
        }

        fadeCoroutine = null;
    }

    private void StartHeartDissolve(int index)
    {
        if (index < 0 || index >= dissolveCoroutines.Count)
        {
            return;
        }

        if (dissolveCoroutines[index] != null)
        {
            StopCoroutine(dissolveCoroutines[index]);
        }

        dissolveCoroutines[index] = StartCoroutine(DissolveHeartRoutine(index));
    }

    private IEnumerator DissolveHeartRoutine(int index)
    {
        float elapsed = 0f;

        while (elapsed < heartDissolveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / heartDissolveDuration);
            SetHeartDissolveProgress(index, progress);
            yield return null;
        }

        SetHeartDissolveProgress(index, 1f);

        if (index < heartIcons.Count && heartIcons[index] != null)
        {
            heartIcons[index].sprite = emptyHeartSprite;
            heartIcons[index].enabled = false;
            SyncOutline(index);
        }

        dissolveCoroutines[index] = null;
    }

    private void SetHeartDissolveProgress(int index, float progress)
    {
        if (index < 0 || index >= dissolveMaterials.Count)
        {
            return;
        }

        Material mat = dissolveMaterials[index];
        if (mat != null)
        {
            mat.SetFloat("_Progress", progress);
        }
    }
}
