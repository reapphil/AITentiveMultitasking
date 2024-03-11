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
using UnityEngine.EventSystems;

/// <summary>
/// UI buttons used in options panel. It has an enum for all kind of buttons. 
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/RCC UI Dashboard Button")]
public class RCC_UIDashboardButton : MonoBehaviour, IPointerClickHandler {

    private Button button;        //  Button.

    public ButtonType _buttonType = ButtonType.ABS;      //  Type of the button.
    public enum ButtonType { Start, ABS, ESP, TCS, Headlights, LeftIndicator, RightIndicator, Gear, Low, Med, High, SH, GearUp, GearDown, HazardLights, SlowMo, Record, Replay, Neutral, ChangeCamera };
    private Scrollbar gearSlider;

    public int gearDirection = 0;

    /// <summary>
    /// When clicked.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData) {

        OnClicked();

    }

    private void Awake() {

        button = GetComponent<Button>();

        //  If this button type is a gear selector, get scrollbar and add listener.
        if (_buttonType == ButtonType.Gear && GetComponentInChildren<Scrollbar>()) {

            gearSlider = GetComponentInChildren<Scrollbar>();
            gearSlider.onValueChanged.AddListener(delegate { ChangeGear(); });

        }

    }

    private void OnEnable() {

        //  Updating image of the button.
        UpdateImageOfButton();

    }

    /// <summary>
    /// When clicked.
    /// </summary>
    private void OnClicked() {

        switch (_buttonType) {

            case ButtonType.Low:

                QualitySettings.SetQualityLevel(1);

                break;

            case ButtonType.Med:

                QualitySettings.SetQualityLevel(3);

                break;

            case ButtonType.High:

                QualitySettings.SetQualityLevel(5);

                break;

            case ButtonType.SlowMo:

                if (Time.timeScale != .2f)
                    Time.timeScale = .2f;
                else
                    Time.timeScale = 1f;

                break;

            case ButtonType.Record:

                RCC.StartStopRecord();

                break;

            case ButtonType.Replay:

                RCC.StartStopReplay();

                break;

            case ButtonType.Neutral:

                RCC.StopRecordReplay();

                break;

            case ButtonType.ChangeCamera:

                RCC.ChangeCamera();

                break;


            case ButtonType.Start:

                if (RCC_SceneManager.Instance.activePlayerVehicle)
                    RCC_SceneManager.Instance.activePlayerVehicle.KillOrStartEngine();

                break;

            case ButtonType.ABS:

                if (RCC_SceneManager.Instance.activePlayerVehicle)
                    RCC_SceneManager.Instance.activePlayerVehicle.ABS = !RCC_SceneManager.Instance.activePlayerVehicle.ABS;

                break;

            case ButtonType.ESP:

                if (RCC_SceneManager.Instance.activePlayerVehicle)
                    RCC_SceneManager.Instance.activePlayerVehicle.ESP = !RCC_SceneManager.Instance.activePlayerVehicle.ESP;

                break;

            case ButtonType.TCS:

                if (RCC_SceneManager.Instance.activePlayerVehicle)
                    RCC_SceneManager.Instance.activePlayerVehicle.TCS = !RCC_SceneManager.Instance.activePlayerVehicle.TCS;

                break;

            case ButtonType.SH:

                if (RCC_SceneManager.Instance.activePlayerVehicle)
                    RCC_SceneManager.Instance.activePlayerVehicle.steeringHelper = !RCC_SceneManager.Instance.activePlayerVehicle.steeringHelper;

                break;

            case ButtonType.Headlights:

                if (RCC_SceneManager.Instance.activePlayerVehicle) {

                    if (!RCC_SceneManager.Instance.activePlayerVehicle.highBeamHeadLightsOn && RCC_SceneManager.Instance.activePlayerVehicle.lowBeamHeadLightsOn) {

                        RCC_SceneManager.Instance.activePlayerVehicle.highBeamHeadLightsOn = true;
                        RCC_SceneManager.Instance.activePlayerVehicle.lowBeamHeadLightsOn = true;
                        break;

                    }

                    if (!RCC_SceneManager.Instance.activePlayerVehicle.lowBeamHeadLightsOn)
                        RCC_SceneManager.Instance.activePlayerVehicle.lowBeamHeadLightsOn = true;

                    if (RCC_SceneManager.Instance.activePlayerVehicle.highBeamHeadLightsOn) {

                        RCC_SceneManager.Instance.activePlayerVehicle.lowBeamHeadLightsOn = false;
                        RCC_SceneManager.Instance.activePlayerVehicle.highBeamHeadLightsOn = false;

                    }

                }

                break;

            case ButtonType.LeftIndicator:

                if (RCC_SceneManager.Instance.activePlayerVehicle) {

                    if (RCC_SceneManager.Instance.activePlayerVehicle.indicatorsOn != RCC_CarControllerV3.IndicatorsOn.Left)
                        RCC_SceneManager.Instance.activePlayerVehicle.indicatorsOn = RCC_CarControllerV3.IndicatorsOn.Left;
                    else
                        RCC_SceneManager.Instance.activePlayerVehicle.indicatorsOn = RCC_CarControllerV3.IndicatorsOn.Off;

                }

                break;

            case ButtonType.RightIndicator:

                if (RCC_SceneManager.Instance.activePlayerVehicle) {

                    if (RCC_SceneManager.Instance.activePlayerVehicle.indicatorsOn != RCC_CarControllerV3.IndicatorsOn.Right)
                        RCC_SceneManager.Instance.activePlayerVehicle.indicatorsOn = RCC_CarControllerV3.IndicatorsOn.Right;
                    else
                        RCC_SceneManager.Instance.activePlayerVehicle.indicatorsOn = RCC_CarControllerV3.IndicatorsOn.Off;

                }

                break;

            case ButtonType.HazardLights:

                if (RCC_SceneManager.Instance.activePlayerVehicle) {

                    if (RCC_SceneManager.Instance.activePlayerVehicle.indicatorsOn != RCC_CarControllerV3.IndicatorsOn.All)
                        RCC_SceneManager.Instance.activePlayerVehicle.indicatorsOn = RCC_CarControllerV3.IndicatorsOn.All;
                    else
                        RCC_SceneManager.Instance.activePlayerVehicle.indicatorsOn = RCC_CarControllerV3.IndicatorsOn.Off;

                }

                break;

            case ButtonType.GearUp:

                if (RCC_SceneManager.Instance.activePlayerVehicle)
                    RCC_SceneManager.Instance.activePlayerVehicle.GearShiftUp();

                break;

            case ButtonType.GearDown:

                if (RCC_SceneManager.Instance.activePlayerVehicle)
                    RCC_SceneManager.Instance.activePlayerVehicle.GearShiftDown();

                break;

        }

        UpdateImageOfButton();

    }

    /// <summary>
    /// Checking ABS, ESP, TCS, SH, And Headlights button. This will illuminate the corresponding button.
    /// </summary>
    public void UpdateImageOfButton() {

        if (!button)
            return;

        //  If no image attached to the button, return.
        if (!button.image)
            return;

        //  If no player vehicle found, return.
        if (!RCC_SceneManager.Instance.activePlayerVehicle)
            return;

        //  Illuminating the image of the button when it's on.
        switch (_buttonType) {

            case ButtonType.ABS:

                if (RCC_SceneManager.Instance.activePlayerVehicle.ABS)
                    button.image.color = new Color(1, 1, 1, 1);
                else
                    button.image.color = new Color(.25f, .25f, .25f, 1);

                break;

            case ButtonType.ESP:

                if (RCC_SceneManager.Instance.activePlayerVehicle.ESP)
                    button.image.color = new Color(1, 1, 1, 1);
                else
                    button.image.color = new Color(.25f, .25f, .25f, 1);

                break;

            case ButtonType.TCS:

                if (RCC_SceneManager.Instance.activePlayerVehicle.TCS)
                    button.image.color = new Color(1, 1, 1, 1);
                else
                    button.image.color = new Color(.25f, .25f, .25f, 1);

                break;

            case ButtonType.SH:

                if (RCC_SceneManager.Instance.activePlayerVehicle.steeringHelper)
                    button.image.color = new Color(1, 1, 1, 1);
                else
                    button.image.color = new Color(.25f, .25f, .25f, 1);

                break;

            case ButtonType.Headlights:

                if (RCC_SceneManager.Instance.activePlayerVehicle.lowBeamHeadLightsOn || RCC_SceneManager.Instance.activePlayerVehicle.highBeamHeadLightsOn)
                    button.image.color = new Color(1, 1, 1, 1);
                else
                    button.image.color = new Color(.25f, .25f, .25f, 1);

                break;

        }

    }

    /// <summary>
    /// Changes the gear.
    /// </summary>
    public void ChangeGear() {

        if (!RCC_SceneManager.Instance.activePlayerVehicle)
            return;

        if (gearDirection == Mathf.CeilToInt(gearSlider.value * 2))
            return;

        gearDirection = Mathf.CeilToInt(gearSlider.value * 2);

        RCC_SceneManager.Instance.activePlayerVehicle.semiAutomaticGear = true;

        switch (gearDirection) {

            case 0:
                RCC_SceneManager.Instance.activePlayerVehicle.StartCoroutine("ChangeGear", 0);
                RCC_SceneManager.Instance.activePlayerVehicle.NGear = false;
                break;

            case 1:
                RCC_SceneManager.Instance.activePlayerVehicle.NGear = true;
                break;

            case 2:
                RCC_SceneManager.Instance.activePlayerVehicle.StartCoroutine("ChangeGear", -1);
                RCC_SceneManager.Instance.activePlayerVehicle.NGear = false;
                break;

        }

    }

    private void OnDisable() {

        //		if (!RCC_SceneManager.Instance.activePlayerVehicle)
        //			return;
        //
        //		if(_buttonType == ButtonType.Gear){
        //
        //			RCC_SceneManager.Instance.activePlayerVehicle.semiAutomaticGear = false;
        //
        //		}

    }

}
