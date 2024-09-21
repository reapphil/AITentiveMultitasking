using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;

/// <summary>
/// Job to calculate the object location probabilities in parallel. index defines the bin for which the probability is calculated. The visitor bins
/// describe the bins for which the index bin can be reached based on the NormalDistributionForVelocity.
/// </summary>
[BurstCompile]
public struct ObjectIn2DRectangleLocationProbabilitiesUpdateJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector2> NormalDistributionForVelocity;

    [ReadOnly] public NativeArray<double> CurrentObjectLocationProbabilities;

    public NativeArray<double> ObjectLocationProbabilities;

    public bool IsVisibleInstance;

    public double ObservationProbability;

    public float RectangleWidth;

    public float RectangleHight;

    public int NumberOFBins;

    public Vector2? ObjectPosition;


    public void Execute(int index)
    {
        if (index < NumberOFBins)
        {
            Vector2 position = PositionConverter.BinToRectangleCoordinates(index, RectangleWidth, RectangleHight, NumberOFBins);

            ObjectLocationProbabilities[index] = 0;

            foreach (Vector2 velocity in NormalDistributionForVelocity)
            {
                if (PositionConverter.IsRectangleEdgeBin(index, RectangleWidth, RectangleHight, NumberOFBins))
                {
                    NativeList<int> crossedBins = PositionConverter.GetRectangleCrossedBins(velocity, index, RectangleWidth, RectangleHight, NumberOFBins);

                    foreach (int crossedBin in crossedBins)
                    {
                        ObjectLocationProbabilities[index] = ObjectLocationProbabilities[index] + CurrentObjectLocationProbabilities[crossedBin] * (1 / ((double)NormalDistributionForVelocity.Length * crossedBins.Length));
                    }
                }
                else
                {
                    Vector2 visitor = position - velocity;
                    int visitorBin = PositionConverter.RectangleCoordinatesToBin(visitor, RectangleWidth, RectangleHight, NumberOFBins);

                    if(visitorBin != -1)
                    {
                        ObjectLocationProbabilities[index] = ObjectLocationProbabilities[index] + CurrentObjectLocationProbabilities[visitorBin] * (1 / (double)NormalDistributionForVelocity.Length);
                    }
                }
            }

            //if the current instance is active b`(s_) = O(s_,a,o) SUM(s_e_S){ T(s,a,s_)*b(s)} otherwise b`(s_) = SUM(s_e_S){ T(s,a,s_)*b(s)}
            if (IsVisibleInstance && ObjectLocationProbabilities[index] != 0 && ObjectPosition != null)
            {
                if (index == PositionConverter.RectangleCoordinatesToBin(ObjectPosition.Value, RectangleWidth, RectangleHight, NumberOFBins))
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
