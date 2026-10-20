using UnityEngine;

public partial class HealthHeartsUI
{
    private void EnsureStructure()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;

        // UI elements require a RectTransform on the root
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = gameObject.AddComponent<RectTransform>();
        }

        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 150;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        transform.localScale = Vector3.one * uiScale;

        // Ensure container for layout
        Transform container = transform.Find("HeartContainer");
        GameObject containerObj;
        if (container == null)
        {
            containerObj = new GameObject("HeartContainer", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup));
            containerObj.transform.SetParent(transform, false);
            container = containerObj.transform;
        }
        else
        {
            containerObj = container.gameObject;
        }

        containerRect = container as RectTransform;
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;

        layoutGroup = containerObj.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = containerObj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        }

        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.spacing = spacing;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
    }

    private void SyncHeartCount(int targetCount)
    {
        // Add hearts if count is lower than required
        while (heartIcons.Count < targetCount)
        {
            int index = heartIcons.Count;
            CreateHeartSlot(index);
        }

        // Enable or disable to match target count
        for (int i = 0; i < heartIcons.Count; i++)
        {
            bool active = i < targetCount;
            if (heartIcons[i] != null)
            {
                heartIcons[i].transform.parent.gameObject.SetActive(active);
            }
        }

        // Adjust container width to center cleanly
        float totalWidth = (targetCount * heartSize.x) + ((targetCount - 1) * spacing);
        containerRect.sizeDelta = new Vector2(totalWidth, heartSize.y);
    }

    private void CreateHeartSlot(int index)
    {
        GameObject slotObj = new GameObject($"Heart_{index}", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
        slotObj.transform.SetParent(containerRect, false);

        UnityEngine.UI.LayoutElement layoutElement = slotObj.GetComponent<UnityEngine.UI.LayoutElement>();
        layoutElement.preferredWidth = heartSize.x;
        layoutElement.preferredHeight = heartSize.y;

        RectTransform slotRect = (RectTransform)slotObj.transform;
        slotRect.sizeDelta = heartSize;
        heartSlotRects.Add(slotRect);

        // Main Heart UnityEngine.UI.Image
        GameObject heartObj = new GameObject("HeartIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        heartObj.transform.SetParent(slotObj.transform, false);
        RectTransform heartRect = (RectTransform)heartObj.transform;
        heartRect.anchorMin = Vector2.zero;
        heartRect.anchorMax = Vector2.one;
        heartRect.offsetMin = Vector2.zero;
        heartRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image heartImg = heartObj.GetComponent<UnityEngine.UI.Image>();
        heartImg.raycastTarget = false;
        heartImg.sprite = fullHeartSprite;

        Material dissolveMatInstance = null;
        if (heartDissolveMaterial != null)
        {
            dissolveMatInstance = new Material(heartDissolveMaterial);
            dissolveMatInstance.SetFloat("_Progress", 0f);
            heartImg.material = dissolveMatInstance;
        }

        heartIcons.Add(heartImg);
        dissolveMaterials.Add(dissolveMatInstance);
        dissolveCoroutines.Add(null);

        // Heart Outline UnityEngine.UI.Image
        UnityEngine.UI.Image outlineImg = null;
        Material outlineMatInstance = null;

        if (heartOutlineMaterial != null)
        {
            GameObject outlineObj = new GameObject("HeartOutline", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            outlineObj.transform.SetParent(heartObj.transform, false);
            RectTransform outlineRect = (RectTransform)outlineObj.transform;
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;

            outlineImg = outlineObj.GetComponent<UnityEngine.UI.Image>();
            outlineImg.raycastTarget = false;
            outlineImg.sprite = fullHeartSprite;

            outlineMatInstance = new Material(heartOutlineMaterial);
            outlineMatInstance.SetFloat("_OutlineOnly", 1f);
            outlineImg.material = outlineMatInstance;
        }

        outlineIcons.Add(outlineImg);
        outlineMaterials.Add(outlineMatInstance);
    }

    private void SyncOutline(int index)
    {
        if (index < 0 || index >= outlineIcons.Count)
        {
            return;
        }

        UnityEngine.UI.Image outline = outlineIcons[index];
        if (outline != null && index < heartIcons.Count && heartIcons[index] != null)
        {
            outline.sprite = heartIcons[index].sprite;
            outline.enabled = heartIcons[index].enabled;
        }
    }

    private void CleanupMaterials()
    {
        foreach (Material mat in dissolveMaterials)
        {
            if (mat != null)
            {
                Destroy(mat);
            }
        }
        dissolveMaterials.Clear();

        foreach (Material mat in outlineMaterials)
        {
            if (mat != null)
            {
                Destroy(mat);
            }
        }
        outlineMaterials.Clear();
    }
}
