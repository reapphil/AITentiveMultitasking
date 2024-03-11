//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------


using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Customization applier for vehicles. Needs to be attached to the vehicle.
/// 5 Upgrade managers for paints, wheels, upgrades, spoilers, and sirens.
/// </summary>
[RequireComponent(typeof(RCC_CarControllerV3))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Customization/RCC Customization Applier")]
public class RCC_CustomizationApplier : MonoBehaviour {

    //  Car controller.
    private RCC_CarControllerV3 _carController;
    public RCC_CarControllerV3 CarController {

        get {

            if (_carController == null)
                _carController = GetComponentInChildren<RCC_CarControllerV3>();

            return _carController;

        }

    }

    #region All upgrade managers

    private RCC_VehicleUpgrade_PaintManager _paintManager;
    public RCC_VehicleUpgrade_PaintManager PaintManager {

        get {

            if (_paintManager == null)
                _paintManager = GetComponentInChildren<RCC_VehicleUpgrade_PaintManager>();

            return _paintManager;

        }

    }

    private RCC_VehicleUpgrade_WheelManager _wheelManager;
    public RCC_VehicleUpgrade_WheelManager WheelManager {

        get {

            if (_wheelManager == null)
                _wheelManager = GetComponentInChildren<RCC_VehicleUpgrade_WheelManager>();

            return _wheelManager;

        }

    }

    private RCC_VehicleUpgrade_UpgradeManager _upgradeManager;
    public RCC_VehicleUpgrade_UpgradeManager UpgradeManager {

        get {

            if (_upgradeManager == null)
                _upgradeManager = GetComponentInChildren<RCC_VehicleUpgrade_UpgradeManager>();

            return _upgradeManager;

        }

    }

    private RCC_VehicleUpgrade_SpoilerManager _spoilerManager;
    public RCC_VehicleUpgrade_SpoilerManager SpoilerManager {

        get {

            if (_spoilerManager == null)
                _spoilerManager = GetComponentInChildren<RCC_VehicleUpgrade_SpoilerManager>();

            return _spoilerManager;

        }

    }

    private RCC_VehicleUpgrade_SirenManager _sirenManager;
    public RCC_VehicleUpgrade_SirenManager SirenManager {

        get {

            if (_sirenManager == null)
                _sirenManager = GetComponentInChildren<RCC_VehicleUpgrade_SirenManager>();

            return _sirenManager;

        }

    }

    #endregion

    public string saveFileName = "";        //  Save file name of the vehicle.
    public bool autoLoadLoadout = true;     //  Loads the latest loadout.
    public RCC_CustomizationLoadout loadout = new RCC_CustomizationLoadout();       //  Loadout class.

    private void OnEnable() {

        //  Loads the latest loadout.
        if (autoLoadLoadout)
            LoadLoadout();

        //  Initializes paint manager.
        if (PaintManager)
            PaintManager.Initialize();

        //  Initializes wheel manager.
        if (WheelManager)
            WheelManager.Initialize();

        //  Initializes upgrade manager.
        if (UpgradeManager)
            UpgradeManager.Initialize();

        //  Initializes spoiler manager.
        if (SpoilerManager)
            SpoilerManager.Initialize();

        //  Initializes siren manager.
        if (SirenManager)
            SirenManager.Initialize();

    }

    /// <summary>
    /// Saves the current loadout with Json.
    /// </summary>
    public void SaveLoadout() {

        PlayerPrefs.SetString(saveFileName, JsonUtility.ToJson(loadout));

    }

    /// <summary>
    /// Loads the latest saved loadout with Json.
    /// </summary>
    public void LoadLoadout() {

        loadout = new RCC_CustomizationLoadout();

        if (PlayerPrefs.HasKey(saveFileName))
            loadout = (RCC_CustomizationLoadout)JsonUtility.FromJson(PlayerPrefs.GetString(saveFileName), typeof(RCC_CustomizationLoadout));

    }

    private void Reset() {

        saveFileName = transform.name;

    }

}
