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
public struct ObjectIn1DLocationProbabilitiesUpdateJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> NormalDistributionForVelocity;

    [ReadOnly] public NativeArray<double> CurrentObjectLocationProbabilities;

    public NativeArray<double> ObjectLocationProbabilities;

    public bool IsVisibleInstance;

    public double ObservationProbability;

    public float RangeMin;

    public float RangeMax;

    public int NumberOFBins;

    public float ObjectPosition;


    public void Execute(int index)
    {
        if (index < NumberOFBins)
        {
            float position = PositionConverter.BinToContinuousValue(index, RangeMin, RangeMax, NumberOFBins);

            ObjectLocationProbabilities[index] = 0;

            foreach (float velocity in NormalDistributionForVelocity)
            {
                if (PositionConverter.IsEdgeBin1D(index, NumberOFBins))
                {
                    NativeList<int> crossedBins = PositionConverter.GetCrossedBins1DNativeList(velocity, index, NumberOFBins, RangeMin, RangeMax);

                    foreach (int crossedBin in crossedBins)
                    {
                        ObjectLocationProbabilities[index] = ObjectLocationProbabilities[index] + CurrentObjectLocationProbabilities[crossedBin] * (1 / ((double)NormalDistributionForVelocity.Length * crossedBins.Length));
                    }
                }
                else
                {
                    float visitor = position - velocity;
                    int visitorBin = PositionConverter.ContinuousValueToBinBurst(visitor, RangeMin, RangeMax, NumberOFBins);

                    if (visitorBin != -1)
                    {
                        ObjectLocationProbabilities[index] = ObjectLocationProbabilities[index] + CurrentObjectLocationProbabilities[visitorBin] * (1 / (double)NormalDistributionForVelocity.Length);
                    }
                }
            }

            //if the current instance is active b`(s_) = O(s_,a,o) SUM(s_e_S){ T(s,a,s_)*b(s)} otherwise b`(s_) = SUM(s_e_S){ T(s,a,s_)*b(s)}
            if (IsVisibleInstance && ObjectLocationProbabilities[index] != 0)
            {
                if (index == PositionConverter.ContinuousValueToBinBurst(ObjectPosition, RangeMin, RangeMax, NumberOFBins))
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
