using System.Collections;
using TMPro;
using UnityEngine;

public sealed partial class CombatController
{
    private GameObject luckyFeedback;

    private IEnumerator ShowLuckyReroll(int[] results, int index, int reroll, int enemyTotal)
    {
        if (playerRollDisplays == null || index < 0 || index >= playerRollDisplays.Length
            || playerRollDisplays[index] == null) yield break;
        SpriteRenderer selected = playerRollDisplays[index];
        yield return new WaitForSeconds(0.25f);
        ShowLuckyFeedback(selected);
        yield return new WaitForSeconds(0.35f);
        yield return diceRollUI.AnimateLuckyDie(index, reroll);
        if (selected == null || playerStats == null || playerStats.IsDead) yield break;
        if (DiceRules.TryApplyLucky(results, index, reroll))
        {
            // Replace and pop this result only. The other dice retain their objects, faces and poses.
            selected.sprite = diceRollUI.GetPipSprite(index, results[index]);
            RefreshLuckyTotal(SumArray(results), enemyTotal);
            yield return PopLuckyResult(selected);
        }
        yield return new WaitForSeconds(0.35f);
        DestroyLuckyFeedback();
    }

    private void ShowLuckyFeedback(SpriteRenderer selected)
    {
        DestroyLuckyFeedback();
        luckyFeedback = new GameObject("Lucky Feedback");
        luckyFeedback.transform.position = new Vector3(selected.bounds.center.x,
            selected.bounds.max.y + 0.22f, selected.transform.position.z);
        var labelObject = new GameObject("Lucky", typeof(TextMeshPro));
        labelObject.transform.SetParent(luckyFeedback.transform, false);
        labelObject.transform.localScale = Vector3.one * rollResultTextScale * 0.55f;
        var label = labelObject.GetComponent<TextMeshPro>();
        label.font = playerProgression.LevelFont != null ? playerProgression.LevelFont : rollResultFont;
        label.text = "LUCKY!";
        label.fontSize = 4f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.color = new Color(0.42f, 1f, 0.48f);
        var mesh = label.GetComponent<MeshRenderer>();
        mesh.sortingLayerID = selected.sortingLayerID;
        mesh.sortingOrder = selected.sortingOrder + 2;
        label.ForceMeshUpdate();
        if (playerProgression.LuckIcon != null)
        {
            var cloverObject = new GameObject("Clover", typeof(SpriteRenderer));
            cloverObject.transform.SetParent(luckyFeedback.transform, false);
            cloverObject.transform.localPosition = Vector3.left
                * (label.textBounds.extents.x * labelObject.transform.localScale.x + 0.12f);
            cloverObject.transform.localScale = Vector3.one * 0.32f;
            var clover = cloverObject.GetComponent<SpriteRenderer>();
            clover.sprite = playerProgression.LuckIcon;
            clover.sharedMaterial = selected.sharedMaterial;
            clover.sortingLayerID = selected.sortingLayerID;
            clover.sortingOrder = selected.sortingOrder + 2;
        }
    }

    private IEnumerator PopLuckyResult(SpriteRenderer selected)
    {
        Vector3 scale = selected.transform.localScale;
        const float duration = 0.28f;
        selected.transform.localScale = Vector3.zero;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            if (selected == null) yield break;
            float t = Mathf.Clamp01(elapsed / duration);
            float factor = t < 0.65f
                ? Mathf.SmoothStep(0f, 1.18f, t / 0.65f)
                : Mathf.SmoothStep(1.18f, 1f, (t - 0.65f) / 0.35f);
            selected.transform.localScale = scale * factor;
            yield return null;
        }
        if (selected != null) selected.transform.localScale = scale;
    }

    private void RefreshLuckyTotal(int playerTotal, int enemyTotal)
    {
        if (rollResultDisplays == null) return;
        if (rollResultDisplays[0] != null)
        {
            rollResultDisplays[0].text = playerTotal.ToString();
            rollResultDisplays[0].ForceMeshUpdate();
        }
        if (rollResultDisplays.Length > RollComparisonDisplayIndex && rollResultDisplays[RollComparisonDisplayIndex] != null)
        {
            rollResultDisplays[RollComparisonDisplayIndex].text = playerTotal > enemyTotal ? ">" : playerTotal < enemyTotal ? "<" : "=";
            rollResultDisplays[RollComparisonDisplayIndex].ForceMeshUpdate();
        }
        // The new total may have an extra digit; expand its backing without moving other results.
        if (rollResultsBackground == null) return;
        Bounds bounds = default;
        bool found = false;
        foreach (var label in rollResultDisplays)
        {
            if (label == null) continue;
            var mesh = label.GetComponent<MeshRenderer>();
            if (!found) { bounds = mesh.bounds; found = true; }
            else bounds.Encapsulate(mesh.bounds);
        }
        if (!found) return;
        rollResultsBackground.transform.position = bounds.center;
        rollResultsBackground.transform.localScale = new Vector3(bounds.size.x + RollResultBackgroundPadding * 2f,
            bounds.size.y + RollResultBackgroundPadding * 2f, 1f);
    }

    private void DestroyLuckyFeedback()
    {
        if (luckyFeedback != null) Destroy(luckyFeedback);
        luckyFeedback = null;
    }
}
