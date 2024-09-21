using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Jobs;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Assertions;

public class Ball3DAgentHumanCognitionSingleProbabilityDistribution : Ball3DAgentHumanCognition
{
    private readonly object _ballLocationProbabilitiesInitializationLock = new();
    
    private static double[] s_initBallLocationProbabilities;

    private static double[] s_ballLocationProbabilities;


    public override void Initialize()
    {
        lock (_ballLocationProbabilitiesInitializationLock)
        {
            if (s_ballLocationProbabilities is null)
            {
                s_initBallLocationProbabilities = new double[_numberOfBins];
                s_ballLocationProbabilities = new double[_numberOfBins];
            }
        }

        base._ballLocationProbabilities = s_ballLocationProbabilities;

        int numberOFBinsPerDirection = (int)Math.Sqrt(_numberOfBins);
        
        for (int i = 0; i < _numberOfBins; i++)
        {
            if (PositionConverter.SquareCoordinatesToBin(BallStartingPosition, _platformRadius, numberOFBinsPerDirection) == i)
            {
                s_initBallLocationProbabilities[i] = 1;
            }
            else
            {
                s_initBallLocationProbabilities[i] = 0;
            }
        }

        base.Initialize();
    }


    protected override void InitializeBallLocationProbabilities(int numberOFBins, float platformRadius)
    {
        lock (_ballLocationProbabilitiesInitializationLock)
        {
            Array.Copy(s_initBallLocationProbabilities, s_ballLocationProbabilities, numberOFBins);
        }
    }
}
