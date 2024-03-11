using Unity.MLAgents.Actuators;

namespace Supervisor
{
    public interface ISupervisorAgentRandom : ISupervisorAgent
    {
        float DecisionRequestIntervalRangeInSeconds { get; set; }

        new void OnActionReceived(ActionBuffers actionBuffers);
    }
}