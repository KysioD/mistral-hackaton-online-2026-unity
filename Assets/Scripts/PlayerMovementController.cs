using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 10.0f;
    [SerializeField] private float sprintSpeed = 15.0f;
    [SerializeField] private float cameraSensitivity = 0.1f;
    [SerializeField] private float jumpStrength = 9.81f / 2;

    private Camera camera;
    private Rigidbody rigidBody;

    private float xRotation, yRotation;
    private bool grounded = true;
    private float lastJumpTime = 0.0f;

    void Awake()
    {
        camera = GetComponentInChildren<Camera>();
        rigidBody = GetComponent<Rigidbody>();

        GameLogic.playerInputActions.Player.Jump.performed += Jump;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Time.time - lastJumpTime > 0.2f && (Physics.SphereCast(transform.position, 1.0f, -transform.up, out RaycastHit raycastInfo, 0.1f) || rigidBody.linearVelocity.y == 0))
        {
            grounded = true;
        }

        Vector2 lookInputVector = GameLogic.playerInputActions.Player.Look.ReadValue<Vector2>();
        float mouseX = lookInputVector.x * cameraSensitivity;
        float mouseY = lookInputVector.y * cameraSensitivity;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90.0f, 90.0f);
        transform.localRotation = Quaternion.Euler(0.0f, yRotation, 0.0f);
        camera.transform.localRotation = Quaternion.Euler(xRotation, 0.0f, 0.0f);
    }

    void FixedUpdate()
    {
        float moveSpeed = GameLogic.playerInputActions.Player.Sprint.IsPressed() ? sprintSpeed : walkSpeed;

        Vector2 moveInputVector = GameLogic.playerInputActions.Player.Move.ReadValue<Vector2>();
        Vector3 velocity = Vector3.zero;
        velocity += moveInputVector.x * transform.right;
        velocity += moveInputVector.y * transform.forward;
        velocity *= grounded ? moveSpeed : moveSpeed / 2;
        rigidBody.AddForce(velocity, ForceMode.Force);
    }

    void Jump(InputAction.CallbackContext context)
    {
        if (!grounded) return;
        rigidBody.AddForce(new Vector3(0.0f, jumpStrength, 0.0f), ForceMode.Impulse);
        grounded = false;
        lastJumpTime = Time.time;
    }

    void OnDestroy()
    {
        GameLogic.playerInputActions.Player.Jump.performed -= Jump;
    }
}