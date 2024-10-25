using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using UnityEngine;

[Serializable]
public class BallStateInformation : IStateInformation, ISettings
{
    [Measure]
    public float ContinuousActionsX { get; set; }
    [Measure]
    public float ContinuousActionsY { get; set; }
    [Measure]
    public float DragValue { get; set; }
    [Measure]
    public float PlatformAngleX { get; set; }
    [Measure]
    public float PlatformAngleY { get; set; }
    [Measure]
    public float PlatformAngleZ { get; set; }
    [Measure]
    public float BallVelocityX { get; set; }
    [Measure]
    public float BallVelocityY { get; set; }
    [Measure]
    public float BallVelocityZ { get; set; }
    [Measure]
    public float BallPositionX { get; set; }
    [Measure]
    public float BallPositionY { get; set; }
    [Measure]
    public float BallPositionZ { get; set; }

    public static float PlatformRadius { get; set; }

    public int version { get; set; }

    [field: ProjectAssign]
    public int NumberOfDistanceBins_ballPosition { get; set; }

    [field: ProjectAssign]
    public int NumberOfDistanceBins_velocity { get; set; }

    [field: ProjectAssign]
    public int NumberOfDistanceBins_angle { get; set; }

    [field: ProjectAssign]
    public int NumberOfAngleBinsPerAxis { get; set; }

    [field: ProjectAssign]
    public int NumberOfAreaBinsPerDirection { get; set; }

    [field: ProjectAssign]
    public int NumberOfBallVelocityBinsPerAxis { get; set; }

    [field: ProjectAssign]
    public int NumberOfActionBinsPerAxis { get; set; }

    //result of experiment: (3.32, 1.47, 3.52) (only valid for the standard parameters of the platform size, etc.)
    public static Vector3 VelocityRangeMax { private set; get; } = new Vector3(4f, 2f, 4f);

    //result of experiment: (-3.45, -10.00, -3.51) (only valid for the standard parameters of the platform size, etc.)
    public static Vector3 VelocityRangeMin { private set; get; } = new Vector3(-4f, -11f, -4f);

    public static Vector3 AngleRangeMax { private set; get; } = new Vector3(35f, 10f, 35f);

    public static Vector3 AngleRangeMin { private set; get; } = new Vector3(-35f, -10f, -35f);

    public int[] BehaviorDimensions { get => new int[] { (int)Math.Pow(NumberOfAreaBinsPerDirection, 2), (int)Math.Pow(NumberOfAngleBinsPerAxis, 3), (int)Math.Pow(NumberOfBallVelocityBinsPerAxis, 3)}; }
    
    public Array AveragePerformedActionsDiscretizedSpace { get; set; }

    public Dictionary<Type, Array> AverageReactionTimesDiscretizedSpace { get; set; }

    //action range is between 1 and -1
    public Vector3 ActionRangeMax { private set; get; } = new Vector3(1, 0, 1);

    public Vector3 ActionRangeMin { private set; get; } = new Vector3(-1, 0, -1);

    public List<dynamic> PerformedActions { 
        get 
        {
            return new List<dynamic> { new Vector3(ContinuousActionsX, 0, ContinuousActionsY) };
        } 
    }

    public BallStateInformation()
    {

    }

    public BallStateInformation(float continuousActionsX, float continuousActionsY, float ballPositionX, float ballPositionY, float ballPositionZ, float ballVelocityX, float ballVelocityY, float ballVelocityZ, float platformAngleX, float platformAngleY, float platformAngleZ)
    {
        ContinuousActionsX = continuousActionsX;
        ContinuousActionsY = continuousActionsY;
        BallPositionX = ballPositionX;
        BallPositionY = ballPositionY;
        BallPositionZ = ballPositionZ;
        BallVelocityX = ballVelocityX;
        BallVelocityY = ballVelocityY;
        BallVelocityZ = ballVelocityZ;
        PlatformAngleX = platformAngleX;
        PlatformAngleY = platformAngleY;
        PlatformAngleZ = platformAngleZ;
    }

    public int[] GetRelationalDimensions(Type type, int numberOfTimeBins = 1)
    {
        return type switch
        {
            Type t when t == typeof(BallStateInformation) => new int[] { numberOfTimeBins, NumberOfDistanceBins_ballPosition, NumberOfDistanceBins_angle, NumberOfDistanceBins_velocity },
            _ => throw new NotImplementedException(),
        };
    }

    public int[] GetDiscretizedRelationalStateInformation(IStateInformation sourceTaskState, int timeBin = 0)
    {
        return sourceTaskState.GetType() switch
        {
            Type t when t == typeof(BallStateInformation) => GetDiscretizedRelationalStateInformation(sourceTaskState as BallStateInformation, timeBin),
            _ => throw new NotImplementedException(),
        };
    }

    public int[] GetDiscretizedStateInformation()
    {
        PlatformAngleX = PlatformAngleX > 300 ? PlatformAngleX - 360 : PlatformAngleX;
        PlatformAngleY = PlatformAngleY > 300 ? PlatformAngleY - 360 : PlatformAngleY;
        PlatformAngleZ = PlatformAngleZ > 300 ? PlatformAngleZ - 360 : PlatformAngleZ;

        Vector3 velocityRangeVector = new Vector3(Math.Abs(VelocityRangeMax.x - VelocityRangeMin.x),
                          Math.Abs(VelocityRangeMax.y - VelocityRangeMin.y),
                          Math.Abs(VelocityRangeMax.z - VelocityRangeMin.z));

        Vector3 angleRangeVector = new Vector3(Math.Abs(AngleRangeMax.x - AngleRangeMin.x),
                          Math.Abs(AngleRangeMax.y - AngleRangeMin.y),
                          Math.Abs(AngleRangeMax.z - AngleRangeMin.z));

        int ballBin = PositionConverter.SquareCoordinatesToBin(new Vector3(BallPositionX, BallPositionY, BallPositionZ), PlatformRadius + 0.1f, NumberOfAreaBinsPerDirection); //add 0.1f in case the ball is beyond the edge
        int velocityBin = PositionConverter.RangeVectorToBin(new Vector3(BallVelocityX, BallVelocityY, BallVelocityZ), velocityRangeVector, NumberOfBallVelocityBinsPerAxis, VelocityRangeMin);
        int angleBin = PositionConverter.RangeVectorToBin(new Vector3(PlatformAngleX, PlatformAngleY, PlatformAngleZ), angleRangeVector, NumberOfAngleBinsPerAxis, AngleRangeMin);

        return new int[] { ballBin, angleBin, velocityBin };
    }

    public void UpdateStateInformation(IStateInformation stateInformation)
    {
        BallStateInformation ballStateInformation = stateInformation as BallStateInformation;

        if (ballStateInformation != null)
        {
            ContinuousActionsX = ballStateInformation.ContinuousActionsX;
            ContinuousActionsY = ballStateInformation.ContinuousActionsY;
            BallPositionX = ballStateInformation.BallPositionX;
            BallPositionY = ballStateInformation.BallPositionY;
            BallPositionZ = ballStateInformation.BallPositionZ;
            BallVelocityX = ballStateInformation.BallVelocityX;
            BallVelocityY = ballStateInformation.BallVelocityY;
            BallVelocityZ = ballStateInformation.BallVelocityZ;
            PlatformAngleX = ballStateInformation.PlatformAngleX;
            PlatformAngleY = ballStateInformation.PlatformAngleY;
            PlatformAngleZ = ballStateInformation.PlatformAngleZ;
            DragValue = ballStateInformation.DragValue;
        }
    }

    public void UpdateMeasurementSettings(IStateInformation stateInformation)
    {
        BallStateInformation ballStateInformation = stateInformation as BallStateInformation;

        NumberOfDistanceBins_ballPosition = ballStateInformation.NumberOfDistanceBins_ballPosition;
        NumberOfDistanceBins_velocity = ballStateInformation.NumberOfDistanceBins_velocity;
        NumberOfDistanceBins_angle = ballStateInformation.NumberOfDistanceBins_angle;
        NumberOfAngleBinsPerAxis = ballStateInformation.NumberOfAngleBinsPerAxis;
        NumberOfAreaBinsPerDirection = ballStateInformation.NumberOfAreaBinsPerDirection;
        NumberOfBallVelocityBinsPerAxis = ballStateInformation.NumberOfBallVelocityBinsPerAxis;
        NumberOfActionBinsPerAxis = ballStateInformation.NumberOfActionBinsPerAxis;
    }

    public IStateInformation GetCopyOfCurrentState()
    {
        BallStateInformation copy = new();
        copy.UpdateStateInformation(this);

        return copy;
    }


    private int[] GetDiscretizedRelationalStateInformation(BallStateInformation sourceTaskState, int timeBin = 0)
    {
        PlatformAngleX = PlatformAngleX > 300 ? PlatformAngleX - 360 : PlatformAngleX;
        PlatformAngleY = PlatformAngleY > 300 ? PlatformAngleY - 360 : PlatformAngleY;
        PlatformAngleZ = PlatformAngleZ > 300 ? PlatformAngleZ - 360 : PlatformAngleZ;

        int distanceBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(new Vector3(BallPositionX, BallPositionY, BallPositionZ), new Vector3(sourceTaskState.BallPositionX, sourceTaskState.BallPositionY, sourceTaskState.BallPositionZ)),
                                                                 PlatformRadius * 2,  //scale of platform
                                                                 NumberOfDistanceBins_ballPosition);
        int velocityBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(new Vector3(BallVelocityX, BallVelocityY, BallVelocityZ), new Vector3(sourceTaskState.BallVelocityX, sourceTaskState.BallVelocityY, sourceTaskState.BallVelocityZ)),
                                                                 Vector3.Distance(VelocityRangeMin, VelocityRangeMax),
                                                                 NumberOfDistanceBins_velocity);
        int angleBinDistance = PositionConverter.ContinuousValueToBin(Vector3.Distance(new Vector3(PlatformAngleX, PlatformAngleY, PlatformAngleZ), new Vector3(sourceTaskState.PlatformAngleX, sourceTaskState.PlatformAngleY, sourceTaskState.PlatformAngleZ)),
                                                                      Vector3.Distance(AngleRangeMin, AngleRangeMax),
                                                                      NumberOfDistanceBins_angle);

        return new int[] { timeBin, distanceBin, angleBinDistance, velocityBin };
    }

    public bool ActionIsInUsualRange(List<dynamic> currentAverageActionBehavioralData, List<dynamic> performedAction)
    {
        Vector3 actionRangeVector = new Vector3(Math.Abs(ActionRangeMax.x - ActionRangeMin.x),
                  Math.Abs(ActionRangeMax.y - ActionRangeMin.y),
                  Math.Abs(ActionRangeMax.z - ActionRangeMin.z));

        int averageActionBinBehavioralData = PositionConverter.RangeVectorToBin(currentAverageActionBehavioralData[0], actionRangeVector, NumberOfActionBinsPerAxis, ActionRangeMin);
        int currentActionBinBehavioralData = PositionConverter.RangeVectorToBin(performedAction[0], actionRangeVector, NumberOfActionBinsPerAxis, ActionRangeMin);

        return averageActionBinBehavioralData == currentActionBinBehavioralData;
    }
}
