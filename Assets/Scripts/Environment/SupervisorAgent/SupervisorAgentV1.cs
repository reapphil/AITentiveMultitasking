using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;


namespace Supervisor
{
    /**
     * Legacy class which was used for the first user study of KW26 2023 with the following differences:
	 *  - Different order in the observation vector (AddObservation was called in a different order)
	 *  - Time between switches was part of the state space
	 *  - SetTimeBetweenSwitches was called at the task switch decision- and not at task switch completion-time
     **/
    public class SupervisorAgentV1 : SupervisorAgent
    {
        private DateTime _agentSwitchTime;


        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(Tasks[0].GetGameObject().transform.rotation.z);
            sensor.AddObservation(Tasks[0].GetGameObject().transform.rotation.x);

            sensor.AddObservation(Tasks[1].GetGameObject().transform.rotation.z);
            sensor.AddObservation(Tasks[1].GetGameObject().transform.rotation.x);

            //distance between balls and centers of platforms
            sensor.AddObservation(Tasks[0].GetGameObject().transform.position - ((BallAgent)Tasks[0]).GetBall().transform.position);
            sensor.AddObservation(Tasks[1].GetGameObject().transform.position - ((BallAgent)Tasks[1]).GetBall().transform.position);

            sensor.AddObservation(((BallAgent)Tasks[0]).GetBallVelocity());
            sensor.AddObservation(((BallAgent)Tasks[1]).GetBallVelocity());

            sensor.AddObservation((float)((DateTime.Now - _agentSwitchTime).TotalSeconds));
            sensor.AddObservation(_switchCount);
        }


        protected override void PropagateTimeBetweenSwitches()
        {
            InvokeOnTaskSwitchCompleted();
            //_timeSinceLastSwitch = 0; <-- diff
        }

        protected override float DequeueReward()
        {
            float reward = (float)((DecisionRequestIntervalInSeconds / (1 + Math.Exp(-(Math.Exp(2) * (TimeSinceLastSwitch - 0.5))))));

            return reward;
        }

        protected override void SwitchAgentTo(int activeInstance)
        {
            UpdateAgentsActiveStatus(activeInstance);
            //_switchCount += 1; <-- diff
            PropagateTimeBetweenSwitches();

            if (FocusActiveTask)
            {
                FocusActiveInstance();
            }
        }

        protected override void SwitchingAction(int activeInstance)
        {
            _activeInstanceActionLevel = activeInstance;

            if (_previousActiveActionLevel != _activeInstanceActionLevel)
            {
                if (!(GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType == Unity.MLAgents.Policies.BehaviorType.HeuristicOnly))
                {
                    if (_audioSource != null)
                    {
                        _audioSource.Play();
                    }
                }

                if (AdvanceNoticeInSeconds > 0)
                {
                    _advanceNoticeTimer = AdvanceNoticeInSeconds;
                    StartCoroutine(DelayedAgentSwitchTo(AdvanceNoticeInSeconds, _activeInstanceActionLevel));
                }
                else
                {
                    SwitchAgentTo(_activeInstanceActionLevel);
                }

                _switchCount += 1; //<-- diff
                SetTimeBetweenSwitches(); //<-- diff
            }

            _previousActiveActionLevel = activeInstance;
        }

        protected override void NotificationAction(int targetInstance)
        {
            if (_previousActiveActionLevel != targetInstance)
            {
                if (!_isUserInput)
                {
                    GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType = Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;

                    StartCoroutine(base.Notification(Tasks[targetInstance]));

                    if (_audioSource != null)
                    {
                        _audioSource.Play();
                    }

                    _isUserInput = true;
                    _fixedNotificationExecutionTimer = 0;
                    _pendingInstance = targetInstance;
                }
                else
                {
                    _isUserInput = false;
                    GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType = Unity.MLAgents.Policies.BehaviorType.InferenceOnly;
                    _activeInstanceActionLevel = _previousActiveActionLevel = targetInstance;
                    SwitchAgentTo(_activeInstanceActionLevel);
                    SetTimeBetweenSwitches(); //<-- diff
                }
            }
        }


        private void SetTimeBetweenSwitches()
        {
            DateTime now = DateTime.Now;
            _agentSwitchTime = now;
            TimeSinceLastSwitch = 0;
        }
    }
}
