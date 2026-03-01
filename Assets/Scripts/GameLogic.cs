using UnityEngine;

public class GameLogic : MonoBehaviour
{
    public static bool Paused { get; set; } = false;
    public static bool InUi { set; get; } = false;

    public static PlayerInputActions playerInputActions;

    void Awake()
    {
        playerInputActions = new PlayerInputActions();

        playerInputActions.Player.Enable();
    }

    void OnDestroy()
    {
        playerInputActions.Player.Disable();
        playerInputActions.UI.Disable();
    }
}
