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
/// Teleports the vehicle in zone to the target spawn point.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Teleporter")]
public class RCC_Teleporter : MonoBehaviour {

    public Transform spawnPoint;        //  Target spawn point.

    private void OnTriggerEnter(Collider col) {

        //  If trigger enabled, return.
        if (col.isTrigger)
            return;

        //  Getting car controller.
        RCC_CarControllerV3 carController = col.gameObject.GetComponentInParent<RCC_CarControllerV3>();

        //  If no car controller found, return.
        if (!carController)
            return;

        //  Transport the vehicle.
        RCC.Transport(carController, spawnPoint.position, spawnPoint.rotation);

    }

}
