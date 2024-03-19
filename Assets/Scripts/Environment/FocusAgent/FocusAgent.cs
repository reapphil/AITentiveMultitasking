using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class FocusAgent : Agent
{
    [field: SerializeField]
    public GameObject[] TaskGameObjects { get; set; }

    public ITask[] Tasks { get; set; }


    private Supervisor.SupervisorAgent _supervisorAgent;

    private float _fixedUpdateTimer = 0.0f;


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Focus(Tasks[actionBuffers.DiscreteActions[0]]);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(_supervisorAgent.GetActiveTaskNumber());

        foreach (ITask task in Tasks)
        {
            if (task.GetType().GetInterfaces().Contains(typeof(ICrTask)))
            {
                ((ICrTask)task).AddPerceivedObservationsToSensor(sensor);
            }
            else
            {
                task.AddObservationsToSensor(sensor);
            }

        }
    }

    public override void Initialize()
    {
        _supervisorAgent = gameObject.transform.root.GetComponent<Supervisor.SupervisorAgent>();

        Focus(Tasks[0]);
        _fixedUpdateTimer = 0;
    }


    protected void Awake()
    {
        Tasks = ITask.GetTasksFromGameObjects(TaskGameObjects);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Supervisor.SupervisorAgent.EndEpisodeEvent += CatchEndEpisode;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Supervisor.SupervisorAgent.EndEpisodeEvent -= CatchEndEpisode;
    }


    private void FixedUpdate()
    {
        _fixedUpdateTimer += Time.fixedDeltaTime;

        //could be a parameter of the model
        if (_fixedUpdateTimer > 0.2)
        {
            RequestDecision();
            _fixedUpdateTimer = 0;
        }

        SetReward(Time.fixedDeltaTime);
    }

    private void Focus(ITask toBeFocused)
    {
        foreach (ITask task in Tasks)
        {
            if (task.GetType().GetInterfaces().Contains(typeof(ICrTask)))
            {
                ((ICrTask)task).IsFocused = false;
            }

            task.GetGameObject().transform.parent.transform.GetChildByName("Camera").GetChildByName("Eye_Canvas").gameObject.SetActive(false);
        }

        if (toBeFocused.GetType().GetInterfaces().Contains(typeof(ICrTask)))
        {
            ((ICrTask)toBeFocused).IsFocused = true;
        }
        toBeFocused.GetGameObject().transform.parent.transform.GetChildByName("Camera").GetChildByName("Eye_Canvas").gameObject.SetActive(true);
    }

    private void CatchEndEpisode(object sender, bool aborted)
    {
        EndEpisode();
    }
}
