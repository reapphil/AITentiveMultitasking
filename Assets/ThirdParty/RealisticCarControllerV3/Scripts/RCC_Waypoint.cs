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
/// Single waypoint for AI.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/AI/RCC Waypoint")]
public class RCC_Waypoint : MonoBehaviour {

    public float targetSpeed = 240f;        //  Target speed of this waypoint.
    public float radius = 20f;      //  Pass radius of this waypoint.

}
