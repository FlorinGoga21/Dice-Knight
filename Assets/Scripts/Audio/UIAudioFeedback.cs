using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Mouse and controller feedback for existing uGUI controls.</summary>
public sealed class UIAudioFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private UnityEngine.UI.Selectable selectable;
    private UnityEngine.UI.Button button;
    private UnityEngine.UI.Slider slider;
    private UnityEngine.UI.Toggle toggle;

    private void Awake()
    {
        selectable = GetComponent<UnityEngine.UI.Selectable>();
        button = GetComponent<UnityEngine.UI.Button>();
        slider = GetComponent<UnityEngine.UI.Slider>();
        toggle = GetComponent<UnityEngine.UI.Toggle>();
        if (button != null) button.onClick.AddListener(Confirm);
        if (slider != null) slider.onValueChanged.AddListener(OnSliderChanged);
        if (toggle != null) toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Confirm);
        if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
        if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    public void OnPointerEnter(PointerEventData data) => Focus();
    public void OnSelect(BaseEventData data) => Focus();
    private void Focus() { if (selectable != null && selectable.IsInteractable()) GameAudio.Play(GameAudio.Cue.UiFocus); }
    private void Confirm() => GameAudio.Play(GameAudio.Cue.UiConfirm);
    private void OnSliderChanged(float value) => GameAudio.Play(GameAudio.Cue.SettingChanged);
    private void OnToggleChanged(bool value) => GameAudio.Play(GameAudio.Cue.SettingChanged);
}
