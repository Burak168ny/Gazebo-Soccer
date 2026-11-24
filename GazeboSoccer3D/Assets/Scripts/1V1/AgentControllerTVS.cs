using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AgentControllerTVS : Agent
{
    [SerializeField] private BallMlTVS ballLauncherttt;
    [SerializeField] private BallControl ballControl;
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeedX = 30f;
    [SerializeField] private float moveSpeedZ = 30f;
    [SerializeField] private GameObject referenceObject; // Referans alacağımız GameObject

    private Vector3 goal;
    private Rigidbody rb;
    public Material envMaterial;
    public GameObject env;
    public float Force = 10f;
    public Material successMaterial; // Başarı durumunda atanacak materyal
    public Material failMaterial;    // Başarısızlık durumunda atanacak materyal
    

    public override void Initialize()
    {
        envMaterial = env.GetComponent<Renderer>().material;
        rb = this.GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        if (ballLauncherttt != null)
        {
            ballLauncherttt.classObjecttt = this;
        }

        transform.localPosition = new Vector3(Random.Range(-12.5f, 1.2f), -21, Random.Range(16, 20f));
        //transform.localPosition = new Vector3(transform.localPosition.x, -21, transform.localPosition.z);
        // target.localPosition = new Vector3(Random.Range(16f, -28f), Random.Range(-23f, -17.5f), Random.Range(30f, 50));
        // target.position = referenceObject.transform.position;
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            targetRb.linearVelocity = Vector3.zero;  // Hızını sıfırla
            targetRb.angularVelocity = Vector3.zero;  // Dönüş hızını sıfırla
        }

        //target.localPosition = new Vector3(-6.5f, -22f, 53f);
target.position = referenceObject.transform.position + new Vector3(1.2f, 0f, 1.2f);


        goal = new Vector3(Random.Range(-15f, 3.5f), target.localPosition.y, 17);


        ballLauncherttt.SetTarget(target);
        // ballLauncherttt.LaunchBall();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition); // Kalecinin pozisyonu
        sensor.AddObservation(target.localPosition);     // Topun pozisyonu

    Vector3 ballVelocity = ballLauncherttt.GetBallVelocity();
    sensor.AddObservation(ballVelocity); // Topun hızı

    Debug.Log($"Ball Velocity observed by sensor: {ballVelocity}");    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0] * moveSpeedX;
        float moveZ = actions.ContinuousActions[1] * moveSpeedZ;

        Vector3 velocity = new Vector3(moveX, 0f, moveZ) * Time.deltaTime;
        transform.localPosition += velocity;

        // Zıplama aksiyonu, top yere yakınsa engellenir
        if (actions.ContinuousActions[2] > 0.5f && Mathf.Abs(rb.linearVelocity.y) < 0.01f && target.localPosition.y > -19f)
        {
            rb.AddForce(Vector3.up * Force, ForceMode.Impulse);
        }

        float movementPenalty = velocity.magnitude * -0.001f;
        AddReward(movementPenalty);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
        continuousActions[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        Renderer envRenderer = env.GetComponent<Renderer>();

        if (other.gameObject.tag == "Ball")
        {
            if (envRenderer != null && successMaterial != null)
            {
                envRenderer.material = successMaterial;
            }
            AddReward(15f);
            EndEpisode();
        }
        if (other.gameObject.tag == "Cage")
        {
            // if (envRenderer != null && failMaterial != null)
            // {
            //     envRenderer.material = failMaterial;
            // }
            AddReward(-4f);
            // EndEpisode();
        }
    }
}
