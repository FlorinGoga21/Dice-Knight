using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Restores focus inside the active menu after fades, clicks, or disabled buttons.</summary>
public static class UISelection
{
    public static void Ensure(Transform root, UnityEngine.UI.Selectable[] choices)
    {
        if (EventSystem.current == null) return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && selected.transform.IsChildOf(root)
            && selected.TryGetComponent(out UnityEngine.UI.Selectable selectable)
            && selectable.isActiveAndEnabled && selectable.IsInteractable()) return;

        foreach (var choice in choices)
        {
            if (choice != null && choice.isActiveAndEnabled && choice.IsInteractable())
            {
                choice.Select();
                return;
            }
        }
        EventSystem.current.SetSelectedGameObject(null);
    }
}
