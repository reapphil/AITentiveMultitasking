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
/// Used to slow down the vehicle by increasing drag.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Speed Limiter")]
public class RCC_SpeedLimiter : MonoBehaviour {

    private float defaultDrag = -1f;

    private void OnTriggerStay(Collider other) {

        RCC_CarControllerV3 carController = other.GetComponentInParent<RCC_CarControllerV3>();

        if (!carController)
            return;

        if (defaultDrag == -1)
            defaultDrag = carController.Rigid.drag;

        carController.Rigid.drag = .02f * carController.speed;

    }

    private void OnTriggerExit(Collider other) {

        RCC_CarControllerV3 carController = other.GetComponentInParent<RCC_CarControllerV3>();

        if (!carController)
            return;

        carController.Rigid.drag = defaultDrag;

    }

}
