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
/// Upgrades engine of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Customization/RCC Vehicle Upgrade Engine")]
public class RCC_VehicleUpgrade_Engine : MonoBehaviour {

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

    private int _engineLevel = 0;
    public int EngineLevel {
        get {
            return _engineLevel;
        }
        set {
            if (value <= 5)
                _engineLevel = value;
        }
    }

    [HideInInspector] public float defEngine = 0f;
    [Range(200, 1000)] public float maxEngine = 750f;

    /// <summary>
    /// Updates engine torque and initializes it.
    /// </summary>
    public void Initialize() {

        CarController.maxEngineTorque = Mathf.Lerp(defEngine, maxEngine, EngineLevel / 5f);

    }

    /// <summary>
    /// Updates engine torque and save it.
    /// </summary>
    public void UpdateStats() {

        CarController.maxEngineTorque = Mathf.Lerp(defEngine, maxEngine, EngineLevel / 5f);
        ModApplier.loadout.engineLevel = EngineLevel;
        ModApplier.SaveLoadout();

    }

    private void Update() {

        //  Make sure max torque is not smaller.
        if (maxEngine < CarController.maxEngineTorque)
            maxEngine = CarController.maxEngineTorque;

    }

    private void Reset() {

        maxEngine = GetComponentInParent<RCC_CarControllerV3>().maxEngineTorque + 200f;

    }

}
