using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace Supervisor
{
    public class SupervisorAgentRandom : SupervisorAgent, ISupervisorAgentRandom
    {
        [field: SerializeField, Tooltip("Defines the range around the DecisionRequestIntervalInSeconds in which the agent should perform an action. " +
            "For example if the range and DecisionRequestIntervalInSeconds are equal to 1 then decisions are requested between 0.5 and 1.5 seconds."), ProjectAssign]
        public float DecisionRequestIntervalRangeInSeconds { get; set; }


        private readonly Random _rand = new();

        private double _chosenInterval = 0;


        //The active instance will be selected according to the selected action of the agent. The Reward will be increased every x seconds like
        //defined in DecisionRequestIntervalInSeconds. If the Ball fell off a platform, the episode ends and a negative reward is given.
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            int action = UseHeuristic ? actionBuffers.DiscreteActions[0] : _rand.Next(0, Tasks.Length);

            Act(action);
            ResolveInteraction(actionBuffers.DiscreteActions[0]);
        }


        protected override void OnEnable()
        {
            base.OnEnable();

            SupervisorAgent.OnTaskSwitchCompleted += UpdateSwitchingInterval;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SupervisorAgent.OnTaskSwitchCompleted -= UpdateSwitchingInterval;
        }

        protected override void RequestInteractionAfterInterval()
        {
            bool hasIntervalExpired = _fixedUpdateTimer > _chosenInterval + AdvanceNoticeInSeconds;

            if (SetConstantDecisionRequestInterval)
            {
                RequestInteractionAfterConstantInterval(hasIntervalExpired);
            }
            else
            {
                RequestInteractionAfterVariableInterval(hasIntervalExpired);
            }
        }


        private new void Awake()
        {
            base.Awake();
            UpdateSwitchingInterval();
        }

        private void UpdateSwitchingInterval(double timeBetweenSwitches = 0, ISupervisorAgent supervisorAgent = default, bool isNewEpisode = false)
        {
            _chosenInterval = DecisionRequestIntervalInSeconds + _rand.NextDouble() * DecisionRequestIntervalRangeInSeconds - DecisionRequestIntervalRangeInSeconds / 2;

            //Give the player time to react. If the next switch would be fast again, it could be the case that the player had not enough time to react
            //which leads to no measurement of a reaction time.
            if (timeBetweenSwitches < 0.8)
            {
                _chosenInterval = 1.5;
            }

            Debug.Log(string.Format("Time between task switches: {0}", timeBetweenSwitches));
            Debug.Log(string.Format("New update interval: {0}", _chosenInterval));
        }
    }
}