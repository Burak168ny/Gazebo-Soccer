using UnityEngine;
using Fusion;

public class Player : NetworkBehaviour
{
    private SilentBall ball;
    public float throwForce = 1f;
    private bool hasPossession = false;

    private void Awake()
    {
        ball = FindObjectOfType<SilentBall>();
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && Runner.TryGetInputForPlayer(Object.InputAuthority, out NetworkInputData inputData))
        {
            // Şut atma işlemi
            if (inputData.buttons.IsSet(NetworkInputData.SHOOT) && hasPossession)
            {
                Vector3 force = CalculateShootForce(inputData.shootDirection);
                ShootBall(force);
            }

            // Eğer topun sahibi isek topu güncel pozisyonda tut
            if (hasPossession)
            {
                UpdateBallPosition();
            }
        }
    }

    private void GainPossession()
    {
        if (ball != null && HasStateAuthority)
        {
            ball.AssignOwner(GetComponent<NetworkObject>());
            ball.rb.isKinematic = true;
            hasPossession = true;
            UpdateBallPosition();
            Debug.Log("Top kontrolü alındı.");
        }
    }

    private void ShootBall(Vector3 force)
    {
        if (hasPossession && ball.IsOwnedBy(GetComponent<NetworkObject>()))
        {
            ball.rb.isKinematic = false;
            ball.SThrowBall(force);
            hasPossession = false;
            Debug.Log("Top şutlandı.");
        }
    }

    private void UpdateBallPosition()
    {
        Vector3 forwardOffset = transform.forward * 1.5f;
        Vector3 upOffset = transform.up * 0.5f;

        ball.transform.position = transform.position + forwardOffset + upOffset;
        ball.transform.rotation = transform.rotation;
    }

    private Vector3 CalculateShootForce(float shootAngle)
    {
        float radians = shootAngle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));
        return transform.TransformDirection(direction) * throwForce;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.GetComponent<SilentBall>() && HasStateAuthority)
        {
            GainPossession();
        }
    }
}
