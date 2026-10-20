using TMPro;
using UnityEngine;

public sealed partial class CombatController
{
    private SpriteRenderer[] playerRollDisplays;
    private SpriteRenderer[] enemyRollDisplays;
    private TMP_Text[] rollResultDisplays;
    private SpriteRenderer rollResultsBackground;

    private SpriteRenderer[] CreateRollDisplays(int[] results, Transform owner, string displayName)
    {
        if (owner == null || diceRollUI == null || results == null) return null;
        SpriteRenderer[] displays = new SpriteRenderer[results.Length];
        float startX = -(results.Length - 1) * DiceDisplaySpacing * 0.5f;
        for (int i = 0; i < results.Length; i++)
        {
            GameObject item = new GameObject($"{displayName} {i + 1}");
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 100;
            renderer.sprite = diceRollUI.GetPipSprite(0, results[i]);
            renderer.transform.position = owner.position + rollDisplayOffset
                + new Vector3(startX + i * DiceDisplaySpacing, 0f, 0f);
            renderer.transform.localScale = Vector3.one * rollDisplayScale;
            renderer.color = new Color(1f, 1f, 1f, 0f);
            renderer.enabled = renderer.sprite != null;
            displays[i] = renderer;
        }
        return displays;
    }

    private SpriteRenderer[] CreateBlockDisplay(Transform owner, string displayName)
    {
        if (owner == null) return null;

        if (blockShieldSprite == null)
        {
            blockShieldSprite = Resources.Load<Sprite>(BlockShieldResourcePath);
        }

        if (blockShieldSprite == null)
        {
            Debug.LogError($"Could not load the SPUM block shield at Resources/{BlockShieldResourcePath}.png", this);
            return null;
        }

        GameObject item = new GameObject(displayName);
        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 100;
        renderer.sprite = blockShieldSprite;
        renderer.transform.position = owner.position + rollDisplayOffset;
        renderer.transform.localScale = Vector3.one * rollDisplayScale;
        renderer.color = new Color(1f, 1f, 1f, 0f);
        renderer.enabled = true;
        return new[] { renderer };
    }

    private TMP_Text[] CreateRollResultDisplays(
        int playerTotal,
        int enemyTotal,
        SpriteRenderer[] playerDisplays,
        SpriteRenderer[] enemyDisplays,
        bool blockAction)
    {
        if (playerDisplays == null || playerDisplays.Length == 0
            || enemyDisplays == null || enemyDisplays.Length == 0)
        {
            return null;
        }

        bool enemyIsRight = activeEnemy != null && activeEnemy.transform.position.x >= transform.position.x;
        float inwardDirection = enemyIsRight ? 1f : -1f;
        SpriteRenderer playerInnerEdge = playerDisplays[enemyIsRight ? playerDisplays.Length - 1 : 0];
        SpriteRenderer enemyInnerEdge = enemyDisplays[enemyIsRight ? 0 : enemyDisplays.Length - 1];
        Vector3 playerTotalPosition = playerInnerEdge.transform.position
            + Vector3.right * inwardDirection * (playerInnerEdge.bounds.extents.x + rollResultPadding);
        Vector3 enemyTotalPosition = enemyInnerEdge.transform.position
            - Vector3.right * inwardDirection * (enemyInnerEdge.bounds.extents.x + rollResultPadding);
        Vector3 comparisonPosition = (playerTotalPosition + enemyTotalPosition) * 0.5f;

        TMP_Text[] resultDisplays = new TMP_Text[3];
        if (!blockAction)
        {
            resultDisplays[0] = CreateWorldResultText("Player Roll Total", playerTotal.ToString(), playerTotalPosition, playerInnerEdge);
        }
        resultDisplays[1] = CreateWorldResultText("Enemy Roll Total", enemyTotal.ToString(), enemyTotalPosition, enemyInnerEdge);
        if (!blockAction)
        {
            string comparison = playerTotal > enemyTotal ? ">" : playerTotal < enemyTotal ? "<" : "=";
            resultDisplays[2] = CreateWorldResultText("Roll Comparison", comparison, comparisonPosition, playerInnerEdge);

            // Keep text spacing separate from the padding between the dice and results.
            Bounds comparisonBounds = resultDisplays[2].textBounds;
            float resultGap = comparisonBounds.size.y * rollResultTextScale * 0.25f;
            float comparisonHalfWidth = comparisonBounds.extents.x * rollResultTextScale * 1.35f;
            float totalScale = rollResultTextScale * Mathf.Max(1f, winningRollScale);
            float playerHalfWidth = resultDisplays[0].textBounds.extents.x * totalScale;
            float enemyHalfWidth = resultDisplays[1].textBounds.extents.x * totalScale;
            resultDisplays[0].transform.position = comparisonPosition - Vector3.right * inwardDirection
                * (comparisonHalfWidth + resultGap + playerHalfWidth);
            resultDisplays[1].transform.position = comparisonPosition + Vector3.right * inwardDirection
                * (comparisonHalfWidth + resultGap + enemyHalfWidth);

            resultDisplays[RollComparisonDisplayIndex].transform.localScale = Vector3.zero;
        }
        rollResultsBackground = CreateRollResultsBackground(resultDisplays, playerInnerEdge);
        return resultDisplays;
    }

    private TMP_Text CreateWorldResultText(string objectName, string value, Vector3 position, SpriteRenderer referenceRenderer)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.position = position;
        textObject.transform.localScale = Vector3.one * rollResultTextScale;

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.text = value;
        text.fontSize = rollResultFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = new Color(1f, 1f, 1f, 0f);
        if (rollResultFont != null)
        {
            text.font = rollResultFont;
        }

        MeshRenderer meshRenderer = textObject.GetComponent<MeshRenderer>();
        meshRenderer.sortingLayerID = referenceRenderer.sortingLayerID;
        meshRenderer.sortingOrder = referenceRenderer.sortingOrder + (int)RollResultSortingOrderOffset;
        text.ForceMeshUpdate();
        return text;
    }

    private SpriteRenderer CreateRollResultsBackground(TMP_Text[] labels, SpriteRenderer referenceRenderer)
    {
        if (labels == null || labels.Length == 0 || referenceRenderer == null)
        {
            return null;
        }

        MeshRenderer firstLabelRenderer = null;
        foreach (TMP_Text label in labels)
        {
            if (label == null) continue;
            firstLabelRenderer = label.GetComponent<MeshRenderer>();
            if (firstLabelRenderer != null) break;
        }
        if (firstLabelRenderer == null)
        {
            return null;
        }

        Bounds bounds = firstLabelRenderer.bounds;
        for (int i = 1; i < labels.Length; i++)
        {
            if (labels[i] == null) continue;
            MeshRenderer labelRenderer = labels[i].GetComponent<MeshRenderer>();
            if (labelRenderer != null) bounds.Encapsulate(labelRenderer.bounds);
        }

        GameObject backgroundObject = new GameObject("Roll Results Background");
        backgroundObject.transform.position = bounds.center;
        backgroundObject.transform.localScale = new Vector3(
            bounds.size.x + RollResultBackgroundPadding * 2f,
            bounds.size.y + RollResultBackgroundPadding * 2f,
            1f);

        SpriteRenderer background = backgroundObject.AddComponent<SpriteRenderer>();
        background.sprite = GetRollResultBackgroundSprite();
        background.color = new Color(0.025f, 0.035f, 0.065f, 0f);
        background.sortingLayerID = referenceRenderer.sortingLayerID;
        background.sortingOrder = referenceRenderer.sortingOrder - 1;
        return background;
    }

    private static Sprite GetRollResultBackgroundSprite()
    {
        if (rollResultBackgroundSprite == null)
        {
            rollResultBackgroundSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            rollResultBackgroundSprite.name = "Roll Result Background";
        }

        return rollResultBackgroundSprite;
    }

    private TMP_FontAsset ResolveRollResultFont()
    {
        if (rollResultFont != null)
        {
            return rollResultFont;
        }

        foreach (TMP_FontAsset loadedFont in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (loadedFont != null && loadedFont.name == "Pixel UI TMP")
            {
                return loadedFont;
            }
        }

        return TMP_Settings.defaultFontAsset;
    }

    private void DestroyRollResultDisplays(ref TMP_Text[] displays)
    {
        if (displays != null)
        {
            foreach (TMP_Text text in displays)
            {
                if (text != null) Destroy(text.gameObject);
            }
            displays = null;
        }

        if (rollResultsBackground != null)
        {
            Destroy(rollResultsBackground.gameObject);
            rollResultsBackground = null;
        }
    }

    private static void DestroyRollDisplays(ref SpriteRenderer[] displays)
    {
        if (displays == null) return;
        foreach (SpriteRenderer renderer in displays)
        {
            if (renderer != null) Destroy(renderer.gameObject);
        }
        displays = null;
    }
}
