using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;

/// <summary>
/// Job to calculate the Ball location probabilities in parallel. index defines the bin for which the probability is calculated. The visitor bins
/// describe the bins for which the index bin can be reached based on the NormalDistributionForVelocity.
/// </summary>
[BurstCompile]
public struct BallLocationProbabilitiesUpdateJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector3> NormalDistributionForVelocity;

    [ReadOnly] public NativeArray<double> CurrentBallLocationProbabilities;

    public NativeArray<double> BallLocationProbabilities;

    public bool IsVisibleInstance;

    public double ObservationProbability;

    public float PlatformRadius;

    public int NumberOFBinsPerDirection;

    public int NumberOFBins;

    public Vector3 BallPosition;

    public float LocalScaleZ;

    public float LocalScaleX;


    public void Execute(int index)
    {
        if (index < NumberOFBins)
        {
            Vector3 position = PositionConverter.BinToCoordinates(index, PlatformRadius, NumberOFBinsPerDirection, BallPosition.y);

            BallLocationProbabilities[index] = 0;

            foreach (Vector3 velocity in NormalDistributionForVelocity)
            {
                if (PositionConverter.IsEdgeBin(index, NumberOFBinsPerDirection))
                {
                    NativeList<int> crossedBins = PositionConverter.GetCrossedBinsNativeList(velocity, index, NumberOFBinsPerDirection, PlatformRadius);

                    foreach (int crossedBin in crossedBins)
                    {
                        BallLocationProbabilities[index] = BallLocationProbabilities[index] + CurrentBallLocationProbabilities[crossedBin] * (1 / ((double)NormalDistributionForVelocity.Length * crossedBins.Length));
                    }
                }
                else
                {
                    Vector3 visitor = position - velocity;
                    int visitorBin = PositionConverter.CoordinatesToBin(visitor, PlatformRadius, NumberOFBinsPerDirection);

                    if(visitorBin != -1)
                    {
                        BallLocationProbabilities[index] = BallLocationProbabilities[index] + CurrentBallLocationProbabilities[visitorBin] * (1 / (double)NormalDistributionForVelocity.Length);
                    }
                }
            }

            //if the current instance is active b`(s_) = O(s_,a,o) SUM(s_e_S){ T(s,a,s_)*b(s)} otherwise b`(s_) = SUM(s_e_S){ T(s,a,s_)*b(s)}
            if (IsVisibleInstance && BallLocationProbabilities[index] != 0)
            {
                if (index == PositionConverter.CoordinatesToBin(BallPosition, PlatformRadius, NumberOFBinsPerDirection))
                {
                    BallLocationProbabilities[index] = BallLocationProbabilities[index] * ObservationProbability;
                }
                else
                {
                    BallLocationProbabilities[index] = BallLocationProbabilities[index] * (1 - ObservationProbability) / (NumberOFBins - 1);
                }
            }
        }
    }
}
