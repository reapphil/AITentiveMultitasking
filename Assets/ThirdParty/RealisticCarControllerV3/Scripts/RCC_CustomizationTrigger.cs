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
/// Customization trigger used on customization demo scene. It will enable customization mode when player vehicle triggers.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Customization/RCC Customization Trigger")]
public class RCC_CustomizationTrigger : MonoBehaviour {

    public GameObject trigger;      //  Trigger object.
    private RCC_CarControllerV3 vehicle;        //  Current vehicle.

    private void OnTriggerEnter(Collider other) {

        //  Getting car controller.
        RCC_CarControllerV3 carController = other.GetComponentInParent<RCC_CarControllerV3>();

        //  If trigger is not a vehicle, return.
        if (!carController)
            return;

        //  Enable customization mode, disable trigger.
        RCC_CustomizationDemo.Instance.EnableCustomization(carController);
        trigger.SetActive(false);
        vehicle = carController;

    }

    private void Update() {

        //  If no any vehicle triggered, return.
        if (!vehicle || trigger.activeSelf)
            return;

        //  Id distance is higher than 30 meters, reenable the trigger again.
        if (Vector3.Distance(transform.position, vehicle.transform.position) > 30f) {

            trigger.SetActive(true);
            vehicle = null;

        }

    }

}
