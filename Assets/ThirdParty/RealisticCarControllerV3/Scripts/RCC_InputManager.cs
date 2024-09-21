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
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Main input class of the RCC. Used by RCC_CarControllerV3, RCC_Camera, RCC_MobileButtons.
/// </summary>
public class RCC_InputManager : RCC_Singleton<RCC_InputManager> {

    public RCC_Inputs inputs = new RCC_Inputs();
    private static RCC_InputActions inputActions;

    public bool gyroUsed = false;
    public bool logitechSteeringUsed = false;
    public bool logitechHShifterUsed = false;
    public int logitechGear = -2;

    public delegate void onStartStopEngine();
    public static event onStartStopEngine OnStartStopEngine;

    public delegate void onLowBeamHeadlights();
    public static event onLowBeamHeadlights OnLowBeamHeadlights;

    public delegate void onHighBeamHeadlights();
    public static event onHighBeamHeadlights OnHighBeamHeadlights;

    public delegate void onChangeCamera();
    public static event onChangeCamera OnChangeCamera;

    public delegate void onIndicatorLeft();
    public static event onIndicatorLeft OnIndicatorLeft;

    public delegate void onIndicatorRight();
    public static event onIndicatorRight OnIndicatorRight;

    public delegate void onIndicatorHazard();
    public static event onIndicatorHazard OnIndicatorHazard;

    public delegate void onGearShiftUp();
    public static event onGearShiftUp OnGearShiftUp;

    public delegate void onGearShiftDown();
    public static event onGearShiftDown OnGearShiftDown;

    public delegate void onNGear(bool state);
    public static event onNGear OnNGear;

    public delegate void onSlowMotion(bool state);
    public static event onSlowMotion OnSlowMotion;

    public delegate void onRecord();
    public static event onRecord OnRecord;

    public delegate void onReplay();
    public static event onReplay OnReplay;

    public delegate void onLookBack(bool state);
    public static event onLookBack OnLookBack;

    public delegate void onTrailerDetach();
    public static event onTrailerDetach OnTrailerDetach;

    private void Awake() {

        gameObject.hideFlags = HideFlags.HideInHierarchy;

        //  Creating inputs.
        inputs = new RCC_Inputs();

    }

    private void Update() {

        //  Creating inputs.
        if (inputs == null)
            inputs = new RCC_Inputs();

        //  Receive inputs from the controller.
        GetInputs();

    }

    /// <summary>
    /// Gets all inputs and registers button events.
    /// </summary>
    /// <returns></returns>
    public void GetInputs() {

        if (inputActions == null) {

            inputActions = new RCC_InputActions();
            inputActions.Enable();

            inputActions.Vehicle.StartStopEngine.performed += StartStopEngine_performed;
            inputActions.Vehicle.LowBeamLights.performed += LowBeamLights_performed;
            inputActions.Vehicle.HighBeamLights.performed += HighBeamLights_performed;
            inputActions.Camera.ChangeCamera.performed += ChangeCamera_performed;
            inputActions.Vehicle.IndicatorLeft.performed += IndicatorLeft_performed;
            inputActions.Vehicle.IndicatorRight.performed += IndicatorRight_performed;
            inputActions.Vehicle.IndicatorHazard.performed += IndicatorHazard_performed;
            inputActions.Vehicle.GearShiftUp.performed += GearShiftUp_performed;
            inputActions.Vehicle.GearShiftDown.performed += GearShiftDown_performed;
            inputActions.Vehicle.NGear.performed += NGear_performed;
            inputActions.Vehicle.NGear.canceled += NGear_canceled;
            inputActions.Optional.SlowMotion.performed += SlowMotion_performed;
            inputActions.Optional.SlowMotion.canceled += SlowMotion_canceled;
            inputActions.Optional.Record.performed += Record_performed;
            inputActions.Optional.Replay.performed += Replay_performed;
            inputActions.Camera.LookBack.performed += LookBack_performed;
            inputActions.Camera.LookBack.canceled += LookBack_canceled;
            inputActions.Vehicle.TrailerDetach.performed += TrailerDetach_performed;

#if RCC_LOGITECH
            //	LOGITECH STEERING WHEEL INPUTS
            inputActions.Vehicle._1stGear.performed += _1stGear_performed;
            inputActions.Vehicle._2ndGear.performed += _2ndGear_performed;
            inputActions.Vehicle._3rdGear.performed += _3rdGear_performed;
            inputActions.Vehicle._4thGear.performed += _4thGear_performed;
            inputActions.Vehicle._5thGear.performed += _5thGear_performed;
            inputActions.Vehicle._6thGear.performed += _6thGear_performed;
            inputActions.Vehicle.RGear.performed += _RGear_performed;

            inputActions.Vehicle._1stGear.canceled += _Gear_canceled;
            inputActions.Vehicle._2ndGear.canceled += _Gear_canceled;
            inputActions.Vehicle._3rdGear.canceled += _Gear_canceled;
            inputActions.Vehicle._4thGear.canceled += _Gear_canceled;
            inputActions.Vehicle._5thGear.canceled += _Gear_canceled;
            inputActions.Vehicle._6thGear.canceled += _Gear_canceled;
            inputActions.Vehicle.RGear.canceled += _Gear_canceled;
#endif

        }

        if (!RCC_Settings.Instance.mobileControllerEnabled) {

            inputs.throttleInput = inputActions.Vehicle.Throttle.ReadValue<float>();
            inputs.brakeInput = inputActions.Vehicle.Brake.ReadValue<float>();
            inputs.steerInput = inputActions.Vehicle.Steering.ReadValue<float>();
            inputs.handbrakeInput = inputActions.Vehicle.Handbrake.ReadValue<float>();
            inputs.boostInput = inputActions.Vehicle.NOS.ReadValue<float>();
            inputs.clutchInput = inputActions.Vehicle.Clutch.ReadValue<float>();
            inputs.gearInput = logitechGear;
            inputs.orbitX = inputActions.Camera.Orbit.ReadValue<Vector2>().x;
            inputs.orbitY = inputActions.Camera.Orbit.ReadValue<Vector2>().y;
            inputs.scroll = inputActions.Camera.Zoom.ReadValue<Vector2>();

        } else {

            inputs.throttleInput = RCC_MobileButtons.mobileInputs.throttleInput;
            inputs.brakeInput = RCC_MobileButtons.mobileInputs.brakeInput;
            inputs.steerInput = RCC_MobileButtons.mobileInputs.steerInput;
            inputs.handbrakeInput = RCC_MobileButtons.mobileInputs.handbrakeInput;
            inputs.boostInput = RCC_MobileButtons.mobileInputs.boostInput;

        }

    }

#if RCC_LOGITECH
    //	LOGITECH H SHIFTER INPUTS
    private void _1stGear_performed(InputAction.CallbackContext obj) {

        logitechHShifterUsed = true;
        logitechGear = 0;

    }

    private void _Gear_canceled(InputAction.CallbackContext obj) {

        logitechGear = -2;

    }

    private void _2ndGear_performed(InputAction.CallbackContext obj) {

        logitechHShifterUsed = true;
        logitechGear = 1;

    }

    private void _3rdGear_performed(InputAction.CallbackContext obj) {

        logitechHShifterUsed = true;
        logitechGear = 2;

    }

    private void _4thGear_performed(InputAction.CallbackContext obj) {

        logitechHShifterUsed = true;
        logitechGear = 3;

    }

    private void _5thGear_performed(InputAction.CallbackContext obj) {

        logitechHShifterUsed = true;
        logitechGear = 4;

    }

    private void _6thGear_performed(InputAction.CallbackContext obj) {

        logitechHShifterUsed = true;
        logitechGear = 5;

    }

    private void _RGear_performed(InputAction.CallbackContext obj) {

        logitechHShifterUsed = true;
        logitechGear = -1;

    }
#endif
    private void StartStopEngine_performed(InputAction.CallbackContext obj) {

        if (OnStartStopEngine != null)
            OnStartStopEngine();

    }

    private void TrailerDetach_performed(InputAction.CallbackContext obj) {

        if (OnTrailerDetach != null)
            OnTrailerDetach();

    }

    private void LookBack_performed(InputAction.CallbackContext obj) {

        if (OnLookBack != null)
            OnLookBack(true);

    }

    private void LookBack_canceled(InputAction.CallbackContext obj) {

        if (OnLookBack != null)
            OnLookBack(false);

    }

    private static void Replay_performed(InputAction.CallbackContext obj) {

        if (OnReplay != null)
            OnReplay();

    }

    private static void Record_performed(InputAction.CallbackContext obj) {

        if (OnRecord != null)
            OnRecord();

    }

    private static void SlowMotion_performed(InputAction.CallbackContext obj) {

        if (OnSlowMotion != null)
            OnSlowMotion(true);

    }

    private static void SlowMotion_canceled(InputAction.CallbackContext obj) {

        if (OnSlowMotion != null)
            OnSlowMotion(false);

    }

    private static void NGear_performed(InputAction.CallbackContext obj) {

        if (OnNGear != null)
            OnNGear(true);

    }

    private static void NGear_canceled(InputAction.CallbackContext obj) {

        if (OnNGear != null)
            OnNGear(false);

    }

    private static void GearShiftDown_performed(InputAction.CallbackContext obj) {

        if (OnGearShiftDown != null)
            OnGearShiftDown();

    }

    private static void GearShiftUp_performed(InputAction.CallbackContext obj) {

        if (OnGearShiftUp != null)
            OnGearShiftUp();

    }

    private static void IndicatorHazard_performed(InputAction.CallbackContext obj) {

        if (OnIndicatorHazard != null)
            OnIndicatorHazard();

    }

    private static void IndicatorRight_performed(InputAction.CallbackContext obj) {

        if (OnIndicatorRight != null)
            OnIndicatorRight();

    }

    private static void IndicatorLeft_performed(InputAction.CallbackContext obj) {

        if (OnIndicatorLeft != null)
            OnIndicatorLeft();

    }

    private static void ChangeCamera_performed(InputAction.CallbackContext obj) {

        if (OnChangeCamera != null)
            OnChangeCamera();

    }

    private static void HighBeamLights_performed(InputAction.CallbackContext obj) {

        if (OnHighBeamHeadlights != null)
            OnHighBeamHeadlights();

    }

    private static void LowBeamLights_performed(InputAction.CallbackContext obj) {

        if (OnLowBeamHeadlights != null)
            OnLowBeamHeadlights();

    }

}
