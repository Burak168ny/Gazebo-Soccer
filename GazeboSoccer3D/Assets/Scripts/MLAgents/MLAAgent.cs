using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MLAAgents : Agent
{
    [SerializeField] private MLABall ballLaunche;
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeedX = 30f;
    [SerializeField] private float moveSpeedZ = 30f;
    private Vector3 goal;
    private Rigidbody rb;
    public Material envMaterial;
    public GameObject env;
    public float Force = 10f;

    public override void Initialize()
    {
        envMaterial = env.GetComponent<Renderer>().material;
        rb = this.GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        if (ballLaunche != null)
        {
            ballLaunche.classObjec = this;
        }

        transform.localPosition = new Vector3(Random.Range(-12.5f, 1.2f), -21, Random.Range(16, 20f));
        target.localPosition = new Vector3(Random.Range(16f, -28f), Random.Range(-23f, -17.5f), Random.Range(30f, 50));

        goal = new Vector3(Random.Range(-15f, 3.5f), target.localPosition.y, 17);

        ballLaunche.SetTarget(target);
        ballLaunche.LaunchBall();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(target.localPosition);
        sensor.AddObservation(ballLaunche.GetBallVelocity());
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0] * moveSpeedX;
        float moveZ = actions.ContinuousActions[1] * moveSpeedZ;

        Vector3 velocity = new Vector3(moveX, 0f, moveZ) * Time.deltaTime;
        transform.localPosition += velocity;

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
        if (other.gameObject.tag == "Ball")
        {
            envMaterial.color = Color.green;
            AddReward(15f);
            EndEpisode();
        }
        if (other.gameObject.tag == "Cage")
        {
            envMaterial.color = Color.red;
            AddReward(-4f);
            EndEpisode();
        }
    }
}
