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

///<summary>
/// API for instantiating, registering new RCC vehicles, and changes at runtime.
///</summary>
public class RCC {

    ///<summary>
    /// Spawn a RCC vehicle prefab with given position, rotation, sets its controllable, and engine state.
    ///</summary>
    public static RCC_CarControllerV3 SpawnRCC(RCC_CarControllerV3 vehiclePrefab, Vector3 position, Quaternion rotation, bool registerAsPlayerVehicle, bool isControllable, bool isEngineRunning) {

        RCC_CarControllerV3 spawnedRCC = (RCC_CarControllerV3)GameObject.Instantiate(vehiclePrefab, position, rotation);
        spawnedRCC.gameObject.SetActive(true);
        spawnedRCC.SetCanControl(isControllable);

        if (registerAsPlayerVehicle)
            RCC_SceneManager.Instance.RegisterPlayer(spawnedRCC);

        if (isEngineRunning)
            spawnedRCC.StartEngine(true);
        else
            spawnedRCC.KillEngine();

        return spawnedRCC;

    }

    ///<summary>
    /// Registers the vehicle as player vehicle. 
    ///</summary>
    public static void RegisterPlayerVehicle(RCC_CarControllerV3 vehicle) {

        RCC_SceneManager.Instance.RegisterPlayer(vehicle);

    }

    ///<summary>
    /// Registers the vehicle as player vehicle with controllable state. 
    ///</summary>
    public static void RegisterPlayerVehicle(RCC_CarControllerV3 vehicle, bool isControllable) {

        RCC_SceneManager.Instance.RegisterPlayer(vehicle, isControllable);

    }

    ///<summary>
    /// Registers the vehicle as player vehicle with controllable and engine state. 
    ///</summary>
    public static void RegisterPlayerVehicle(RCC_CarControllerV3 vehicle, bool isControllable, bool engineState) {

        RCC_SceneManager.Instance.RegisterPlayer(vehicle, isControllable, engineState);

    }

    ///<summary>
    /// De-Registers the player vehicle. 
    ///</summary>
    public static void DeRegisterPlayerVehicle() {

        RCC_SceneManager.Instance.DeRegisterPlayer();

    }

    ///<summary>
    /// Sets controllable state of the vehicle.
    ///</summary>
    public static void SetControl(RCC_CarControllerV3 vehicle, bool isControllable) {

        vehicle.SetCanControl(isControllable);

    }

    ///<summary>
    /// Sets engine state of the vehicle.
    ///</summary>
    public static void SetEngine(RCC_CarControllerV3 vehicle, bool engineState) {

        if (engineState)
            vehicle.StartEngine();
        else
            vehicle.KillEngine();

    }

    ///<summary>
    /// Sets the mobile controller type.
    ///</summary>
    public static void SetMobileController(RCC_Settings.MobileController mobileController) {

        RCC_Settings.Instance.mobileController = mobileController;
        Debug.Log("Mobile Controller has been changed to " + mobileController.ToString());

    }

    ///<summary>
    /// Sets the units.
    ///</summary>
    public static void SetUnits() { }

    ///<summary>
    /// Sets the Automatic Gear.
    ///</summary>
    public static void SetAutomaticGear() { }

    ///<summary>
    /// Starts / stops to record the player vehicle.
    ///</summary>
    public static void StartStopRecord() {

        RCC_SceneManager.Instance.Record();

    }

    ///<summary>
    /// Start / stops replay of the last record.
    ///</summary>
    public static void StartStopReplay() {

        RCC_SceneManager.Instance.Play();

    }

    ///<summary>
    /// Stops record / replay of the last record.
    ///</summary>
    public static void StopRecordReplay() {

        RCC_SceneManager.Instance.Stop();

    }

    ///<summary>
    /// Sets new behavior.
    ///</summary>
    public static void SetBehavior(int behaviorIndex) {

        RCC_SceneManager.Instance.SetBehavior(behaviorIndex);
        Debug.Log("Behavior has been changed to " + behaviorIndex.ToString());

    }

    /// <summary>
    /// Changes the camera mode.
    /// </summary>
    public static void ChangeCamera() {

        RCC_SceneManager.Instance.ChangeCamera();

    }

    /// <summary>
    /// Transport player vehicle the specified position and rotation.
    /// </summary>
    /// <param name="position">Position.</param>
    /// <param name="rotation">Rotation.</param>
    public static void Transport(Vector3 position, Quaternion rotation) {

        RCC_SceneManager.Instance.Transport(position, rotation);

    }

    /// <summary>
    /// Transport the target vehicle to specified position and rotation.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    public static void Transport(RCC_CarControllerV3 vehicle, Vector3 position, Quaternion rotation) {

        RCC_SceneManager.Instance.Transport(vehicle, position, rotation);

    }

    /// <summary>
    /// Cleans all skidmarks on the current scene.
    /// </summary>
    public static void CleanSkidmarks() {

        RCC_SkidmarksManager.Instance.CleanSkidmarks();

    }

    /// <summary>
    /// Cleans target skidmarks on the current scene.
    /// </summary>
    public static void CleanSkidmarks(int index) {

        RCC_SkidmarksManager.Instance.CleanSkidmarks(index);

    }

    /// <summary>
    /// Repairs the target vehicle.
    /// </summary>
    /// <param name="carController"></param>
    public static void Repair(RCC_CarControllerV3 carController) {

        carController.damage.repairNow = true;

    }

    /// <summary>
    /// Repairs the player vehicle.
    /// </summary>
    public static void Repair() {

        RCC_CarControllerV3 carController = RCC_SceneManager.Instance.activePlayerVehicle;

        if (!carController)
            carController.damage.repairNow = true;

    }

}
