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
using UnityEngine.SceneManagement;

/// <summary>
/// A simple manager script for all demo scenes. It has an array of spawnable player vehicles, public methods, setting new behavior modes, restart, and quit application.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/RCC Demo Manager")]
public class RCC_Demo : MonoBehaviour {

    [HideInInspector] public int selectedVehicleIndex = 0;      // An integer index value used to spawn a new vehicle.
    [HideInInspector] public int selectedBehaviorIndex = 0;     // An integer index value used to set a new behavior mode.

    /// <summary>
    /// An integer index value used for spawning a new vehicle.
    /// </summary>
    /// <param name="index"></param>
    public void SelectVehicle(int index) {

        selectedVehicleIndex = index;

    }

    /// <summary>
    /// Spawns the player vehicle.
    /// </summary>
    public void Spawn() {

        // Last known position and rotation of last active vehicle.
        Vector3 lastKnownPos = new Vector3();
        Quaternion lastKnownRot = new Quaternion();

        // Checking if there is a player vehicle on the scene.
        if (RCC_SceneManager.Instance.activePlayerVehicle) {

            lastKnownPos = RCC_SceneManager.Instance.activePlayerVehicle.transform.position;
            lastKnownRot = RCC_SceneManager.Instance.activePlayerVehicle.transform.rotation;

        }

        // If last known position and rotation is not assigned, camera's position and rotation will be used.
        if (lastKnownPos == Vector3.zero) {

            if (RCC_SceneManager.Instance.activePlayerCamera) {

                lastKnownPos = RCC_SceneManager.Instance.activePlayerCamera.transform.position;
                lastKnownRot = RCC_SceneManager.Instance.activePlayerCamera.transform.rotation;

            }

        }

        // We don't need X and Z rotation angle. Just Y.
        lastKnownRot.x = 0f;
        lastKnownRot.z = 0f;

        // Is there any last vehicle?
        RCC_CarControllerV3 lastVehicle = RCC_SceneManager.Instance.activePlayerVehicle;

#if BCG_ENTEREXIT

        BCG_EnterExitVehicle lastEnterExitVehicle;
        bool enterExitVehicleFound = false;

        if (lastVehicle) {

            lastEnterExitVehicle = lastVehicle.GetComponentInChildren<BCG_EnterExitVehicle>();

            if (lastEnterExitVehicle && lastEnterExitVehicle.driver) {

                enterExitVehicleFound = true;
                BCG_EnterExitManager.Instance.waitTime = 10f;
                lastEnterExitVehicle.driver.GetOut();

            }

        }

#endif

        // If we have controllable vehicle by player on scene, destroy it.
        if (lastVehicle)
            Destroy(lastVehicle.gameObject);

        // Here we are creating our new vehicle.
        RCC.SpawnRCC(RCC_DemoVehicles.Instance.vehicles[selectedVehicleIndex], lastKnownPos, lastKnownRot, true, true, true);

#if BCG_ENTEREXIT

        if (enterExitVehicleFound) {

            lastEnterExitVehicle = null;

            lastEnterExitVehicle = RCC_SceneManager.Instance.activePlayerVehicle.GetComponentInChildren<BCG_EnterExitVehicle>();

            if (!lastEnterExitVehicle)
                lastEnterExitVehicle = RCC_SceneManager.Instance.activePlayerVehicle.gameObject.AddComponent<BCG_EnterExitVehicle>();

            if (BCG_EnterExitManager.Instance.activePlayer && lastEnterExitVehicle && lastEnterExitVehicle.driver == null) {

                BCG_EnterExitManager.Instance.waitTime = 10f;
                BCG_EnterExitManager.Instance.activePlayer.GetIn(lastEnterExitVehicle);

            }

        }

#endif

    }

    /// <summary>
    /// An integer index value used for setting behavior mode.
    /// </summary>
    /// <param name="index"></param>
    public void SetBehavior(int index) {

        selectedBehaviorIndex = index;

    }

    /// <summary>
    /// Here we are setting new selected behavior to corresponding one.
    /// </summary>
    public void InitBehavior() {

        RCC.SetBehavior(selectedBehaviorIndex);

    }

    /// <summary>
    /// Sets the mobile controller type.
    /// </summary>
    /// <param name="index"></param>
    public void SetMobileController(int index) {

        switch (index) {

            case 0:
                RCC.SetMobileController(RCC_Settings.MobileController.TouchScreen);
                break;
            case 1:
                RCC.SetMobileController(RCC_Settings.MobileController.Gyro);
                break;
            case 2:
                RCC.SetMobileController(RCC_Settings.MobileController.SteeringWheel);
                break;
            case 3:
                RCC.SetMobileController(RCC_Settings.MobileController.Joystick);
                break;

        }

    }

    /// <summary>
    /// Sets the quality.
    /// </summary>
    /// <param name="index">Index.</param>
    public void SetQuality(int index) {

        QualitySettings.SetQualityLevel(index);

    }

    /// <summary>
    /// Simply restarting the current scene.
    /// </summary>
    public void RestartScene() {

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }

    /// <summary>
    /// Simply quit application. Not working on Editor.
    /// </summary>
    public void Quit() {

        Application.Quit();

    }

}
