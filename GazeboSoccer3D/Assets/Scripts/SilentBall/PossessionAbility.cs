using UnityEngine;
using Fusion;

public class PossessionAbility : NetworkBehaviour
{
    private SilentBall ball;

    public bool hasPossession = false;
    [SerializeField] private float throwForce = 50f;

    private void Awake()
    {
        ball = FindObjectOfType<SilentBall>();
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && Runner.TryGetInputForPlayer(Object.InputAuthority, out NetworkInputData inputData))
        {
            if (inputData.buttons.IsSet(NetworkInputData.TOGGLE_VISIBILITY))
            {
                ToggleBallVisibility();
            }

            if (inputData.buttons.IsSet(NetworkInputData.SHOOT) && hasPossession)
            {
                Vector3 force = CalculateShootForce(inputData.shootDirection);
                SThrowBall(force);
            }

            if (hasPossession)
            {
                UpdateBallPosition();
            }
        }
    }

    private void ToggleBallVisibility()
{
    if (hasPossession && ball != null)
    {
        bool newVisibilityState = !ball.IsInvisible;
        ball.SetInvisibility(newVisibilityState);
        Debug.Log("Top görünmezlik durumu değiştirildi: " + newVisibilityState);
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

    public void SThrowBall(Vector3 force)
    {
        if (hasPossession && ball.IsOwnedBy(GetComponent<NetworkObject>()))
        {
            ball.rb.isKinematic = false;
            ball.SThrowBall(force);
            hasPossession = false;
            Debug.Log("Şut atıldı.");
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
