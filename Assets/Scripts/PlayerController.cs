using UnityEngine;

/// <summary>
/// Управляет передвижением игрока от первого лица.
/// Использует стандартный старый Input Manager Unity.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Transform cameraPivot;

    private CharacterController characterController;
    private float verticalVelocity;
    private float cameraPitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraPivot == null && Camera.main != null)
        {
            cameraPivot = Camera.main.transform;
        }

        LockCursor();
    }

    private void Update()
    {
        LookAround();
        Move();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }
    }

    private void LookAround()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                LockCursor();
            }
            else
            {
                return;
            }
        }

        float yaw = Input.GetAxis("Mouse X") * mouseSensitivity;
        float pitch = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * yaw);

        cameraPitch = Mathf.Clamp(cameraPitch - pitch, -85f, 85f);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void Move()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;
        if (Input.GetKey(KeyCode.W)) vertical += 1f;

        Vector3 direction = transform.right * horizontal + transform.forward * vertical;
        direction = Vector3.ClampMagnitude(direction, 1f);

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = direction * currentSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
