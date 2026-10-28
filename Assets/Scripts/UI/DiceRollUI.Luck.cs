using System.Collections;
using UnityEngine;

public partial class DiceRollUI
{
    /// <summary>Animates only the selected small HUD die; combat owns the large result reveal.</summary>
    public IEnumerator AnimateLuckyDie(int index, int result)
    {
        if (diceImages == null || index < 0 || index >= diceImages.Length || diceImages[index] == null)
            yield break;
        Sprite face = GetPipSprite(index, result);
        if (face == null)
            yield break;
        var die = diceImages[index];
        isRolling = true;
        float interval = Mathf.Max(0.01f, frameInterval);
        for (float elapsed = 0f; elapsed < 0.45f; elapsed += interval)
        {
            int frameCount = GetRollFrameCount(index);
            if (frameCount > 0)
            {
                int frame = Mathf.FloorToInt(elapsed / interval) % frameCount;
                die.sprite = GetRollFrame(index, frame);
            }
            yield return new WaitForSeconds(interval);
        }
        die.sprite = face;
        isRolling = false;
        GameAudio.Play(GameAudio.Cue.DiceSettled);
    }

    private void OnDisable()
    {
        isRolling = false;
    }
}
