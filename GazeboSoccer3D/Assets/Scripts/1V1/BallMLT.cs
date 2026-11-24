using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallMlT : MonoBehaviour
{
    private Rigidbody ballRigidbody;
    [SerializeField] private Transform target;
    [SerializeField] private float power;
    private Vector3 goal;
    public AgentControllerT classObjectt;

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }

    public void LaunchBall()
    {
        if (target == null)
        {
            Debug.LogError("Target not set for ball launcher.");
            return;
        }

        // Topun hızını sıfırlayın
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero; // Dönme hızını da sıfırlayın

        goal = new Vector3(Random.Range(-14.5f, 3f), target.localPosition.y, 16);
        Vector3 direction = (goal - target.localPosition).normalized;

        ballRigidbody.AddForce(direction * power, ForceMode.Impulse);
    }


    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public Vector3 GetBallVelocity()
    {
        return ballRigidbody.linearVelocity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Goal")
        {
            if (classObjectt != null)
            {
                classObjectt.envMaterial.color = Color.blue;
                classObjectt.AddReward(-6f);
                classObjectt.EndEpisode();
            }
        }
    }
}
