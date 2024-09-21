using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class Ball3DAgentOptimal : BallAgent
{
    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
        sensor.AddObservation(gameObject.transform.rotation.z);
        sensor.AddObservation(gameObject.transform.rotation.x);
        sensor.AddObservation(Ball.transform.position - gameObject.transform.position);
        sensor.AddObservation(_ballRb.velocity);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);

        _ballRb.WakeUp();
        RotateZ(2f * Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f));
        RotateX(2f * Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f));

        if (IsAutonomous)
        {
            if ((Ball.transform.position.y - gameObject.transform.position.y) < -2f ||
                Mathf.Abs(Ball.transform.position.x - gameObject.transform.position.x) > 5f ||
                Mathf.Abs(Ball.transform.position.z - gameObject.transform.position.z) > 5f)
            {
                EndEpisode();
            }
        }

        var curDist = Vector3.Distance(Ball.transform.localPosition, gameObject.transform.localPosition);
        //divided by the radius of the platform (which is 5)
        float reward = 1 - curDist / 5;

        TaskRewardForFocusAgent.Enqueue(reward);
        TaskRewardForSupervisorAgent.Enqueue(reward);
        SetReward(reward);
    }
}
