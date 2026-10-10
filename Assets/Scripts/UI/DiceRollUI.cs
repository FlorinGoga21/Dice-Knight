using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class DiceRollUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image dieImage;
    [SerializeField] private Sprite[] rollFrames;
    [SerializeField] private bool hideWhenFinished = true;

    [Header("Animation")]
    [Min(0.01f)]
    [SerializeField] private float frameDuration = 0.08f;
    [Min(1)]
    [SerializeField] private int cycles = 2;

    private Coroutine rollCoroutine;

    private void Awake()
    {
        if (dieImage == null)
        {
            Debug.LogWarning(
                $"{nameof(DiceRollUI)} requires an Image slot.",
                this);
            return;
        }

        dieImage.gameObject.SetActive(!hideWhenFinished);
    }

    public void PlayRoll()
    {
        if (dieImage == null || rollFrames == null || rollFrames.Length == 0)
        {
            Debug.LogWarning(
                $"{nameof(DiceRollUI)} cannot play because its Image or roll frames are not assigned.",
                this);
            return;
        }

        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
        }

        rollCoroutine = StartCoroutine(PlayRollAnimation());
    }

    public void StopRoll()
    {
        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
            rollCoroutine = null;
        }

        if (dieImage != null && hideWhenFinished)
        {
            dieImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator PlayRollAnimation()
    {
        dieImage.gameObject.SetActive(true);

        int totalFrames = rollFrames.Length * Mathf.Max(1, cycles);
        for (int i = 0; i < totalFrames; i++)
        {
            dieImage.sprite = rollFrames[i % rollFrames.Length];
            yield return new WaitForSeconds(frameDuration);
        }

        rollCoroutine = null;

        if (hideWhenFinished)
        {
            dieImage.gameObject.SetActive(false);
        }
    }
}
