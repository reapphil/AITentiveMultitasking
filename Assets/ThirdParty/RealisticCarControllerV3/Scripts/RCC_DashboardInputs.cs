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
/// Receiving inputs from active vehicle on your scene, and feeds dashboard needles, texts, images.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/RCC UI Dashboard Inputs")]
public class RCC_DashboardInputs : MonoBehaviour {

    public RCC_CarControllerV3 vehicle;     //  Target vehicle.
    public bool autoAssignVehicle = true;       //  Auto assign target vehicle as player vehicle from the RCC_SceneManager.

    //  Needles.
    [Header("Needles")]
    public GameObject RPMNeedle;
    public GameObject KMHNeedle;
    public GameObject turboGauge;
    public GameObject turboNeedle;
    public GameObject NOSGauge;
    public GameObject NoSNeedle;
    public GameObject heatGauge;
    public GameObject heatNeedle;
    public GameObject fuelGauge;
    public GameObject fuelNeedle;

    //  Needle rotations.
    private float RPMNeedleRotation = 0f;
    private float KMHNeedleRotation = 0f;
    private float BoostNeedleRotation = 0f;
    private float NoSNeedleRotation = 0f;
    private float heatNeedleRotation = 0f;
    private float fuelNeedleRotation = 0f;

    //  Variables of the player vehicle.
    [HideInInspector] public float RPM;
    [HideInInspector] public float KMH;
    [HideInInspector] public int direction = 1;
    [HideInInspector] public float Gear;
    [HideInInspector] public bool changingGear = false;
    [HideInInspector] public bool NGear = false;
    [HideInInspector] public bool ABS = false;
    [HideInInspector] public bool ESP = false;
    [HideInInspector] public bool Park = false;
    [HideInInspector] public bool Headlights = false;
    [HideInInspector] public RCC_CarControllerV3.IndicatorsOn indicators;

    private void Update() {

        if (autoAssignVehicle && RCC_SceneManager.Instance.activePlayerVehicle)
            vehicle = RCC_SceneManager.Instance.activePlayerVehicle;
        else
            vehicle = null;

        //  If no any player vehicle, return.
        if (!vehicle)
            return;

        //  If player vehicle is not controllable or controlled by AI, return.
        if (!vehicle.canControl || vehicle.externalController)
            return;

        //  If nos gauge is selected, enable or disable gauge related to vehicle. 
        if (NOSGauge) {

            if (vehicle.useNOS) {

                if (!NOSGauge.activeSelf)
                    NOSGauge.SetActive(true);

            } else {

                if (NOSGauge.activeSelf)
                    NOSGauge.SetActive(false);

            }

        }

        //  If turbo gauge is selected, enable or disable turbo gauge related to vehicle.
        if (turboGauge) {

            if (vehicle.useTurbo) {

                if (!turboGauge.activeSelf)
                    turboGauge.SetActive(true);

            } else {

                if (turboGauge.activeSelf)
                    turboGauge.SetActive(false);

            }

        }

        //  If heat gauge is selected, enable or disable heat gauge related to vehicle.
        if (heatGauge) {

            if (vehicle.useEngineHeat) {

                if (!heatGauge.activeSelf)
                    heatGauge.SetActive(true);

            } else {

                if (heatGauge.activeSelf)
                    heatGauge.SetActive(false);

            }

        }

        //  If fuel gauge is selected, enable or disable fuel gauge related to vehicle.
        if (fuelGauge) {

            if (vehicle.useFuelConsumption) {

                if (!fuelGauge.activeSelf)
                    fuelGauge.SetActive(true);

            } else {

                if (fuelGauge.activeSelf)
                    fuelGauge.SetActive(false);

            }

        }

        // Getting variables from the player vehicle.
        RPM = vehicle.engineRPM;
        KMH = vehicle.speed;
        direction = vehicle.direction;
        Gear = vehicle.currentGear;
        changingGear = vehicle.changingGear;
        NGear = vehicle.NGear;
        ABS = vehicle.ABSAct;
        ESP = vehicle.ESPAct;
        Park = vehicle.handbrakeInput > .1f ? true : false;
        Headlights = vehicle.lowBeamHeadLightsOn || vehicle.highBeamHeadLightsOn;
        indicators = vehicle.indicatorsOn;

        //  If RPM needle is selected, assign rotation of the needle.
        if (RPMNeedle) {

            RPMNeedleRotation = (vehicle.engineRPM / 50f);
            RPMNeedleRotation = Mathf.Clamp(RPMNeedleRotation, 0f, 180f);
            RPMNeedle.transform.eulerAngles = new Vector3(RPMNeedle.transform.eulerAngles.x, RPMNeedle.transform.eulerAngles.y, -RPMNeedleRotation);

        }

        //  If KMH needle is selected, assign rotation of the needle.
        if (KMHNeedle) {

            if (RCC_Settings.Instance.units == RCC_Settings.Units.KMH)
                KMHNeedleRotation = (vehicle.speed);
            else
                KMHNeedleRotation = (vehicle.speed * 0.62f);

            KMHNeedle.transform.eulerAngles = new Vector3(KMHNeedle.transform.eulerAngles.x, KMHNeedle.transform.eulerAngles.y, -KMHNeedleRotation);

        }

        //  If turbo needle is selected, assign rotation of the needle.
        if (turboNeedle) {

            BoostNeedleRotation = (vehicle.turboBoost / 30f) * 270f;
            turboNeedle.transform.eulerAngles = new Vector3(turboNeedle.transform.eulerAngles.x, turboNeedle.transform.eulerAngles.y, -BoostNeedleRotation);

        }

        //  If nos needle is selected, assign rotation of the needle.
        if (NoSNeedle) {

            NoSNeedleRotation = (vehicle.NoS / 100f) * 270f;
            NoSNeedle.transform.eulerAngles = new Vector3(NoSNeedle.transform.eulerAngles.x, NoSNeedle.transform.eulerAngles.y, -NoSNeedleRotation);

        }

        //  If heat needle is selected, assign rotation of the needle.
        if (heatNeedle) {

            heatNeedleRotation = (vehicle.engineHeat / 110f) * 270f;
            heatNeedle.transform.eulerAngles = new Vector3(heatNeedle.transform.eulerAngles.x, heatNeedle.transform.eulerAngles.y, -heatNeedleRotation);

        }

        //  If fuel needle is selected, assign rotation of the needle.
        if (fuelNeedle) {

            fuelNeedleRotation = (vehicle.fuelTank / vehicle.fuelTankCapacity) * 270f;
            fuelNeedle.transform.eulerAngles = new Vector3(fuelNeedle.transform.eulerAngles.x, fuelNeedle.transform.eulerAngles.y, -fuelNeedleRotation);

        }

    }

}
