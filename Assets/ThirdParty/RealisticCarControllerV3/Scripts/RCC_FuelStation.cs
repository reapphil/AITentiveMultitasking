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
/// Fuel station. When a vehicle enters the trigger, fuel tank will be filled up.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Fuel Station")]
public class RCC_FuelStation : MonoBehaviour {

    private RCC_CarControllerV3 targetVehicle;      //  Target vehicle.
    public float refillSpeed = 1f;      //  Refill speed.

    private void OnTriggerStay(Collider col) {

        targetVehicle = col.gameObject.GetComponentInParent<RCC_CarControllerV3>();

        //  If target vehicle is null, return.
        if (!targetVehicle)
            return;

        //  Refill the tank with given speed * time.
        if (targetVehicle)
            targetVehicle.fuelTank += refillSpeed * Time.deltaTime;

    }

    private void OnTriggerExit(Collider col) {

        //  Setting target vehicle to null when vehicle exits the trigger.
        if (col.gameObject.GetComponentInParent<RCC_CarControllerV3>())
            targetVehicle = null;

    }

}
