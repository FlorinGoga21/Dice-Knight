using HeyHeyThere.SceneTransitionsFree;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Shared room-completion transition, also accessible through the Q development shortcut.</summary>
public sealed class RoomSceneController : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "MapScene";
    [SerializeField] private string transitionStyle = "clouds";
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.8f;

    private bool isReturningToMap;

    public bool IsReturningToMap => isReturningToMap;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            CompleteRoom();
    }

    public void CompleteRoom()
    {
        if (isReturningToMap || SceneTransition.IsPlaying || Time.timeScale <= 0f)
            return;
        if (!Application.CanStreamedLevelBeLoaded(mapSceneName))
        {
            Debug.LogError($"RoomSceneController: Scene '{mapSceneName}' is not in Build Settings.", this);
            return;
        }

        isReturningToMap = true;
        SaveManager.Instance?.CaptureRunPlayer();
        RunMapState.CompleteCurrentRoom();
        Time.timeScale = 1f;
        SceneTransition.ChangeScene(mapSceneName, transitionStyle,
            new SceneTransition.Options { Duration = transitionDuration });
    }
}
