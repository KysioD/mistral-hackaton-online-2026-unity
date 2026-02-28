using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 10.0f;
    [SerializeField] private float sprintSpeed = 15.0f;
    [SerializeField] private float cameraSensitivity = 1.0f;
    [SerializeField] private float jumpStrength = 9.81f / 2;
    [SerializeField] private float interactRange = 2.0f;

    private PlayerInputActions playerInputActions;
    private Camera camera;
    private Rigidbody rigidBody;

    private float xRotation, yRotation;
    private bool grounded = true;
    private float lastJump = 0.0f;

    void Awake()
    {
        playerInputActions = new PlayerInputActions();
        camera = GetComponentInChildren<Camera>();
        rigidBody = GetComponent<Rigidbody>();

        playerInputActions.Player.Enable();
        playerInputActions.Player.Jump.performed += Jump;
        playerInputActions.Player.Interact.performed += Interact;
        playerInputActions.Player.Attack.performed += Attack;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Time.time - lastJump > 0.2f && (Physics.SphereCast(transform.position, 1.0f, -transform.up, out RaycastHit raycastInfo, 0.1f) || rigidBody.linearVelocity.y==0))
        {
            grounded = true;
        }
    }

    void FixedUpdate()
    {
        float moveSpeed = playerInputActions.Player.Sprint.IsPressed() ? sprintSpeed : walkSpeed;

        Vector2 moveInputVector = playerInputActions.Player.Move.ReadValue<Vector2>();
        Vector3 velocity = Vector3.zero;
        velocity += moveInputVector.x * camera.transform.right;
        velocity += moveInputVector.y * camera.transform.forward;
        velocity *= grounded ? moveSpeed : moveSpeed / 2;
        rigidBody.AddForce(velocity, ForceMode.Force);

        Vector2 lookInputVector = playerInputActions.Player.Look.ReadValue<Vector2>();
        float mouseX = lookInputVector.x * cameraSensitivity;
        float mouseY = lookInputVector.y * cameraSensitivity;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90.0f, 90.0f);
        camera.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0.0f);
    }

    void Jump(InputAction.CallbackContext context)
    {
        if (!grounded) return;
        rigidBody.AddForce(new Vector3(0.0f, jumpStrength, 0.0f), ForceMode.Impulse);
        grounded = false;
        lastJump = Time.time;
    }

    void Interact(InputAction.CallbackContext context)
    {
        Debug.Log("INTERACT");
    }

    void Attack(InputAction.CallbackContext context)
    {
        Debug.Log("ATTACK");    
    }

    void OnDestroy()
    {
        playerInputActions.Player.Disable();
        playerInputActions.UI.Disable();
    }
}
