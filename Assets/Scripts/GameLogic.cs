using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameLogic : MonoBehaviour
{
    public static bool Paused { get; set; } = false;

    [SerializeField] private Transform pauseUi;

    public static PlayerInputActions playerInputActions;

    void Awake()
    {
        playerInputActions = new PlayerInputActions();
        pauseUi.gameObject.SetActive(false);

        playerInputActions.Global.Enable();
        playerInputActions.Player.Enable();
        playerInputActions.Global.Pause.performed += Pause;

        Debug.Log(new EntityStatus.Wanted().Serialize());
    }

    void Pause(InputAction.CallbackContext context)
    {
        GameLogic.Paused = !GameLogic.Paused;
        if (GameLogic.Paused)
        {
            pauseUi.gameObject.SetActive(true);
            playerInputActions.Player.Disable();
            playerInputActions.UI.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0.0f;
        }
        else
        {
            pauseUi.gameObject.SetActive(false);
            playerInputActions.Player.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1.0f;
        }
    }

    void OnDestroy()
    {
        playerInputActions.Global.Disable();
        playerInputActions.Player.Disable();
        playerInputActions.UI.Disable();
    }
}
