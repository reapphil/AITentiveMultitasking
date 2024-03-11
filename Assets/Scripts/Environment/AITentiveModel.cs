using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

[CreateAssetMenu(fileName = "AITentiveModel", menuName = "ScriptableObjects/AITentiveModel")]
public class AITentiveModel : ScriptableObject
{
    public NNModel Model;

    public SupervisorSettings SupervisorSettings;

    public string Type;

    public int DecisionPeriod;


    public new Type GetType()
    {
        return Util.GetType(Type);
    }
}
