using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls toggling the pause menu UI and manages game time scaling.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        pauseMenu = pauseMenu != null ? pauseMenu : gameObject;
        inputActions = new PlayerInputActions();
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        inputActions.Player.Pause.performed += OnPausePerformed;
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Pause.performed -= OnPausePerformed;
        inputActions.Disable();
    }

    private void OnDestroy()
    {
        inputActions.Dispose();
    }

    public void ReturnToGame()
    {
        SetPaused(false);
    }

    public void ExitGame()
    {
        SetPaused(false);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        SetPaused(!pauseMenu.activeSelf);
    }

    private void SetPaused(bool paused)
    {
        pauseMenu.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
    }
}
