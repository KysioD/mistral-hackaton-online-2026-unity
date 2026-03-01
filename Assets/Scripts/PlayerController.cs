using npcs;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float interactRange = 2.0f;
    [SerializeField] private Transform hud;
    [SerializeField] private Transform npcUi;
    [SerializeField] private LayerMask npcLayer;

    private PlayerEntity entity;
    private PlayerHudController hudController;
    private NpcUi dialogueController;
    private Camera camera;

    void Awake()
    {
        entity = new PlayerEntity();

        npcUi.gameObject.SetActive(false);

        GameLogic.playerInputActions.Player.Interact.performed += Interact;
        GameLogic.playerInputActions.Player.Attack.performed += Attack;

        hudController = hud.GetComponent<PlayerHudController>();
        dialogueController = npcUi.GetComponent<NpcUi>();
        camera = GetComponentInChildren<Camera>();
    }

    void FixedUpdate()
    {
        //hudController.SetHealth(entity.Health);
        hudController.SetGold(entity.Gold);
        //hudController.SetMagic(entity.MagicLevel);
    }

    void Interact(InputAction.CallbackContext context)
    {
        Debug.Log("INTERACT");
        if(Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hitInfo, interactRange, npcLayer))
        {
            NpcBehavior npcBehavior = hitInfo.transform.GetComponentInParent<NpcBehavior>();
            if (npcBehavior!= null) 
            {
                dialogueController.Init(ref npcBehavior.GetNpcEntity());
            }
        }
    }

    void Attack(InputAction.CallbackContext context)
    {
        Debug.Log("ATTACK");    
    }

    void OnDestroy()
    {
        GameLogic.playerInputActions.Player.Interact.performed -= Interact;
        GameLogic.playerInputActions.Player.Attack.performed -= Attack;
    }
}
