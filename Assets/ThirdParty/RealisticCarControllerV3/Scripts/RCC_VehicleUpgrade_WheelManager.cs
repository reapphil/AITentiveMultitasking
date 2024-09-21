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
/// Manager for upgradable wheels.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Customization/RCC Vehicle Upgrade Wheel Manager")]
public class RCC_VehicleUpgrade_WheelManager : MonoBehaviour {

    //  Mod applier.
    private RCC_CustomizationApplier modApplier;
    public RCC_CustomizationApplier ModApplier {

        get {

            if (modApplier == null)
                modApplier = GetComponentInParent<RCC_CustomizationApplier>();

            return modApplier;

        }

    }

    /// <summary>
    /// Initializing.
    /// </summary>
    public void Initialize() {

        // If last selected wheel found, change the wheels.
        int wheelIndex = ModApplier.loadout.wheel;

        if (wheelIndex != -1)
            RCC_Customization.ChangeWheels(ModApplier.CarController, RCC_ChangableWheels.Instance.wheels[wheelIndex].wheel, true);

    }

    /// <summary>
    /// Changes the wheel with target wheel index.
    /// </summary>
    /// <param name="wheelIndex"></param>
    public void UpdateWheel(int wheelIndex) {

        ModApplier.loadout.wheel = wheelIndex;
        ModApplier.SaveLoadout();
        RCC_Customization.ChangeWheels(ModApplier.CarController, RCC_ChangableWheels.Instance.wheels[wheelIndex].wheel, true);

    }

}
