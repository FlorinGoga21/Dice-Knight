using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the dice rolling UI, die selection, rolling animation frames, and roll results.
/// </summary>
public class DiceRollUI : MonoBehaviour
{
    [Serializable]
    private class DiceVisualSet
    {
        [Tooltip("Assign pip faces in order: 1 through 6 pips.")]
        [SerializeField] public Sprite[] pipFaces;

        [Tooltip("Assign the rolling-die animation frames for this die.")]
        [SerializeField] public Sprite[] rollFrames;
    }

    [Header("Dice UI")]
    [SerializeField] private Image[] diceImages;
    [SerializeField] private Button rollButton;
    [SerializeField] private GameObject[] selectedIndicators;
    [SerializeField, Range(1, 3)] private int activeDiceCount = 1;
    /// <summary>
    /// Number of dice currently available for the player to roll.
    /// </summary>
    public int ActiveDiceCount => activeDiceCount;
    [SerializeField] private DiceVisualSet[] diceVisuals;

    [Header("Animation")]
    [SerializeField] private float rollDuration = 0.8f;
    [SerializeField] private float frameInterval = 0.07f;
    [SerializeField] private float randomRotationMin = 0f;
    [SerializeField] private float randomRotationMax = 360f;
    [SerializeField] private float impactScale = 1.25f;
    [SerializeField] private float impactDuration = 0.2f;

    private bool isRolling;
    private int selectedDieIndex;

    /// <summary>
    /// Fired after all active dice finish their roll animation.
    /// Passes an array of results, one per die.
    /// </summary>
    public event Action<int[]> DiceRolled;
    public bool IsRolling => isRolling;

    public Sprite GetPipSprite(int dieIndex, int result)
    {
        DiceVisualSet visuals = GetDiceVisuals(dieIndex);
        if (visuals == null || visuals.pipFaces == null || result < 1 || result > visuals.pipFaces.Length)
        {
            return null;
        }

        return visuals.pipFaces[result - 1];
    }

    private void Awake()
    {
        activeDiceCount = Mathf.Clamp(activeDiceCount, 1, GetDiceSlotCount());
        UpdateDiceAvailability();
        HighlightAllActiveDice();
    }

    /// <summary>
    /// Called by the Roll button. Rolls ALL active dice with animation.
    /// </summary>
    public void RollDice()
    {
        if (!isRolling)
        {
            StartCoroutine(RollAllDiceAnimated());
        }
    }

    public void SetRollButtonInteractable(bool interactable)
    {
        if (rollButton != null)
        {
            rollButton.interactable = interactable;
        }
    }

    public void SelectDie(int dieIndex)
    {
        if (diceImages == null
            || selectedIndicators == null
            || diceImages.Length != selectedIndicators.Length
            || dieIndex < 0
            || dieIndex >= activeDiceCount)
        {
            Debug.LogWarning($"{nameof(DiceRollUI)} has invalid die selection references.", this);
            return;
        }

        selectedDieIndex = dieIndex;

        for (int i = 0; i < selectedIndicators.Length; i++)
        {
            selectedIndicators[i].SetActive(i == selectedDieIndex);
        }
    }

    /// <summary>
    /// Highlights all active dice selection indicators.
    /// </summary>
    private void HighlightAllActiveDice()
    {
        if (selectedIndicators == null)
        {
            return;
        }

        for (int i = 0; i < selectedIndicators.Length; i++)
        {
            selectedIndicators[i].SetActive(i < activeDiceCount);
        }
    }

    public void BuyNextDie()
    {
        if (activeDiceCount >= GetDiceSlotCount())
        {
            return;
        }

        activeDiceCount++;
        UpdateDiceAvailability();
    }

    private int GetDiceSlotCount()
    {
        if (diceImages == null || selectedIndicators == null)
        {
            return 0;
        }

        return Mathf.Min(diceImages.Length, selectedIndicators.Length);
    }

    private void UpdateDiceAvailability()
    {
        int slotCount = GetDiceSlotCount();

        for (int i = 0; i < slotCount; i++)
        {
            Transform slot = diceImages[i].transform.parent;
            if (slot != null)
            {
                slot.gameObject.SetActive(i < activeDiceCount);
            }

            selectedIndicators[i].SetActive(i < activeDiceCount);
        }
    }

    /// <summary>
    /// Rolls all active dice simultaneously with rolling animation,
    /// then fires DiceRolled with the array of results.
    /// </summary>
    private IEnumerator RollAllDiceAnimated()
    {
        isRolling = true;
        int count = activeDiceCount;
        int[] results = new int[count];

        // Pre-generate results and start roll animation for each die.
        for (int i = 0; i < count; i++)
        {
            results[i] = UnityEngine.Random.Range(1, 7);
        }

        // Apply random rotation to all dice at the start.
        for (int i = 0; i < count; i++)
        {
            if (diceImages != null && i < diceImages.Length)
            {
                float randomZ = UnityEngine.Random.Range(randomRotationMin, randomRotationMax);
                diceImages[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, randomZ);
            }
        }

        // Animate all dice rolling frames simultaneously.
        float elapsed = 0f;
        int frameIndex = 0;

        while (elapsed < rollDuration)
        {
            for (int i = 0; i < count; i++)
            {
                if (diceImages == null || i >= diceImages.Length)
                {
                    continue;
                }

                DiceVisualSet visuals = GetDiceVisuals(i);
                if (visuals != null && visuals.rollFrames != null && visuals.rollFrames.Length > 0)
                {
                    // Offset each die's frame index slightly for visual variety.
                    int offset = (frameIndex + i * 2) % visuals.rollFrames.Length;
                    diceImages[i].sprite = visuals.rollFrames[offset];
                }
            }

            frameIndex++;
            yield return new WaitForSeconds(frameInterval);
            elapsed += frameInterval;
        }

        // Reset rotation and show final pip face for each die.
        for (int i = 0; i < count; i++)
        {
            if (diceImages == null || i >= diceImages.Length)
            {
                continue;
            }

            diceImages[i].rectTransform.localRotation = Quaternion.identity;

            DiceVisualSet visuals = GetDiceVisuals(i);
            if (visuals == null || visuals.pipFaces == null || visuals.pipFaces.Length < results[i])
            {
                Debug.LogWarning(
                    $"{nameof(DiceRollUI)} has no six pip faces configured for die {i + 1}.",
                    this);
            }
            else
            {
                diceImages[i].sprite = visuals.pipFaces[results[i] - 1];
            }
        }

        // Play impact animation on all dice simultaneously.
        Coroutine[] impacts = new Coroutine[count];
        for (int i = 0; i < count; i++)
        {
            if (diceImages != null && i < diceImages.Length)
            {
                impacts[i] = StartCoroutine(PlayImpactAnimation(diceImages[i].rectTransform));
            }
        }

        // Wait for all impact animations to finish.
        for (int i = 0; i < count; i++)
        {
            if (impacts[i] != null)
            {
                yield return impacts[i];
            }
        }

        string rollLog = "Dice rolled:";
        for (int i = 0; i < count; i++)
        {
            rollLog += $" [{results[i]}]";
        }
        Debug.Log(rollLog);

        isRolling = false;
        DiceRolled?.Invoke(results);
    }

    private IEnumerator PlayImpactAnimation(RectTransform diceImage)
    {
        Vector3 originalScale = diceImage.localScale;
        Vector3 enlargedScale = originalScale * Mathf.Max(1f, impactScale);
        float halfDuration = Mathf.Max(0.01f, impactDuration) * 0.5f;

        yield return ScaleDiceImage(diceImage, originalScale, enlargedScale, halfDuration);
        yield return ScaleDiceImage(diceImage, enlargedScale, originalScale, halfDuration);
    }

    private IEnumerator ScaleDiceImage(
        RectTransform diceImage,
        Vector3 startScale,
        Vector3 endScale,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            progress = progress * progress * (3f - 2f * progress);
            diceImage.localScale = Vector3.LerpUnclamped(startScale, endScale, progress);
            yield return null;
        }

        diceImage.localScale = endScale;
    }

    private DiceVisualSet GetDiceVisuals(int dieIndex)
    {
        if (diceVisuals == null || dieIndex < 0 || dieIndex >= diceVisuals.Length)
        {
            return null;
        }

        return diceVisuals[dieIndex];
    }
}
