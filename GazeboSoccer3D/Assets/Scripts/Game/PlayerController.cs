using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f; // Hareket hızı
    public float jumpForce = 10f; // Zıplama kuvveti
    public Rigidbody rb;
    public FixedJoystick joystick; // Joystick bileşeni

    private bool canJump = true; // Zıplama durumunu kontrol eder

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        float moveX = joystick.Horizontal;
        float moveZ = joystick.Vertical;

        Vector3 moveDirection = new Vector3(moveX, 0, moveZ).normalized;

        if (moveDirection.magnitude > 0)
        {
            rb.AddForce(moveDirection * moveSpeed, ForceMode.Force);
        }
    }

    public void HandleJump()
    {
        if (canJump)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            canJump = false; // Tekrar yere inene kadar zıplayamaz
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            canJump = true; // Zeminle temas kurulduğunda zıplama izni ver
        }
    }
}
