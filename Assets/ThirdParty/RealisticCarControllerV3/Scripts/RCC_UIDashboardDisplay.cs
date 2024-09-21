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
using UnityEngine.UI;

/// <summary>
/// Handles RCC Canvas dashboard elements.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/RCC UI Dashboard Displayer")]
[RequireComponent(typeof(RCC_DashboardInputs))]
public class RCC_UIDashboardDisplay : MonoBehaviour {

    //  Inputs of the dashboard elements.
    private RCC_DashboardInputs inputs;
    private RCC_DashboardInputs Inputs {

        get {

            if (inputs == null)
                inputs = GetComponent<RCC_DashboardInputs>();

            return inputs;

        }

    }

    public DisplayType displayType = DisplayType.Full;     //  Current display type.
    public enum DisplayType { Full, Customization, TopButtonsOnly, Off }

    public RCC_CarControllerV3 vehicle;
    public bool autoAssignVehicle = true;

    //  Buttons, texts, images, and dropdown menus.
    [Header("Panels")]
    public GameObject controllerButtons;
    public GameObject gauges;
    public GameObject customizationMenu;

    [Header("Texts")]
    public Text RPMLabel;
    public Text KMHLabel;
    public Text GearLabel;
    public Text recordingLabel;

    [Header("Images")]
    public Image ABS;
    public Image ESP;
    public Image Park;
    public Image Headlights;
    public Image leftIndicator;
    public Image rightIndicator;
    public Image heatIndicator;
    public Image fuelIndicator;
    public Image rpmIndicator;

    [Header("Colors")]
    public Color color_On = Color.yellow;
    public Color color_Off = Color.white;

    [Header("Dropdowns")]
    public Dropdown mobileControllersDropdown;

    private void Update() {

        if (mobileControllersDropdown)
            mobileControllersDropdown.interactable = RCC_Settings.Instance.mobileControllerEnabled;

        //  Enabling / disabling corresponding elements related to choosen display type.
        switch (displayType) {

            case DisplayType.Full:

                if (controllerButtons && !controllerButtons.activeSelf)
                    controllerButtons.SetActive(true);

                if (gauges && !gauges.activeSelf)
                    gauges.SetActive(true);

                if (customizationMenu && customizationMenu.activeSelf)
                    customizationMenu.SetActive(false);

                break;

            case DisplayType.Customization:

                if (controllerButtons && controllerButtons.activeSelf)
                    controllerButtons.SetActive(false);

                if (gauges && gauges.activeSelf)
                    gauges.SetActive(false);

                if (customizationMenu && !customizationMenu.activeSelf)
                    customizationMenu.SetActive(true);

                break;

            case DisplayType.TopButtonsOnly:

                if (controllerButtons.activeSelf)
                    controllerButtons.SetActive(false);

                if (gauges.activeSelf)
                    gauges.SetActive(false);

                if (customizationMenu.activeSelf)
                    customizationMenu.SetActive(false);

                break;

            case DisplayType.Off:

                if (controllerButtons && controllerButtons.activeSelf)
                    controllerButtons.SetActive(false);

                if (gauges && gauges.activeSelf)
                    gauges.SetActive(false);

                if (customizationMenu && customizationMenu.activeSelf)
                    customizationMenu.SetActive(false);

                break;

        }

    }

    private void LateUpdate() {

        //  If inputs are not enabled yet, disable it and return.
        if (!Inputs.enabled)
            return;

        if (autoAssignVehicle && RCC_SceneManager.Instance.activePlayerVehicle)
            vehicle = RCC_SceneManager.Instance.activePlayerVehicle;

        if (!vehicle)
            return;

        if (RPMLabel)
            RPMLabel.text = Inputs.RPM.ToString("0");

        if (KMHLabel) {

            if (RCC_Settings.Instance.units == RCC_Settings.Units.KMH)
                KMHLabel.text = Inputs.KMH.ToString("0") + "\nKMH";
            else
                KMHLabel.text = (Inputs.KMH * 0.62f).ToString("0") + "\nMPH";

        }

        if (GearLabel) {

            if (!Inputs.NGear && !Inputs.changingGear)
                GearLabel.text = Inputs.direction == 1 ? (Inputs.Gear + 1).ToString("0") : "R";
            else
                GearLabel.text = "N";

        }

        if (recordingLabel) {

            switch (RCC_SceneManager.Instance.recordMode) {

                case RCC_SceneManager.RecordMode.Neutral:

                    if (recordingLabel.gameObject.activeSelf)
                        recordingLabel.gameObject.SetActive(false);

                    recordingLabel.text = "";

                    break;

                case RCC_SceneManager.RecordMode.Play:

                    if (!recordingLabel.gameObject.activeSelf)
                        recordingLabel.gameObject.SetActive(true);

                    recordingLabel.text = "Playing";
                    recordingLabel.color = Color.green;

                    break;

                case RCC_SceneManager.RecordMode.Record:

                    if (!recordingLabel.gameObject.activeSelf)
                        recordingLabel.gameObject.SetActive(true);

                    recordingLabel.text = "Recording";
                    recordingLabel.color = Color.red;

                    break;

            }

        }

        if (ABS)
            ABS.color = Inputs.ABS == true ? color_On : color_Off;

        if (ESP)
            ESP.color = Inputs.ESP == true ? color_On : color_Off;

        if (Park)
            Park.color = Inputs.Park == true ? Color.red : color_Off;

        if (Headlights)
            Headlights.color = Inputs.Headlights == true ? Color.green : color_Off;

        if (heatIndicator)
            heatIndicator.color = vehicle.engineHeat >= 100f ? Color.red : new Color(.1f, 0f, 0f);

        if (fuelIndicator)
            fuelIndicator.color = vehicle.fuelTank < 10f ? Color.red : new Color(.1f, 0f, 0f);

        if (rpmIndicator)
            rpmIndicator.color = vehicle.engineRPM >= vehicle.maxEngineRPM - 500f ? Color.red : new Color(.1f, 0f, 0f);

        if (leftIndicator && rightIndicator) {

            switch (Inputs.indicators) {

                case RCC_CarControllerV3.IndicatorsOn.Left:
                    leftIndicator.color = new Color(1f, .5f, 0f);
                    rightIndicator.color = new Color(.5f, .25f, 0f);
                    break;
                case RCC_CarControllerV3.IndicatorsOn.Right:
                    leftIndicator.color = new Color(.5f, .25f, 0f);
                    rightIndicator.color = new Color(1f, .5f, 0f);
                    break;
                case RCC_CarControllerV3.IndicatorsOn.All:
                    leftIndicator.color = new Color(1f, .5f, 0f);
                    rightIndicator.color = new Color(1f, .5f, 0f);
                    break;
                case RCC_CarControllerV3.IndicatorsOn.Off:
                    leftIndicator.color = new Color(.5f, .25f, 0f);
                    rightIndicator.color = new Color(.5f, .25f, 0f);
                    break;

            }

        }

    }

    public void SetDisplayType(DisplayType _displayType) {

        displayType = _displayType;

    }

}
