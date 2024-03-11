using Supervisor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SupervisorExtension
{
    public static BallAgent[] GetBallAgents(this SupervisorAgent supervisorAgent)
    {
        List<BallAgent> ballAgents = new();

        foreach (GameObject taskGameobject in supervisorAgent.TaskGameObjects)
        {
            ITask task = taskGameobject.transform.GetChildByName("Agent").GetComponent<ITask>();

            if (task.GetType().IsSubclassOf(typeof(BallAgent)))
            {
                ballAgents.Add((BallAgent)task);
            }
        }

        return ballAgents.ToArray();
    }
}
