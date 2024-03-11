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
/// All demo materials.
/// </summary>
public class RCC_DemoMaterials : ScriptableObject {

    #region singleton
    private static RCC_DemoMaterials instance;
    public static RCC_DemoMaterials Instance { get { if (instance == null) instance = Resources.Load("RCC Assets/RCC_DemoMaterials") as RCC_DemoMaterials; return instance; } }
    #endregion

    public Material[] demoMaterials;

}
