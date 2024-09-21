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
/// Upgrades traction strength of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Customization/RCC Vehicle Upgrade Handling")]
public class RCC_VehicleUpgrade_Handling : MonoBehaviour {

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

    private int _handlingLevel = 0;
    public int HandlingLevel {
        get {
            return _handlingLevel;
        }
        set {
            if (value <= 5)
                _handlingLevel = value;
        }
    }

    [HideInInspector] public float defHandling = 0f;
    [Range(.1f, 1f)]public float maxHandling = .4f;

    /// <summary>
    /// Updates handling and initializes it.
    /// </summary>
    public void Initialize() {

        CarController.tractionHelperStrength = Mathf.Lerp(defHandling, maxHandling, HandlingLevel / 5f);

    }

    /// <summary>
    /// Updates handling strength and save it.
    /// </summary>
    public void UpdateStats() {

        CarController.tractionHelperStrength = Mathf.Lerp(defHandling, maxHandling, HandlingLevel / 5f);
        ModApplier.loadout.handlingLevel = HandlingLevel;
        ModApplier.SaveLoadout();

    }

    private void Update() {

        //  Make sure max handling is not smaller.
        if (maxHandling < CarController.tractionHelperStrength)
            maxHandling = CarController.tractionHelperStrength;

    }

    private void Reset() {

        maxHandling = GetComponentInParent<RCC_CarControllerV3>().tractionHelperStrength + .3f;

    }

}
