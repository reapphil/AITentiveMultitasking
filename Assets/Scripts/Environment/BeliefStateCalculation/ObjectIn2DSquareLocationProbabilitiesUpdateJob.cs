using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Job to calculate the object location probabilities in parallel. index defines the bin for which the probability is calculated. The visitor bins
/// describe the bins for which the index bin can be reached based on the NormalDistributionForVelocity.
/// </summary>
[Obsolete("ObjectIn2DSquareLocationProbabilitiesUpdateJob is deprecated, please use ObjectIn2DRectangleLocationProbabilitiesUpdateJob instead.")]
[BurstCompile]
public struct ObjectIn2DSquareLocationProbabilitiesUpdateJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector3> NormalDistributionForVelocity;

    [ReadOnly] public NativeArray<double> CurrentObjectLocationProbabilities;

    public NativeArray<double> ObjectLocationProbabilities;

    public bool IsVisibleInstance;

    public double ObservationProbability;

    public float PlatformRadius;

    public int NumberOFBinsPerDirection;

    public int NumberOFBins;

    public Vector3 ObjectPosition;


    public void Execute(int index)
    {
        if (index < NumberOFBins)
        {
            Vector3 position = PositionConverter.BinToSquareCoordinates(index, PlatformRadius, NumberOFBinsPerDirection, ObjectPosition.y);

            ObjectLocationProbabilities[index] = 0;

            foreach (Vector3 velocity in NormalDistributionForVelocity)
            {
                if (PositionConverter.IsSquareEdgeBin(index, NumberOFBinsPerDirection))
                {
                    NativeList<int> crossedBins = PositionConverter.GetCrossedBins2DNativeList(velocity, index, NumberOFBinsPerDirection, PlatformRadius);

                    foreach (int crossedBin in crossedBins)
                    {
                        ObjectLocationProbabilities[index] = ObjectLocationProbabilities[index] + CurrentObjectLocationProbabilities[crossedBin] * (1 / ((double)NormalDistributionForVelocity.Length * crossedBins.Length));
                    }
                }
                else
                {
                    Vector3 visitor = position - velocity;
                    int visitorBin = PositionConverter.SquareCoordinatesToBin(visitor, PlatformRadius, NumberOFBinsPerDirection);

                    if(visitorBin != -1)
                    {
                        ObjectLocationProbabilities[index] = ObjectLocationProbabilities[index] + CurrentObjectLocationProbabilities[visitorBin] * (1 / (double)NormalDistributionForVelocity.Length);
                    }
                }
            }

            //if the current instance is active b`(s_) = O(s_,a,o) SUM(s_e_S){ T(s,a,s_)*b(s)} otherwise b`(s_) = SUM(s_e_S){ T(s,a,s_)*b(s)}
            if (IsVisibleInstance && ObjectLocationProbabilities[index] != 0)
            {
                if (index == PositionConverter.SquareCoordinatesToBin(ObjectPosition, PlatformRadius, NumberOFBinsPerDirection))
                {
                    ObjectLocationProbabilities[index] = ObjectLocationProbabilities[index] * ObservationProbability;
                }
                else
                {
                    ObjectLocationProbabilities[index] = ObjectLocationProbabilities[index] * (1 - ObservationProbability) / (NumberOFBins - 1);
                }
            }
        }
    }
}
