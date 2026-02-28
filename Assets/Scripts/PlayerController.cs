using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform hud;

    private PlayerInputActions playerInputActions;
    private PlayerEntity entity;
    private PlayerHudController hudController;

    void Awake()
    {
        entity = new PlayerEntity();
        playerInputActions = new PlayerInputActions();

        playerInputActions.Player.Interact.performed += Interact;
        playerInputActions.Player.Attack.performed += Attack;

        hudController = hud.GetComponent<PlayerHudController>();
    }

    void FixedUpdate()
    {
        hudController.SetHealth(entity.Health);
        hudController.SetGold(entity.Gold);
        hudController.SetMagic(entity.MagicLevel);
    }

    void Interact(InputAction.CallbackContext context)
    {
        Debug.Log("INTERACT");
    }

    void Attack(InputAction.CallbackContext context)
    {
        Debug.Log("ATTACK");    
    }
}
