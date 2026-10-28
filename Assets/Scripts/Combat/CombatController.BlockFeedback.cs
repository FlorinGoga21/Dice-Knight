using System.Collections;
using UnityEngine;

public sealed partial class CombatController
{
    [Header("---Block Feedback---")]
    [Tooltip("Seconds for the player's dice and total to shrink and fade after choosing a reactive block.")]
    [SerializeField, Min(0.01f)] private float blockDiceFadeDuration = 0.12f;
    [Tooltip("Seconds for the replacement shield to rise, overshoot, and settle before the enemy attacks.")]
    [SerializeField, Min(0.01f)] private float blockShieldRaiseDuration = 0.2f;
    [Tooltip("World-space distance the result shield rises into position.")]
    [SerializeField, Min(0f)] private float blockShieldRise = 0.18f;

    private IEnumerator AnimateBlockAfterRoll()
    {
        var dice = playerRollDisplays;
        var scales = new Vector3[dice != null ? dice.Length : 0];
        var colors = GetColors(dice);
        for (int i = 0; i < scales.Length; i++)
            if (dice[i] != null) scales[i] = dice[i].transform.localScale;
        var total = rollResultDisplays != null && rollResultDisplays.Length > 0 ? rollResultDisplays[0] : null;
        var comparison = rollResultDisplays != null && rollResultDisplays.Length > RollComparisonDisplayIndex
            ? rollResultDisplays[RollComparisonDisplayIndex] : null;
        Vector3 totalScale = total != null ? total.transform.localScale : Vector3.one;
        Vector3 comparisonScale = comparison != null ? comparison.transform.localScale : Vector3.one;
        float totalAlpha = total != null ? total.color.a : 0f;
        float comparisonAlpha = comparison != null ? comparison.color.a : 0f;
        float duration = Mathf.Max(0.01f, blockDiceFadeDuration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < scales.Length; i++)
            {
                if (dice[i] == null) continue;
                dice[i].transform.localScale = scales[i] * Mathf.Lerp(1f, 0.65f, t);
                Color color = colors[i];
                color.a *= 1f - t;
                dice[i].color = color;
            }
            if (total != null) total.transform.localScale = totalScale * Mathf.Lerp(1f, 0.65f, t);
            if (comparison != null) comparison.transform.localScale = comparisonScale * (1f - t);
            SetTextAlpha(total, totalAlpha * (1f - t));
            SetTextAlpha(comparison, comparisonAlpha * (1f - t));
            yield return null;
        }
        HideDisplays(dice);
        if (total != null) { total.enabled = false; Destroy(total.gameObject); rollResultDisplays[0] = null; }
        if (comparison != null) { comparison.enabled = false; Destroy(comparison.gameObject); rollResultDisplays[RollComparisonDisplayIndex] = null; }
        DestroyRollDisplays(ref playerRollDisplays);
        // Keep the enemy's exact dice, total and their transforms throughout the transition.
        if (rollResultsBackground != null && rollResultDisplays != null && rollResultDisplays.Length > 1 && rollResultDisplays[1] != null)
        {
            var bounds = rollResultDisplays[1].GetComponent<MeshRenderer>().bounds;
            rollResultsBackground.transform.position = bounds.center;
            rollResultsBackground.transform.localScale = new Vector3(bounds.size.x + RollResultBackgroundPadding * 2f,
                bounds.size.y + RollResultBackgroundPadding * 2f, 1f);
        }
        playerRollDisplays = CreateBlockDisplay(transform, "Player Block");
        if (playerRollDisplays == null || playerRollDisplays.Length == 0) yield break;
        var shield = playerRollDisplays[0];
        Vector3 restPosition = shield.transform.position;
        Vector3 restScale = shield.transform.localScale;
        duration = Mathf.Max(0.01f, blockShieldRaiseDuration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float rise = Mathf.SmoothStep(0f, 1f, t);
            float scale = t < 0.65f ? Mathf.SmoothStep(0.75f, 1.1f, t / 0.65f)
                : Mathf.SmoothStep(1.1f, 1f, (t - 0.65f) / 0.35f);
            shield.transform.position = restPosition - Vector3.up * (blockShieldRise * (1f - rise));
            shield.transform.localScale = restScale * scale;
            shield.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t * 3f));
            yield return null;
        }
        shield.transform.position = restPosition;
        shield.transform.localScale = restScale;
        shield.color = Color.white;
    }

    private IEnumerator AnimateBlockImpact()
    {
        if (playerRollDisplays == null || playerRollDisplays.Length == 0 || playerRollDisplays[0] == null) yield break;
        var shield = playerRollDisplays[0];
        Vector3 restScale = shield.transform.localScale;
        Vector3 restPosition = shield.transform.position;
        float direction = activeEnemy != null && activeEnemy.transform.position.x >= transform.position.x ? -1f : 1f;
        const float duration = 0.18f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
            shield.transform.localScale = Vector3.Scale(restScale, new Vector3(1f - 0.08f * pulse, 1f + 0.08f * pulse, 1f));
            shield.transform.position = restPosition + Vector3.right * (direction * 0.06f * pulse);
            yield return null;
        }
        shield.transform.localScale = restScale;
        shield.transform.position = restPosition;
    }
}
