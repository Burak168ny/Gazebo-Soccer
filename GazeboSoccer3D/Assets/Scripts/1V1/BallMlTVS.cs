using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallMlTVS : MonoBehaviour
{
    private Rigidbody ballRigidbody;
    [SerializeField] private Transform target;
    [SerializeField] private float power;
    private Vector3 goal;
    public AgentControllerTVS classObjecttt;
    [SerializeField] private Material goalMaterial; // Goal durumunda atanacak materyal
        [SerializeField] private Material arenaMaterial;
            private int score = 0; // Skor için integer değişken



    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }

    // public void LaunchBall()
    // {
    //     if (target == null)
    //     {
    //         Debug.LogError("Target not set for ball launcher.");
    //         return;
    //     }

    //     // Topun hızını sıfırlayın
    //     ballRigidbody.velocity = Vector3.zero;
    //     ballRigidbody.angularVelocity = Vector3.zero; // Dönme hızını da sıfırlayın

    //     goal = new Vector3(Random.Range(-14.5f, 3f), target.localPosition.y, 16);
    //     Vector3 direction = (goal - target.localPosition).normalized;

    //     ballRigidbody.AddForce(direction * power, ForceMode.Impulse);
    // }


    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public Vector3 GetBallVelocity()
    {
        return -ballRigidbody.linearVelocity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Goal")
        {
                        IncrementScore(); // Skoru artır

            if (classObjecttt != null)
            {
                Renderer envRenderer = classObjecttt.env.GetComponent<Renderer>();

                if (envRenderer != null && goalMaterial != null)
                {
                    envRenderer.material = goalMaterial;
                }
                classObjecttt.AddReward(-6f);
                classObjecttt.EndEpisode();
            }
        }
        if (other.gameObject.tag == "Arena")
        {
            if (classObjecttt != null)
            {
                Renderer envRenderer = classObjecttt.env.GetComponent<Renderer>();

                if (envRenderer != null && goalMaterial != null)
                {
                    envRenderer.material = arenaMaterial;
                }
                classObjecttt.EndEpisode();
            }
        }
    }
        private void IncrementScore()
    {
        score++;
        Debug.Log($"Current Score: {score}"); // Skoru konsola yazdır
    }

    public int GetScore()
    {
        return score; // Skor değerini döndür
    }

    public void ResetScore()
    {
        score = 0; // Skoru sıfırla
    }
}
