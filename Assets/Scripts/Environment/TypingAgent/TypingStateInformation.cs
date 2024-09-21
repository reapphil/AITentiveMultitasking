using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TypingStateInformation : IStateInformation
{
    public Array PerformedActions { get; set; }
    public Dictionary<Type, Array> ReactionTimes { get; set; }

    public Vector3 ActionRangeMax => throw new NotImplementedException();

    public Vector3 ActionRangeMin => throw new NotImplementedException();

    public int NumberOfActionBinsPerAxis => throw new NotImplementedException();

    public int[] BehaviorDimensions => throw new NotImplementedException();

    //TODO: Add properties for typing state information
    public IStateInformation GetCopyOfCurrentState()
    {
        throw new NotImplementedException();
    }

    public int[] GetDiscretizedRelationalStateInformation(IStateInformation sourceTaskState, int timeBin = 0)
    {
        throw new NotImplementedException();
    }

    public int[] GetDiscretizedStateInformation()
    {
        throw new NotImplementedException();
    }

    public int[] GetRelationalDimensions(Type type, int numberOfTimeBins = 1)
    {
        throw new NotImplementedException();
    }

    public void UpdateMeasurementSettings(IStateInformation stateInformation)
    {
        throw new NotImplementedException();
    }

    public void UpdateStateInformation(IStateInformation stateInformation)
    {
        throw new NotImplementedException();
    }
}
