using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;


/// <summary>
/// Job to calculate the object location probabilities in parallel. index defines the bin for which the probability is calculated. The visitor bins
/// describe the bins for which the index bin can be reached based on the NormalDistributionForVelocity.
/// </summary>
[BurstCompile]
public struct ObjectIn2DGridProbabilitiesUpdateJob : IJobParallelFor
{
    [BurstCompile]
    public struct GameObjectPosition
    {
        public Vector2 position;
        public Vector2 size;

        public bool IsInside(Vector2 point)
        {
            // Check if the point is within the button's area
            return point.x >= position.x && point.x <= position.x + size.x &&
                   point.y >= position.y && point.y <= position.y + size.y;
        }
    }

    [ReadOnly] public NativeArray<Vector2> NormalDistributionForVelocity;

    [ReadOnly] public NativeArray<double> CurrentObjectLocationProbabilities;

    [ReadOnly] public NativeArray<GameObjectPosition> GameObjectPositions;

    public NativeArray<double> ObjectLocationProbabilities;

    public bool IsFullVision;

    public bool IsFocusOnFinger;

    public double ObservationProbability;

    public int IndexActiveObject;

    public int VelocityThreshold;


    public void Execute(int index)
    {
        GameObjectPosition gameObjectPosition = GameObjectPositions[index];

        ObjectLocationProbabilities[index] = 0;

        foreach (Vector2 velocity in NormalDistributionForVelocity)
        {
            Vector2 visitor = gameObjectPosition.position + gameObjectPosition.size/2 - velocity;

            int visitorIndex = -1;
            float minDistance = 99999;

            for (int i = 0; i < GameObjectPositions.Length; i++)
            {
                if (GameObjectPositions[i].IsInside(visitor))
                {
                    visitorIndex = i;
                    break;
                }
                else
                {
                    GameObjectPosition gp = GameObjectPositions[i];
                    float dist = Vector2.Distance(visitor, gp.position + gp.size/2);

                    if (dist < VelocityThreshold && dist < minDistance) 
                    {
                        visitorIndex = i;
                        minDistance = dist;
                    }
                }
            }

            if (visitorIndex != -1)
            {
                ObjectLocationProbabilities[index] = ObjectLocationProbabilities[index] + CurrentObjectLocationProbabilities[visitorIndex] * (1 / (double)NormalDistributionForVelocity.Length);
            }
        }

        //if the current instance is active b`(s_) = O(s_,a,o) SUM(s_e_S){ T(s,a,s_)*b(s)} otherwise b`(s_) = SUM(s_e_S){ T(s,a,s_)*b(s)}
        if ((IsFullVision || IsFocusOnFinger) && (ObjectLocationProbabilities[index] != 0 || index == IndexActiveObject))
        {
            if (ObjectLocationProbabilities[index] == 0)
            {
                Debug.LogWarning($"Location probability of active index {index} is equal to 0.");
                ObjectLocationProbabilities[index] = 0.0001;
            }

            if (index == IndexActiveObject)
            {
                ObjectLocationProbabilities[index] = ObjectLocationProbabilities[index] * ObservationProbability;
            }
            else
            {
                ObjectLocationProbabilities[index] = ObjectLocationProbabilities[index] * (1 - ObservationProbability) / (GameObjectPositions.Length - 1);
            }
        }
    }
}

