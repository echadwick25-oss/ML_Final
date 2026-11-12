using JetBrains.Annotations;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoveToTargetAgent : Agent
{

    
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Renderer floor;
    [SerializeField] private Color winColour = Color.green;
    [SerializeField] private Color loseColour = Color.red;
    [SerializeField] private Color idleColour = Color.grey;
    public GameObject[] checkPoints;
    private int nextCheckpoint;

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(8.25f, 0.5f, -8.5f);
        nextCheckpoint = 0;
        //target.localPosition = new Vector3(
        //Random.Range(-4f, 4f), 0.5f, Random.Range(1f, 4f));
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 toNext = PointAt(nextCheckpoint) - transform.position;
        toNext.y = 0f;
        Vector3 toAhead = PointAt(nextCheckpoint + 4) - transform.position;
        toAhead.y = 0f;

        sensor.AddObservation(toNext.normalized.x);
        sensor.AddObservation(toNext.normalized.z);
        sensor.AddObservation(Mathf.Clamp01(toNext.magnitude / 5f));
        sensor.AddObservation(toAhead.normalized.x);
        sensor.AddObservation(toAhead.normalized.z);
    }

    private Vector3 PointAt(int index)
    {
        if (index < checkPoints.Length) return checkPoints[index].transform.position;
        return target.position;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        Vector3 move = new Vector3(moveX, 0f, moveZ);
        transform.localPosition += move * moveSpeed * Time.deltaTime;

        // Small penalty every step so it learns to hurry
        if (MaxStep > 0) AddReward(-1f / MaxStep);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;
        var kb = Keyboard.current;
        if (kb ==  null) return;

        continuous[0] = (kb.dKey.isPressed ? 1f : 0f)
                      - (kb.aKey.isPressed ? 1f : 0f);
        continuous[1] = (kb.wKey.isPressed ? 1f : 0f)
                      - (kb.sKey.isPressed ? 1f : 0f);
    }

    public override void Initialize() // runs once at startup
    {
        floor.material.color = idleColour;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            AddReward(5f);
            floor.material.color = winColour;
            EndEpisode();
        }
        else if (other.CompareTag("Wall"))
        {
            AddReward(-2f);
            floor.material.color = loseColour;
            EndEpisode();
        }

        if (nextCheckpoint < checkPoints.Length && other.gameObject == checkPoints[nextCheckpoint])
        {
            print("checkpoint touched");
            AddReward(0.1f * (nextCheckpoint + 1));
            nextCheckpoint++;
        }
    }
}