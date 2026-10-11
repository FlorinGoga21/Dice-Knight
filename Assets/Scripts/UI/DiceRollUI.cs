
using System.Collections;
using ActionInt = System.Action<int>;
using UnityEngine;
using UnityEngine.UI;

public class DiceRollUI : MonoBehaviour
{
    [System.Serializable]
    private class DiceVisualSet
    {
        // Assign pip faces in order: one pip through six pips.
        public Sprite[] pipFaces;

        // Assign the rolling-die frames for this die.
        public Sprite[] rollFrames;
    }

    [Header("Dice UI")]
    [SerializeField] private Image[] diceImages;
    [SerializeField] private Button rollButton;
    [SerializeField] private GameObject[] selectedIndicators;
    [SerializeField, Range(1, 3)] private int activeDiceCount = 1;
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

    public event ActionInt DiceRolled;
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
        SelectDie(0);
    }

    public void RollDice()
    {
        if (!isRolling)
            StartCoroutine(RollAnimation());
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

            selectedIndicators[i].SetActive(i == selectedDieIndex && i < activeDiceCount);
        }
    }

    private IEnumerator RollAnimation()
    {
        isRolling = true;
        Image selectedDiceImage = diceImages[selectedDieIndex];
        DiceVisualSet selectedDiceVisuals = GetDiceVisuals(selectedDieIndex);
        float randomZRotation = Random.Range(randomRotationMin, randomRotationMax);
        selectedDiceImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, randomZRotation);

        float elapsed = 0f;
        int frameIndex = 0;

        // Play the rolling-die frames repeatedly while the roll is in progress.
        while (elapsed < rollDuration
            && selectedDiceVisuals != null
            && selectedDiceVisuals.rollFrames != null
            && selectedDiceVisuals.rollFrames.Length > 0)
        {
            selectedDiceImage.sprite =
                selectedDiceVisuals.rollFrames[frameIndex % selectedDiceVisuals.rollFrames.Length];
            frameIndex++;

            yield return new WaitForSeconds(frameInterval);
            elapsed += frameInterval;
        }

        //reset the rotation to identity so the final pip face is not rotated. (maybe will use in the future)
        selectedDiceImage.rectTransform.localRotation = Quaternion.identity;

        // Map the result directly to the matching pip face.
        int result = Random.Range(1, 7);
        if (selectedDiceVisuals == null
            || selectedDiceVisuals.pipFaces == null
            || selectedDiceVisuals.pipFaces.Length < result)
        {
            Debug.LogWarning(
                $"{nameof(DiceRollUI)} has no six pip faces configured for die {selectedDieIndex + 1}.",
                this);
        }
        else
        {
            selectedDiceImage.sprite = selectedDiceVisuals.pipFaces[result - 1];
        }

        yield return PlayImpactAnimation(selectedDiceImage.rectTransform);

        Debug.Log("Dice rolled: " + result);

        isRolling = false;
        DiceRolled?.Invoke(result);
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
