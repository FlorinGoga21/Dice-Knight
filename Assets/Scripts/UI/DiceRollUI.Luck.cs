using System.Collections;
using UnityEngine;

public partial class DiceRollUI
{
    /// <summary>Animates only the selected small HUD die; combat owns the large result reveal.</summary>
    public IEnumerator AnimateLuckyDie(int index, int result)
    {
        if (diceImages == null || index < 0 || index >= diceImages.Length || diceImages[index] == null)
            yield break;
        var visuals = GetDiceVisuals(index);
        if (visuals == null || visuals.pipFaces == null || result < 1 || result > visuals.pipFaces.Length)
            yield break;
        var die = diceImages[index];
        isRolling = true;
        float interval = Mathf.Max(0.01f, frameInterval);
        for (float elapsed = 0f; elapsed < 0.45f; elapsed += interval)
        {
            if (visuals.rollFrames != null && visuals.rollFrames.Length > 0)
            {
                int frame = Mathf.FloorToInt(elapsed / interval) % visuals.rollFrames.Length;
                die.sprite = visuals.rollFrames[frame];
            }
            yield return new WaitForSeconds(interval);
        }
        die.sprite = visuals.pipFaces[result - 1];
        isRolling = false;
        GameAudio.Play(GameAudio.Cue.DiceSettled);
    }

    private void OnDisable()
    {
        isRolling = false;
    }
}
