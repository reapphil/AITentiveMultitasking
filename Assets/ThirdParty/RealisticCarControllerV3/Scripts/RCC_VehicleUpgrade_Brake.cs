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
/// Upgrades brake torque of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Customization/RCC Vehicle Upgrade Brake")]
public class RCC_VehicleUpgrade_Brake : MonoBehaviour {

    private RCC_CarControllerV3 _carController;
    public RCC_CarControllerV3 CarController {

        get {

            if (_carController == null)
                _carController = GetComponentInParent<RCC_CarControllerV3>();

            return _carController;

        }

    }

    private RCC_CustomizationApplier modApplier;
    public RCC_CustomizationApplier ModApplier {

        get {

            if (modApplier == null)
                modApplier = GetComponentInParent<RCC_CustomizationApplier>();

            return modApplier;

        }

    }

    private int _brakeLevel = 0;
    public int BrakeLevel {
        get {
            return _brakeLevel;
        }
        set {
            if (value <= 5)
                _brakeLevel = value;
        }
    }

    [HideInInspector] public float defBrake = 0f;
    [Range(2000, 5000)]public float maxBrake = 4000f;

    /// <summary>
    /// Updates brake torque and initializes it.
    /// </summary>
    public void Initialize() {

        CarController.brakeTorque = Mathf.Lerp(defBrake, maxBrake, BrakeLevel / 5f);

    }

    /// <summary>
    /// Updates brake torque and save it.
    /// </summary>
    public void UpdateStats() {

        CarController.brakeTorque = Mathf.Lerp(defBrake, maxBrake, BrakeLevel / 5f);
        ModApplier.loadout.brakeLevel = BrakeLevel;
        ModApplier.SaveLoadout();

    }

    private void Update() {

        //  Make sure max brake is not smaller.
        if (maxBrake < CarController.brakeTorque)
            maxBrake = CarController.brakeTorque;

    }

    private void Reset() {

        maxBrake = GetComponentInParent<RCC_CarControllerV3>().brakeTorque + 1000f;

    }

}
