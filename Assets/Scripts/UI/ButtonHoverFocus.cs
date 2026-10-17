using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shows a selectable's assigned focus graphic while it is hovered or keyboard-selected.
/// </summary>
public sealed class ButtonHoverFocus : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Selectable selectable;
    private Graphic focusGraphic;
    private bool pointerIsOver;
    private bool focusAvailableWhenDisabled;

    public void Initialize(Selectable targetSelectable, Graphic targetFocusGraphic)
    {
        selectable = targetSelectable;
        focusGraphic = targetFocusGraphic;
        RefreshFocus();
    }

    public void SetFocusAvailable(bool availableWhenButtonDisabled)
    {
        focusAvailableWhenDisabled = availableWhenButtonDisabled;
        RefreshFocus();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerIsOver = true;
        RefreshFocus();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerIsOver = false;
        RefreshFocus();
    }

    private void Update()
    {
        RefreshFocus();
    }

    private void OnDisable()
    {
        pointerIsOver = false;
        if (focusGraphic != null)
        {
            focusGraphic.enabled = false;
        }
    }

    private void RefreshFocus()
    {
        if (focusGraphic == null || selectable == null)
        {
            return;
        }

        bool selected = EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject == selectable.gameObject;
        bool focusAvailable = selectable.IsInteractable() || focusAvailableWhenDisabled;
        focusGraphic.enabled = focusAvailable && (pointerIsOver || selected);
    }
}
