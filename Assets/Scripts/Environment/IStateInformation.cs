using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine;

[Measure]
public interface IStateInformation
{
    [Measure]
    public string Name { get => this.GetType().Name; }

    public Array AveragePerformedActionsDiscretizedSpace { get; set; }

    List<dynamic> PerformedActions { get; }

    public Dictionary<Type, Array> AverageReactionTimesDiscretizedSpace { get; set; }

    /// <summary>
    /// The maximum value of the possible actions.
    /// </summary>
    public Vector3 ActionRangeMax { get; }

    /// <summary>
    /// The minimum value of the possible actions.
    /// </summary>
    public Vector3 ActionRangeMin { get; }

    /// <summary>
    /// Number of action bins per axis to determine if an action is in the usual range.
    /// </summary>
    public int NumberOfActionBinsPerAxis { get; }

    /// <summary>
    /// Number of behavioral bins per dimension.
    /// </summary>
    public int[] BehaviorDimensions { get; }

    /// <summary>
    /// Returns the relational dimensions of the task in relation to the source task. Must be implemented by the task to allow for the analysis of 
    /// the reaction time.
    /// </summary>
    /// <param name="stateInformation"></param>
    /// <returns>The number of bins per dimension</returns>
    public int[] GetRelationalDimensions(Type type, int numberOfTimeBins = 1);

    public bool ActionIsInUsualRange(List<dynamic> actionsPerformedSoFar, List<dynamic> performedAction);

    /// <summary>
    /// Returns discretized state information of the task in relation to the source task and is used to analyze the reaction time. For instance, if
    /// the source task is equal to this, the discretization should described the distance between the states.
    /// </summary>
    /// <param name="sourceTaskState"></param>
    /// <returns>The bins of the different dimensions of the current state</returns>
    public int[] GetDiscretizedRelationalStateInformation(IStateInformation sourceTaskState, int timeBin = 0);


    /// <summary>
    /// Returns discretized state information of the task and is used to analyze the behavior.
    /// </summary>
    /// <returns>The bins of the different dimensions of the current state</returns>
    public int[] GetDiscretizedStateInformation();

    /// <summary>
    /// Updates the current state information based on the given stateInformation.
    /// </summary>
    /// <param name="stateInformation"></param>
    public void UpdateStateInformation(IStateInformation stateInformation);

    /// <summary>
    /// Updates the current measurement settings based on the given stateInformation.
    /// </summary>
    /// <param name="stateInformation"></param>
    public void UpdateMeasurementSettings(IStateInformation stateInformation);

    /// <summary>
    /// Returns a new instance of this object with the current state information.
    /// </summary>
    public IStateInformation GetCopyOfCurrentState();
}
