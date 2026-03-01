using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class BaseUI : MonoBehaviour
{
    private System.Action closeCallback;
    protected bool Opened { get; set; } = false;

    protected void Awake()
    {
        GameLogic.playerInputActions.UI.Close.performed += Close;
    }

    public virtual void OpenUI(System.Action closeCallback)
    {
        this.closeCallback = closeCallback;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameLogic.playerInputActions.Player.Disable();
        GameLogic.playerInputActions.UI.Enable();
        GameLogic.InUi = true;
        Opened = true;
    }

    public virtual void CloseUI()
    {
        if (!Opened) return;
        Opened = false;
        if (this.closeCallback != null) this.closeCallback.Invoke();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GameLogic.playerInputActions.UI.Disable();
        GameLogic.playerInputActions.Player.Enable();
        GameLogic.InUi = false;
    }

    void Close(InputAction.CallbackContext ctx)
    {
        if (!Opened) return;
        this.CloseUI();
    }

    void OnDestroy()
    {
        GameLogic.playerInputActions.UI.Close.performed -= Close;
    }
}