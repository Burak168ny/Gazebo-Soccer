using Fusion.Sockets;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class PlayerControllerNet : NetworkBehaviour
{
    public float speed = 0.3f;
    public float jumpForce = 10f;
    private bool _isGrounded;
    private bool _canDoubleJump;
    private Rigidbody _rb;
    public float _speedMultiplier = 2f; // Başlangıç katsayısı
    private float _accelerationRate = 0.01f; // Hızın azalım oranı

    [Networked] private TickTimer delay { get; set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            HandleMovement(data);
            HandleJump(data);
        }
    }



private void HandleMovement(NetworkInputData data)
{
    Vector3 moveInput = new Vector3(data.direction.x, 0, data.direction.z);
    Vector3 localForward = transform.forward;
    Vector3 localRight = transform.right;
    Vector3 movement = (localForward * moveInput.z) + (localRight * moveInput.x);

    if (movement.magnitude > 0)
    {
        movement.Normalize();
    }

    // Başlangıçta hızlı hareket, zamanla hız azalır
    _rb.AddForce(movement * speed * _speedMultiplier, ForceMode.VelocityChange);

    // Hız katsayısını kademeli olarak azalt
    _speedMultiplier = Mathf.Max(1f, _speedMultiplier - _accelerationRate * Time.deltaTime);
}


    private void HandleJump(NetworkInputData data)
    {
        if (data.buttons.IsSet(NetworkInputData.JUMP))
        {
            if (_isGrounded)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpForce, _rb.linearVelocity.z);
                _canDoubleJump = true;
            }
            else if (_canDoubleJump)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpForce, _rb.linearVelocity.z);
                _canDoubleJump = false;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = false;
        }
    }
}
