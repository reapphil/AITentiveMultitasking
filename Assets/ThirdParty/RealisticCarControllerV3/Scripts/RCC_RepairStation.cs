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
/// Repairs the vehicle in the zone.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Repair Station")]
public class RCC_RepairStation : MonoBehaviour {

    private RCC_CarControllerV3 targetVehicle;      //  Target vehicle in the zone.

    private void OnTriggerEnter(Collider col) {

        //  If trigger enabled, return.
        if (col.isTrigger)
            return;

        //  Get the vehicle in the zone.
        if (targetVehicle == null)
            targetVehicle = col.gameObject.GetComponentInParent<RCC_CarControllerV3>();

        //  And repair if target vehicle found in the zone.
        if (targetVehicle)
            targetVehicle.Repair();

    }

    private void OnTriggerExit(Collider col) {

        //  Setting target vehicle to null if it gets out of the zone.
        if (col.gameObject.GetComponentInParent<RCC_CarControllerV3>())
            targetVehicle = null;

    }

}
