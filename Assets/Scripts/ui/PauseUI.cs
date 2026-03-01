using UnityEngine;
using UnityEngine.InputSystem;

public class PauseUI : BaseUI
{
    void Awake()
    {
        base.Awake();
        this.gameObject.SetActive(false);

        GameLogic.playerInputActions.Player.Pause.performed += Pause;
    }

    public override void OpenUI(System.Action closeCb)
    {
        base.OpenUI(closeCb);
        this.gameObject.SetActive(true);
        GameLogic.Paused = true;
        Time.timeScale = 0.0f;
    }

    public override void CloseUI()
    {
        Debug.Log("Called here?");
        Time.timeScale = 1.0f;
        GameLogic.Paused = false;
        this.gameObject.SetActive(false);
        base.CloseUI();
    }

    void Pause(InputAction.CallbackContext context)
    {
        if(!Opened) this.OpenUI(null);
    }

    void OnDestroy()
    {
        GameLogic.playerInputActions.Player.Pause.performed -= Pause;
    }
}
