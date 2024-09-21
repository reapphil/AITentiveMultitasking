//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All demo vehicles.
/// </summary>
public class RCC_DemoVehicles : ScriptableObject {

    public RCC_CarControllerV3[] vehicles;

    #region singleton
    private static RCC_DemoVehicles instance;
    public static RCC_DemoVehicles Instance { get { if (instance == null) instance = Resources.Load("RCC Assets/RCC_DemoVehicles") as RCC_DemoVehicles; return instance; } }
    #endregion

}
