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

/// <summary>
/// Receiving inputs from active vehicle on your scene, and feeds visual dashboard needles (Not UI).
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Visual Dashboard Objects")]
public class RCC_DashboardObjects : MonoBehaviour {

    // Car controller.
    public RCC_CarControllerV3 CarController {
        get {
            if (carController == null)
                carController = GetComponentInParent<RCC_CarControllerV3>();
            return carController;
        }
    }
    private RCC_CarControllerV3 carController;

    [System.Serializable]
    public class RPMDial {

        public GameObject dial;
        public float multiplier = .05f;
        public RotateAround rotateAround = RotateAround.Z;
        private Quaternion dialOrgRotation = Quaternion.identity;
        public Text text;

        public void Init() {

            if (dial)
                dialOrgRotation = dial.transform.localRotation;

        }

        public void Update(float value) {

            Vector3 targetAxis = Vector3.forward;

            switch (rotateAround) {

                case RotateAround.X:

                    targetAxis = Vector3.right;
                    break;

                case RotateAround.Y:

                    targetAxis = Vector3.up;
                    break;

                case RotateAround.Z:

                    targetAxis = Vector3.forward;
                    break;

            }

            dial.transform.localRotation = dialOrgRotation * Quaternion.AngleAxis(-multiplier * value, targetAxis);

            if (text)
                text.text = value.ToString("F0");

        }

    }

    [System.Serializable]
    public class SpeedoMeterDial {

        public GameObject dial;
        public float multiplier = 1f;
        public RotateAround rotateAround = RotateAround.Z;
        private Quaternion dialOrgRotation = Quaternion.identity;
        public Text text;

        public void Init() {

            if (dial)
                dialOrgRotation = dial.transform.localRotation;

        }

        public void Update(float value) {

            Vector3 targetAxis = Vector3.forward;

            switch (rotateAround) {

                case RotateAround.X:

                    targetAxis = Vector3.right;
                    break;

                case RotateAround.Y:

                    targetAxis = Vector3.up;
                    break;

                case RotateAround.Z:

                    targetAxis = Vector3.forward;
                    break;

            }

            dial.transform.localRotation = dialOrgRotation * Quaternion.AngleAxis(-multiplier * value, targetAxis);

            if (text)
                text.text = value.ToString("F0");

        }

    }

    [System.Serializable]
    public class FuelDial {

        public GameObject dial;
        public float multiplier = .1f;
        public RotateAround rotateAround = RotateAround.Z;
        private Quaternion dialOrgRotation = Quaternion.identity;
        public Text text;

        public void Init() {

            if (dial)
                dialOrgRotation = dial.transform.localRotation;

        }

        public void Update(float value) {

            Vector3 targetAxis = Vector3.forward;

            switch (rotateAround) {

                case RotateAround.X:

                    targetAxis = Vector3.right;
                    break;

                case RotateAround.Y:

                    targetAxis = Vector3.up;
                    break;

                case RotateAround.Z:

                    targetAxis = Vector3.forward;
                    break;

            }

            dial.transform.localRotation = dialOrgRotation * Quaternion.AngleAxis(-multiplier * value, targetAxis);

            if (text)
                text.text = value.ToString("F0");

        }

    }

    [System.Serializable]
    public class HeatDial {

        public GameObject dial;
        public float multiplier = .1f;
        public RotateAround rotateAround = RotateAround.Z;
        private Quaternion dialOrgRotation = Quaternion.identity;
        public Text text;

        public void Init() {

            if (dial)
                dialOrgRotation = dial.transform.localRotation;

        }

        public void Update(float value) {

            Vector3 targetAxis = Vector3.forward;

            switch (rotateAround) {

                case RotateAround.X:

                    targetAxis = Vector3.right;
                    break;

                case RotateAround.Y:

                    targetAxis = Vector3.up;
                    break;

                case RotateAround.Z:

                    targetAxis = Vector3.forward;
                    break;

            }

            dial.transform.localRotation = dialOrgRotation * Quaternion.AngleAxis(-multiplier * value, targetAxis);

            if (text)
                text.text = value.ToString("F0");

        }

    }

    [System.Serializable]
    public class InteriorLight {

        public Light light;
        public float intensity = 1f;
        public LightRenderMode renderMode = LightRenderMode.Auto;

        public void Update(bool state) {

            if (!light.enabled)
                light.enabled = true;

            light.renderMode = renderMode;
            light.intensity = state ? intensity : 0f;

        }

    }

    [Space()]
    public RPMDial rPMDial = new RPMDial();
    [Space()]
    public SpeedoMeterDial speedDial = new SpeedoMeterDial();
    [Space()]
    public FuelDial fuelDial = new FuelDial();
    [Space()]
    public HeatDial heatDial = new HeatDial();
    [Space()]
    public InteriorLight[] interiorLights = new InteriorLight[0];

    public enum RotateAround { X, Y, Z }

    private void Awake() {

        //  Initializing dials.
        rPMDial.Init();
        speedDial.Init();
        fuelDial.Init();
        heatDial.Init();

    }

    private void Update() {

        //  If no vehicle found, return.
        if (!CarController)
            return;

        Dials();
        Lights();

    }

    /// <summary>
    /// Updates dials rotation.
    /// </summary>
    private void Dials() {

        if (rPMDial.dial != null)
            rPMDial.Update(CarController.engineRPM);

        if (speedDial.dial != null)
            speedDial.Update(CarController.speed);

        if (fuelDial.dial != null)
            fuelDial.Update(CarController.fuelTank);

        if (heatDial.dial != null)
            heatDial.Update(CarController.engineHeat);

    }

    /// <summary>
    /// Updates lights of the dash.
    /// </summary>
    private void Lights() {

        for (int i = 0; i < interiorLights.Length; i++)
            interiorLights[i].Update(CarController.lowBeamHeadLightsOn);

    }

}
