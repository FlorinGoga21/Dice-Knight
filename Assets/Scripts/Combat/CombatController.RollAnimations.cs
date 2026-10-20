using System.Collections;
using TMPro;
using UnityEngine;

public sealed partial class CombatController
{
    private IEnumerator AnimateDisplaysIn(
        SpriteRenderer[] playerDisplays,
        SpriteRenderer[] enemyDisplays,
        TMP_Text[] resultDisplays)
    {
        Vector3[] playerEnds = PrepareEntrance(playerDisplays);
        Vector3[] enemyEnds = PrepareEntrance(enemyDisplays);
        Vector3[] resultEnds = PrepareTextEntrance(resultDisplays);
        float duration = Mathf.Max(0.05f, rollDisplayAppearDuration);
        float elapsed = 0f;
        SetResultBackgroundAlpha(0f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            AnimateEntrance(playerDisplays, playerEnds, progress);
            AnimateEntrance(enemyDisplays, enemyEnds, progress);
            AnimateTextEntrance(resultDisplays, resultEnds, progress);
            SetResultBackgroundAlpha(RollResultBackgroundAlpha * progress);
            yield return null;
        }
        AnimateEntrance(playerDisplays, playerEnds, 1f);
        AnimateEntrance(enemyDisplays, enemyEnds, 1f);
        AnimateTextEntrance(resultDisplays, resultEnds, 1f);
        SetResultBackgroundAlpha(RollResultBackgroundAlpha);
    }

    private Vector3[] PrepareTextEntrance(TMP_Text[] displays)
    {
        if (displays == null) return null;
        Vector3[] ends = new Vector3[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (i == RollComparisonDisplayIndex || displays[i] == null) continue;
            ends[i] = displays[i].transform.position;
            displays[i].transform.position = ends[i] - Vector3.up * rollDisplayRise;
            SetTextAlpha(displays[i], 0f);
        }
        return ends;
    }

    private void AnimateTextEntrance(TMP_Text[] displays, Vector3[] ends, float progress)
    {
        if (displays == null || ends == null) return;
        for (int i = 0; i < displays.Length; i++)
        {
            if (i == RollComparisonDisplayIndex || displays[i] == null) continue;
            displays[i].transform.position = ends[i] - Vector3.up * (rollDisplayRise * (1f - progress));
            SetTextAlpha(displays[i], progress);
        }
    }

    private IEnumerator PopComparisonDisplay(TMP_Text comparisonDisplay)
    {
        if (comparisonDisplay == null) yield break;

        Vector3 finalScale = Vector3.one * rollResultTextScale;
        Vector3 peakScale = finalScale * 1.35f;
        const float popDuration = 0.32f;
        float elapsed = 0f;
        SetTextAlpha(comparisonDisplay, 0f);

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / popDuration);
            float scaleProgress;
            if (progress < 0.65f)
            {
                scaleProgress = Mathf.SmoothStep(0f, 1f, progress / 0.65f);
                comparisonDisplay.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, peakScale, scaleProgress);
            }
            else
            {
                scaleProgress = Mathf.SmoothStep(0f, 1f, (progress - 0.65f) / 0.35f);
                comparisonDisplay.transform.localScale = Vector3.LerpUnclamped(peakScale, finalScale, scaleProgress);
            }

            SetTextAlpha(comparisonDisplay, Mathf.Clamp01(progress * 3f));
            yield return null;
        }

        comparisonDisplay.transform.localScale = finalScale;
        SetTextAlpha(comparisonDisplay, 1f);
    }

    private Vector3[] PrepareEntrance(SpriteRenderer[] displays)
    {
        if (displays == null) return null;
        Vector3[] ends = new Vector3[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] == null) continue;
            ends[i] = displays[i].transform.position;
            displays[i].transform.position = ends[i] - Vector3.up * rollDisplayRise;
        }
        return ends;
    }

    private void AnimateEntrance(SpriteRenderer[] displays, Vector3[] ends, float progress)
    {
        if (displays == null || ends == null) return;
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] == null) continue;
            displays[i].transform.position = ends[i] - Vector3.up * (rollDisplayRise * (1f - progress));
            Color color = displays[i].color;
            color.a = progress;
            displays[i].color = color;
        }
    }

    private IEnumerator EnlargeDisplays(SpriteRenderer[] displays, TMP_Text winningTotal)
    {
        if (displays == null) yield break;
        float duration = Mathf.Min(0.2f, Mathf.Max(0.05f, rollDisplayFadeDuration));
        Vector3 baseScale = Vector3.one * rollDisplayScale;
        Vector3 winningScale = baseScale * Mathf.Max(1f, winningRollScale);
        Vector3 totalBaseScale = Vector3.one * rollResultTextScale;
        Vector3 totalWinningScale = totalBaseScale * Mathf.Max(1f, winningRollScale);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            foreach (SpriteRenderer renderer in displays)
            {
                if (renderer != null) renderer.transform.localScale = Vector3.Lerp(baseScale, winningScale, progress);
            }
            if (winningTotal != null)
            {
                winningTotal.transform.localScale = Vector3.Lerp(totalBaseScale, totalWinningScale, progress);
            }
            yield return null;
        }
        foreach (SpriteRenderer renderer in displays)
        {
            if (renderer != null) renderer.transform.localScale = winningScale;
        }
        if (winningTotal != null)
        {
            winningTotal.transform.localScale = totalWinningScale;
        }
    }

    private IEnumerator AnimateDisplaysOut(
        SpriteRenderer[] playerDisplays,
        SpriteRenderer[] enemyDisplays,
        TMP_Text[] resultDisplays)
    {
        Vector3[] playerStarts = GetPositions(playerDisplays);
        Vector3[] enemyStarts = GetPositions(enemyDisplays);
        Vector3[] resultStarts = GetTextPositions(resultDisplays);
        Color[] playerColors = GetColors(playerDisplays);
        Color[] enemyColors = GetColors(enemyDisplays);
        Color[] resultColors = GetTextColors(resultDisplays);
        float duration = Mathf.Max(0.05f, rollDisplayFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            AnimateOut(playerDisplays, playerStarts, playerColors, progress);
            AnimateOut(enemyDisplays, enemyStarts, enemyColors, progress);
            AnimateTextOut(resultDisplays, resultStarts, resultColors, progress);
            SetResultBackgroundAlpha(RollResultBackgroundAlpha * (1f - progress));
            yield return null;
        }
        HideDisplays(playerDisplays);
        HideDisplays(enemyDisplays);
        HideTextDisplays(resultDisplays);
        SetResultBackgroundAlpha(0f);
    }

    private Vector3[] GetTextPositions(TMP_Text[] displays)
    {
        if (displays == null) return null;
        Vector3[] positions = new Vector3[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] != null) positions[i] = displays[i].transform.position;
        }
        return positions;
    }

    private Color[] GetTextColors(TMP_Text[] displays)
    {
        if (displays == null) return null;
        Color[] colors = new Color[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] != null) colors[i] = displays[i].color;
        }
        return colors;
    }

    private void AnimateTextOut(TMP_Text[] displays, Vector3[] starts, Color[] colors, float progress)
    {
        if (displays == null || starts == null || colors == null) return;
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] == null) continue;
            displays[i].transform.position = starts[i] + Vector3.up * (rollDisplayRise * progress);
            Color color = colors[i];
            color.a *= 1f - progress;
            displays[i].color = color;
            SetTextAlpha(displays[i], color.a);
        }
    }

    private static void HideTextDisplays(TMP_Text[] displays)
    {
        if (displays == null) return;
        foreach (TMP_Text text in displays)
        {
            if (text != null) text.enabled = false;
        }
    }

    private void SetResultBackgroundAlpha(float alpha)
    {
        if (rollResultsBackground == null) return;
        Color color = rollResultsBackground.color;
        color.a = alpha;
        rollResultsBackground.color = color;
    }

    private static void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null) return;
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private Vector3[] GetPositions(SpriteRenderer[] displays)
    {
        if (displays == null) return null;
        Vector3[] positions = new Vector3[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] != null) positions[i] = displays[i].transform.position;
        }
        return positions;
    }

    private Color[] GetColors(SpriteRenderer[] displays)
    {
        if (displays == null) return null;
        Color[] colors = new Color[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] != null) colors[i] = displays[i].color;
        }
        return colors;
    }

    private void AnimateOut(SpriteRenderer[] displays, Vector3[] starts, Color[] colors, float progress)
    {
        if (displays == null || starts == null || colors == null) return;
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] == null) continue;
            displays[i].transform.position = starts[i] + Vector3.up * (rollDisplayRise * progress);
            Color color = colors[i];
            color.a *= 1f - progress;
            displays[i].color = color;
        }
    }

    private static void HideDisplays(SpriteRenderer[] displays)
    {
        if (displays == null) return;
        foreach (SpriteRenderer renderer in displays)
        {
            if (renderer != null) renderer.enabled = false;
        }
    }
}
