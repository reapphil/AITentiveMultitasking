//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Receiving inputs from UI buttons, and feeds active vehicles on your scene.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/Mobile/RCC UI Mobile Buttons")]
public class RCC_MobileButtons : RCC_Core {

    //  All buttons
    [Header("Controller Buttons")]
    public RCC_UIController gasButton;
    public RCC_UIController gradualGasButton;
    public RCC_UIController brakeButton;
    public RCC_UIController leftButton;
    public RCC_UIController rightButton;
    public RCC_UIController handbrakeButton;
    public RCC_UIController NOSButton;
    public RCC_UIController NOSButtonSteeringWheel;
    //public GameObject gearButton;

    public bool useGradualThrottle = false;     //  Use gradual throttle or not.
    private bool lastUseGradualThrottle = false;        //  Used to update the buttons.

    // Steering wheel.
    [Header("Steering Wheel")]
    public RCC_UISteeringWheelController steeringWheel;

    //  Joystick.
    [Header("Joystick")]
    public RCC_UIJoystick joystick;

    //  Mobile inputs.
    public static RCC_Inputs mobileInputs = new RCC_Inputs();

    //  Inputs.
    private float throttleInput = 0f;
    private float brakeInput = 0f;
    private float leftInput = 0f;
    private float rightInput = 0f;
    private float steeringWheelInput = 0f;
    private float handbrakeInput = 0f;
    private float boostInput = 1f;
    private float gyroInput = 0f;
    private float joystickInput = 0f;
    private bool canUseNos = false;

    private Vector3 orgBrakeButtonPos = Vector3.zero;

    private void Start() {

        //  If brake button is selected, take original position of the button.
        if (brakeButton)
            orgBrakeButtonPos = brakeButton.transform.position;

        //  Checking mobile buttons. Enabling or disabling them.
        CheckMobileButtons();

    }

    private void OnEnable() {

        RCC_SceneManager.OnVehicleChanged += CheckMobileButtons;

    }

    /// <summary>
    /// Checking mobile buttons. Enabling or disabling them.
    /// </summary>
    private void CheckMobileButtons() {

        // If mobile controllers are enabled, enable mobile buttons. Disable otherwise.
        if (RCC_Settings.Instance.mobileControllerEnabled)
            EnableButtons();
        else
            DisableButtons();

    }

    /// <summary>
    /// Disables all mobile buttons.
    /// </summary>
    private void DisableButtons() {

        if (gasButton && gasButton.gameObject.activeSelf)
            gasButton.gameObject.SetActive(false);

        if (gradualGasButton && gradualGasButton.gameObject.activeSelf)
            gradualGasButton.gameObject.SetActive(false);

        if (leftButton && leftButton.gameObject.activeSelf)
            leftButton.gameObject.SetActive(false);

        if (rightButton && rightButton.gameObject.activeSelf)
            rightButton.gameObject.SetActive(false);

        if (brakeButton && brakeButton.gameObject.activeSelf)
            brakeButton.gameObject.SetActive(false);

        if (steeringWheel && steeringWheel.gameObject.activeSelf)
            steeringWheel.gameObject.SetActive(false);

        if (handbrakeButton && handbrakeButton.gameObject.activeSelf)
            handbrakeButton.gameObject.SetActive(false);

        if (NOSButton && NOSButton.gameObject.activeSelf)
            NOSButton.gameObject.SetActive(false);

        if (NOSButtonSteeringWheel && NOSButtonSteeringWheel.gameObject.activeSelf)
            NOSButtonSteeringWheel.gameObject.SetActive(false);

        //if (gearButton && gearButton.gameObject.activeSelf)
        //    gearButton.gameObject.SetActive(false);

        if (joystick && joystick.gameObject.activeSelf)
            joystick.gameObject.SetActive(false);

    }

    /// <summary>
    /// Enables all mobile buttons.
    /// </summary>
    private void EnableButtons() {

        if (!useGradualThrottle) {

            if (gasButton && !gasButton.gameObject.activeSelf)
                gasButton.gameObject.SetActive(true);

            if (gradualGasButton && gradualGasButton.gameObject.activeSelf)
                gradualGasButton.gameObject.SetActive(false);

        } else {

            if (gradualGasButton && !gradualGasButton.gameObject.activeSelf)
                gradualGasButton.gameObject.SetActive(true);

            if (gasButton && gasButton.gameObject.activeSelf)
                gasButton.gameObject.SetActive(false);

        }

        if (leftButton && !leftButton.gameObject.activeSelf)
            leftButton.gameObject.SetActive(true);

        if (rightButton && !rightButton.gameObject.activeSelf)
            rightButton.gameObject.SetActive(true);

        if (brakeButton && !brakeButton.gameObject.activeSelf)
            brakeButton.gameObject.SetActive(true);

        if (steeringWheel && !steeringWheel.gameObject.activeSelf)
            steeringWheel.gameObject.SetActive(true);

        if (handbrakeButton && !handbrakeButton.gameObject.activeSelf)
            handbrakeButton.gameObject.SetActive(true);

        if (NOSButton && !NOSButton.gameObject.activeSelf)
            NOSButton.gameObject.SetActive(true);

        if (NOSButtonSteeringWheel && !NOSButtonSteeringWheel.gameObject.activeSelf)
            NOSButtonSteeringWheel.gameObject.SetActive(true);

        //if (gearButton && !gearButton.gameObject.activeSelf)
        //    gearButton.gameObject.SetActive(true);

        if (joystick && !joystick.gameObject.activeSelf)
            joystick.gameObject.SetActive(true);

    }

    private void Update() {

        // If mobile controllers are not enabled, return.
        if (!RCC_Settings.Instance.mobileControllerEnabled)
            return;

        //  Mobile controller has four options. Buttons, gyro, steering wheel, and joystick.
        switch (RCC_Settings.Instance.mobileController) {

            case RCC_Settings.MobileController.TouchScreen:

                if (RCC_InputManager.Instance.gyroUsed) {

                    RCC_InputManager.Instance.gyroUsed = false;

                    if (UnityEngine.InputSystem.Accelerometer.current != null)
                        InputSystem.DisableDevice(Accelerometer.current);

                }

                gyroInput = 0f;

                if (steeringWheel && steeringWheel.gameObject.activeInHierarchy)
                    steeringWheel.gameObject.SetActive(false);

                if (NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
                    NOSButton.gameObject.SetActive(canUseNos);

                if (joystick && joystick.gameObject.activeInHierarchy)
                    joystick.gameObject.SetActive(false);

                if (!leftButton.gameObject.activeInHierarchy) {

                    if (orgBrakeButtonPos != Vector3.zero)
                        brakeButton.transform.position = orgBrakeButtonPos;

                    leftButton.gameObject.SetActive(true);

                }

                if (!rightButton.gameObject.activeInHierarchy)
                    rightButton.gameObject.SetActive(true);

                break;

            case RCC_Settings.MobileController.Gyro:

                if (!RCC_InputManager.Instance.gyroUsed) {

                    RCC_InputManager.Instance.gyroUsed = true;

                    if (UnityEngine.InputSystem.Accelerometer.current != null)
                        InputSystem.EnableDevice(Accelerometer.current);

                }

                if (Accelerometer.current != null)
                    gyroInput = Mathf.Lerp(gyroInput, Accelerometer.current.acceleration.ReadValue().x * RCC_Settings.Instance.gyroSensitivity, Time.deltaTime * 5f);
                else
                    gyroInput = 0f;

                brakeButton.transform.position = leftButton.transform.position;

                if (steeringWheel && steeringWheel.gameObject.activeInHierarchy)
                    steeringWheel.gameObject.SetActive(false);

                if (NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
                    NOSButton.gameObject.SetActive(canUseNos);

                if (joystick && joystick.gameObject.activeInHierarchy)
                    joystick.gameObject.SetActive(false);

                if (leftButton.gameObject.activeInHierarchy)
                    leftButton.gameObject.SetActive(false);

                if (rightButton.gameObject.activeInHierarchy)
                    rightButton.gameObject.SetActive(false);

                break;

            case RCC_Settings.MobileController.SteeringWheel:

                if (RCC_InputManager.Instance.gyroUsed) {

                    RCC_InputManager.Instance.gyroUsed = false;

                    if (UnityEngine.InputSystem.Accelerometer.current != null)
                        InputSystem.DisableDevice(Accelerometer.current);

                }

                gyroInput = 0f;

                if (steeringWheel && !steeringWheel.gameObject.activeInHierarchy) {

                    steeringWheel.gameObject.SetActive(true);

                    if (orgBrakeButtonPos != Vector3.zero)
                        brakeButton.transform.position = orgBrakeButtonPos;

                }

                if (NOSButton && NOSButton.gameObject.activeInHierarchy)
                    NOSButton.gameObject.SetActive(false);

                if (NOSButtonSteeringWheel && NOSButtonSteeringWheel.gameObject.activeInHierarchy != canUseNos)
                    NOSButtonSteeringWheel.gameObject.SetActive(canUseNos);

                if (joystick && joystick.gameObject.activeInHierarchy)
                    joystick.gameObject.SetActive(false);

                if (leftButton.gameObject.activeInHierarchy)
                    leftButton.gameObject.SetActive(false);
                if (rightButton.gameObject.activeInHierarchy)
                    rightButton.gameObject.SetActive(false);

                break;

            case RCC_Settings.MobileController.Joystick:

                if (RCC_InputManager.Instance.gyroUsed) {

                    RCC_InputManager.Instance.gyroUsed = false;

                    if (UnityEngine.InputSystem.Accelerometer.current != null)
                        InputSystem.DisableDevice(Accelerometer.current);

                }

                gyroInput = 0f;

                if (steeringWheel && steeringWheel.gameObject.activeInHierarchy)
                    steeringWheel.gameObject.SetActive(false);

                if (NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
                    NOSButton.gameObject.SetActive(canUseNos);

                if (joystick && !joystick.gameObject.activeInHierarchy) {

                    joystick.gameObject.SetActive(true);
                    brakeButton.transform.position = orgBrakeButtonPos;

                }

                if (leftButton.gameObject.activeInHierarchy)
                    leftButton.gameObject.SetActive(false);

                if (rightButton.gameObject.activeInHierarchy)
                    rightButton.gameObject.SetActive(false);

                break;

        }

        if (!useGradualThrottle)
            throttleInput = GetInput(gasButton);
        else
            throttleInput = GetInput(gradualGasButton);

        brakeInput = GetInput(brakeButton);
        leftInput = GetInput(leftButton);
        rightInput = GetInput(rightButton);
        handbrakeInput = GetInput(handbrakeButton);
        boostInput = Mathf.Clamp((GetInput(NOSButton) + GetInput(NOSButtonSteeringWheel)), 0f, 1f);

        throttleInput += boostInput;
        throttleInput = Mathf.Clamp01(throttleInput);

        if (steeringWheel && steeringWheel.gameObject.activeSelf)
            steeringWheelInput = steeringWheel.input;
        else
            steeringWheelInput = 0f;

        if (joystick && joystick.gameObject.activeSelf)
            joystickInput = joystick.InputHorizontal;
        else
            joystickInput = 0f;

        if (useGradualThrottle != lastUseGradualThrottle) {

            if (gasButton)
                gasButton.gameObject.SetActive(!useGradualThrottle);

            if (gradualGasButton)
                gradualGasButton.gameObject.SetActive(useGradualThrottle);

        }

        lastUseGradualThrottle = useGradualThrottle;

        SetMobileInputs();

    }

    /// <summary>
    /// Setting mobile inputs.
    /// </summary>
    private void SetMobileInputs() {

        if (RCC_SceneManager.Instance.activePlayerVehicle)
            canUseNos = RCC_SceneManager.Instance.activePlayerVehicle.useNOS;
        else
            canUseNos = false;

        mobileInputs.throttleInput = throttleInput;
        mobileInputs.brakeInput = brakeInput;
        mobileInputs.steerInput = -leftInput + rightInput + steeringWheelInput + gyroInput + joystickInput;
        mobileInputs.handbrakeInput = handbrakeInput;
        mobileInputs.boostInput = boostInput;

    }

    /// <summary>
    /// Gets input from button.
    /// </summary>
    /// <param name="button"></param>
    /// <returns></returns>
    private float GetInput(RCC_UIController button) {

        if (button == null)
            return 0f;

        if (!button.gameObject.activeSelf)
            return 0f;

        return (button.input);

    }

    private void OnDisable() {

        RCC_SceneManager.OnVehicleChanged -= CheckMobileButtons;

    }

}
