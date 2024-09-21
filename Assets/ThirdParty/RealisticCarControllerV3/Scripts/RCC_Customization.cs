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

///<summary>
/// Main Customization Class For RCC.
///</summary>
public class RCC_Customization : MonoBehaviour {

    /// <summary>
    /// Set Customization Mode. This will enable / disable controlling the vehicle, and enable / disable orbit camera mode.
    /// </summary>
    public static void SetCustomizationMode(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!vehicle) {

            Debug.LogError("Player vehicle is not selected for customization! Use RCC_Customization.SetCustomizationMode(playerVehicle, true/false); for enabling / disabling customization mode for player vehicle.");
            return;

        }

        //  Finding camera and dashboard.
        RCC_Camera cam = RCC_SceneManager.Instance.activePlayerCamera;
        RCC_UIDashboardDisplay UI = RCC_SceneManager.Instance.activePlayerCanvas;

        //  If enabled customization mode, set camera mode to TPS and set UI type to Customization. Set controllable state of the vehicle to false, we don't want to control the vehicle while customizing.
        if (state) {

            vehicle.SetCanControl(false);

            if (cam)
                cam.ChangeCamera(RCC_Camera.CameraMode.TPS);

            if (UI)
                UI.SetDisplayType(RCC_UIDashboardDisplay.DisplayType.Customization);

        } else {

            //  If disabled the customization mode, set camera mode to TPS and set UI type to Full. Set controllable state of the vehicle to true, and make sure previewing flames and exhaust is set to false.
            SetSmokeParticle(vehicle, false);
            SetExhaustFlame(vehicle, false);

            vehicle.SetCanControl(true);

            if (cam)
                cam.ChangeCamera(RCC_Camera.CameraMode.TPS);

            if (UI)
                UI.SetDisplayType(RCC_UIDashboardDisplay.DisplayType.Full);

        }

    }

    /// <summary>
    ///	 Enable / Disable Smoke Particles. You can use it for previewing current wheel smokes.
    /// </summary>
    public static void SetSmokeParticle(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.PreviewSmokeParticle(state);

    }

    /// <summary>
    /// Set Smoke Color.
    /// </summary>
    public static void SetSmokeColor(RCC_CarControllerV3 vehicle, int indexOfGroundMaterial, Color color) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Getting all wheelcolliders.
        RCC_WheelCollider[] wheels = vehicle.GetComponentsInChildren<RCC_WheelCollider>();

        //  And setting color of the particles.
        foreach (RCC_WheelCollider wheel in wheels) {

            for (int i = 0; i < wheel.allWheelParticles.Count; i++) {

                ParticleSystem ps = wheel.allWheelParticles[i];
                ParticleSystem.MainModule psmain = ps.main;
                color.a = psmain.startColor.color.a;
                psmain.startColor = color;

            }

        }

    }

    /// <summary>
    /// Set Headlights Color.
    /// </summary>
    public static void SetHeadlightsColor(RCC_CarControllerV3 vehicle, Color color) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Enabling headlights.
        vehicle.lowBeamHeadLightsOn = true;

        //  Getting all lights.
        RCC_Light[] lights = vehicle.GetComponentsInChildren<RCC_Light>();

        //  If light is headlight, set color.
        foreach (RCC_Light l in lights) {

            if (l.lightType == RCC_Light.LightType.HeadLight || l.lightType == RCC_Light.LightType.HighBeamHeadLight)
                l.GetComponent<Light>().color = color;

        }

    }

    /// <summary>
    /// Enable / Disable Exhaust Flame Particles.
    /// </summary>
    public static void SetExhaustFlame(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Getting all exhausts.
        RCC_Exhaust[] exhausts = vehicle.GetComponentsInChildren<RCC_Exhaust>();

        //  Enabling preview mode for all exhausts.
        foreach (RCC_Exhaust exhaust in exhausts)
            exhaust.previewFlames = state;

    }

    /// <summary>
    /// Set Front Wheel Cambers.
    /// </summary>
    public static void SetFrontCambers(RCC_CarControllerV3 vehicle, float camberAngle) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Getting all wheelcolliders.
        RCC_WheelCollider[] wc = vehicle.GetComponentsInChildren<RCC_WheelCollider>();

        //  Setting camber variable of front wheelcolliders.
        foreach (RCC_WheelCollider w in wc) {

            if (w == vehicle.FrontLeftWheelCollider || w == vehicle.FrontRightWheelCollider)
                w.camber = camberAngle;

        }

    }

    /// <summary>
    /// Set Rear Wheel Cambers.
    /// </summary>
    public static void SetRearCambers(RCC_CarControllerV3 vehicle, float camberAngle) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Getting all wheelcolliders.
        RCC_WheelCollider[] wc = vehicle.GetComponentsInChildren<RCC_WheelCollider>();

        //  Setting camber variable of rear wheelcolliders.
        foreach (RCC_WheelCollider w in wc) {

            if (w != vehicle.FrontLeftWheelCollider && w != vehicle.FrontRightWheelCollider)
                w.camber = camberAngle;

        }

    }

    /// <summary>
    /// Change Wheel Models. You can find your wheel models array in Tools --> BCG --> RCC --> Configure Changable Wheels.
    /// </summary>
    public static void ChangeWheels(RCC_CarControllerV3 vehicle, GameObject wheel, bool applyRadius) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Getting all wheelcolliders.
        for (int i = 0; i < vehicle.AllWheelColliders.Length; i++) {

            //  Disabling renderer of the wheelmodel.
            if (vehicle.AllWheelColliders[i].wheelModel.GetComponent<MeshRenderer>())
                vehicle.AllWheelColliders[i].wheelModel.GetComponent<MeshRenderer>().enabled = false;

            //  Disabling all child models of the wheel.
            foreach (Transform t in vehicle.AllWheelColliders[i].wheelModel.GetComponentInChildren<Transform>())
                t.gameObject.SetActive(false);

            //  Instantiating new wheel.
            GameObject newWheel = Instantiate(wheel, vehicle.AllWheelColliders[i].wheelModel.position, vehicle.AllWheelColliders[i].wheelModel.rotation, vehicle.AllWheelColliders[i].wheelModel);

            //  If wheel is at right side, multiply scale X by -1 for symetry.
            if (vehicle.AllWheelColliders[i].wheelModel.localPosition.x > 0f)
                newWheel.transform.localScale = new Vector3(newWheel.transform.localScale.x * -1f, newWheel.transform.localScale.y, newWheel.transform.localScale.z);

            //  If apply radius is set to true, calculate the radius.
            if (applyRadius)
                vehicle.AllWheelColliders[i].WheelCollider.radius = RCC_GetBounds.MaxBoundsExtent(wheel.transform);

        }

    }

    /// <summary>
    /// Set Front Suspension targetPositions. It changes targetPosition of the front WheelColliders.
    /// </summary>
    public static void SetFrontSuspensionsTargetPos(RCC_CarControllerV3 vehicle, float targetPosition) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Sets target position.
        targetPosition = Mathf.Clamp01(targetPosition);

        JointSpring spring1 = vehicle.FrontLeftWheelCollider.WheelCollider.suspensionSpring;
        spring1.targetPosition = 1f - targetPosition;

        vehicle.FrontLeftWheelCollider.WheelCollider.suspensionSpring = spring1;

        JointSpring spring2 = vehicle.FrontRightWheelCollider.WheelCollider.suspensionSpring;
        spring2.targetPosition = 1f - targetPosition;

        vehicle.FrontRightWheelCollider.WheelCollider.suspensionSpring = spring2;

    }

    /// <summary>
    /// Set Rear Suspension targetPositions. It changes targetPosition of the rear WheelColliders.
    /// </summary>
    public static void SetRearSuspensionsTargetPos(RCC_CarControllerV3 vehicle, float targetPosition) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Sets target position.
        targetPosition = Mathf.Clamp01(targetPosition);

        JointSpring spring1 = vehicle.RearLeftWheelCollider.WheelCollider.suspensionSpring;
        spring1.targetPosition = 1f - targetPosition;

        vehicle.RearLeftWheelCollider.WheelCollider.suspensionSpring = spring1;

        JointSpring spring2 = vehicle.RearRightWheelCollider.WheelCollider.suspensionSpring;
        spring2.targetPosition = 1f - targetPosition;

        vehicle.RearRightWheelCollider.WheelCollider.suspensionSpring = spring2;

    }

    /// <summary>
    /// Set All Suspension targetPositions. It changes targetPosition of the all WheelColliders.
    /// </summary>
    public static void SetAllSuspensionsTargetPos(RCC_CarControllerV3 vehicle, float targetPosition) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Sets target position.
        targetPosition = Mathf.Clamp01(targetPosition);

        JointSpring spring1 = vehicle.RearLeftWheelCollider.WheelCollider.suspensionSpring;
        spring1.targetPosition = 1f - targetPosition;

        vehicle.RearLeftWheelCollider.WheelCollider.suspensionSpring = spring1;

        JointSpring spring2 = vehicle.RearRightWheelCollider.WheelCollider.suspensionSpring;
        spring2.targetPosition = 1f - targetPosition;

        vehicle.RearRightWheelCollider.WheelCollider.suspensionSpring = spring2;

        JointSpring spring3 = vehicle.FrontLeftWheelCollider.WheelCollider.suspensionSpring;
        spring3.targetPosition = 1f - targetPosition;

        vehicle.FrontLeftWheelCollider.WheelCollider.suspensionSpring = spring3;

        JointSpring spring4 = vehicle.FrontRightWheelCollider.WheelCollider.suspensionSpring;
        spring4.targetPosition = 1f - targetPosition;

        vehicle.FrontRightWheelCollider.WheelCollider.suspensionSpring = spring4;

    }

    /// <summary>
    /// Set Front Suspension Distances.
    /// </summary>
    public static void SetFrontSuspensionsDistances(RCC_CarControllerV3 vehicle, float distance) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Make sure new distance is not close to 0.
        if (distance <= .01)
            distance = .05f;

        //  Setting suspension distance of front wheelcolliders.
        vehicle.FrontLeftWheelCollider.WheelCollider.suspensionDistance = distance;
        vehicle.FrontRightWheelCollider.WheelCollider.suspensionDistance = distance;

    }

    /// <summary>
    /// Set Rear Suspension Distances.
    /// </summary>
    public static void SetRearSuspensionsDistances(RCC_CarControllerV3 vehicle, float distance) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Make sure new distance is not close to 0.
        if (distance <= .01)
            distance = .05f;

        //  Setting suspension distance of front wheelcolliders.
        vehicle.RearLeftWheelCollider.WheelCollider.suspensionDistance = distance;
        vehicle.RearRightWheelCollider.WheelCollider.suspensionDistance = distance;

        if (vehicle.ExtraRearWheelsCollider != null && vehicle.ExtraRearWheelsCollider.Length > 0) {

            foreach (RCC_WheelCollider wc in vehicle.ExtraRearWheelsCollider)
                wc.WheelCollider.suspensionDistance = distance;

        }

    }

    /// <summary>
    /// Set Drivetrain Mode.
    /// </summary>
    public static void SetDrivetrainMode(RCC_CarControllerV3 vehicle, RCC_CarControllerV3.WheelType mode) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.wheelTypeChoise = mode;

    }

    /// <summary>
    /// Set Gear Shifting Threshold. Automatic gear will shift up at earlier rpm on lower values. Automatic gear will shift up at later rpm on higher values. 
    /// </summary>
    public static void SetGearShiftingThreshold(RCC_CarControllerV3 vehicle, float targetValue) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.gearShiftingThreshold = targetValue;

    }

    /// <summary>
    /// Set Clutch Threshold. Automatic gear will shift up at earlier rpm on lower values. Automatic gear will shift up at later rpm on higher values. 
    /// </summary>
    public static void SetClutchThreshold(RCC_CarControllerV3 vehicle, float targetValue) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.clutchInertia = targetValue;

    }

    /// <summary>
    /// Enable / Disable Counter Steering while vehicle is drifting. Useful for avoid spinning.
    /// </summary>
    public static void SetCounterSteering(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.useCounterSteering = state;

    }

    /// <summary>
    /// Enable / Disable Steering Limiter while vehicle is drifting. Useful for avoid spinning.
    /// </summary>
    public static void SetSteeringLimit(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.useSteeringLimiter = state;

    }

    /// <summary>
    /// Enable / Disable NOS.
    /// </summary>
    public static void SetNOS(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.useNOS = state;

    }

    /// <summary>
    /// Enable / Disable Turbo.
    /// </summary>
    public static void SetTurbo(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.useTurbo = state;

    }

    /// <summary>
    /// Enable / Disable Exhaust Flames.
    /// </summary>
    public static void SetUseExhaustFlame(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.useExhaustFlame = state;

    }

    /// <summary>
    /// Enable / Disable Rev Limiter.
    /// </summary>
    public static void SetRevLimiter(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.useRevLimiter = state;

    }

    /// <summary>
    /// Set Front Suspension Spring Force.
    /// </summary>
    public static void SetFrontSuspensionsSpringForce(RCC_CarControllerV3 vehicle, float targetValue) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        JointSpring spring = vehicle.FrontLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring;
        spring.spring = targetValue;
        vehicle.FrontLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring = spring;
        vehicle.FrontRightWheelCollider.GetComponent<WheelCollider>().suspensionSpring = spring;

    }

    /// <summary>
    /// Set Rear Suspension Spring Force.
    /// </summary>
    public static void SetRearSuspensionsSpringForce(RCC_CarControllerV3 vehicle, float targetValue) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        JointSpring spring = vehicle.RearLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring;
        spring.spring = targetValue;
        vehicle.RearLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring = spring;
        vehicle.RearRightWheelCollider.GetComponent<WheelCollider>().suspensionSpring = spring;

    }

    /// <summary>
    /// Set Front Suspension Spring Damper.
    /// </summary>
    public static void SetFrontSuspensionsSpringDamper(RCC_CarControllerV3 vehicle, float targetValue) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        JointSpring spring = vehicle.FrontLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring;
        spring.damper = targetValue;
        vehicle.FrontLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring = spring;
        vehicle.FrontRightWheelCollider.GetComponent<WheelCollider>().suspensionSpring = spring;

    }

    /// <summary>
    /// Set Rear Suspension Spring Damper.
    /// </summary>
    public static void SetRearSuspensionsSpringDamper(RCC_CarControllerV3 vehicle, float targetValue) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        JointSpring spring = vehicle.RearLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring;
        spring.damper = targetValue;
        vehicle.RearLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring = spring;
        vehicle.RearRightWheelCollider.GetComponent<WheelCollider>().suspensionSpring = spring;

    }

    /// <summary>
    /// Set Maximum Speed of the vehicle.
    /// </summary>
    public static void SetMaximumSpeed(RCC_CarControllerV3 vehicle, float targetValue) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.maxspeed = Mathf.Clamp(targetValue, 10f, 400f);

    }

    /// <summary>
    /// Set Maximum Engine Torque of the vehicle.
    /// </summary>
    public static void SetMaximumTorque(RCC_CarControllerV3 vehicle, float targetValue) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.maxEngineTorque = Mathf.Clamp(targetValue, 50f, 50000f);

    }

    /// <summary>
    /// Set Maximum Brake of the vehicle.
    /// </summary>
    public static void SetMaximumBrake(RCC_CarControllerV3 vehicle, float targetValue) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.brakeTorque = Mathf.Clamp(targetValue, 0f, 50000f);

    }

    /// <summary>
    /// Repair vehicle.
    /// </summary>
    public static void Repair(RCC_CarControllerV3 vehicle) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.Repair();

    }

    /// <summary>
    /// Enable / Disable ESP.
    /// </summary>
    public static void SetESP(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.ESP = state;

    }

    /// <summary>
    /// Enable / Disable ABS.
    /// </summary>
    public static void SetABS(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.ABS = state;

    }

    /// <summary>
    /// Enable / Disable TCS.
    /// </summary>
    public static void SetTCS(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.TCS = state;

    }

    /// <summary>
    /// Enable / Disable Steering Helper.
    /// </summary>
    public static void SetSH(RCC_CarControllerV3 vehicle, bool state) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.steeringHelper = state;

    }

    /// <summary>
    /// Set Steering Helper strength.
    /// </summary>
    public static void SetSHStrength(RCC_CarControllerV3 vehicle, float value) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.steeringHelper = true;
        vehicle.steerHelperLinearVelStrength = value;
        vehicle.steerHelperAngularVelStrength = value;

    }

    /// <summary>
    /// Set Transmission of the vehicle.
    /// </summary>
    public static void SetTransmission(RCC_CarControllerV3 vehicle, bool automatic) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        vehicle.AutomaticGear = automatic;

    }

    /// <summary>
    /// Save all stats with PlayerPrefs.
    /// </summary>
    public static void SaveStats(RCC_CarControllerV3 vehicle) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Saving all major settings of the vehicle with PlayerPrefs.
        PlayerPrefs.SetFloat(vehicle.transform.name + "_FrontCamber", vehicle.FrontLeftWheelCollider.camber);
        PlayerPrefs.SetFloat(vehicle.transform.name + "_RearCamber", vehicle.RearLeftWheelCollider.camber);

        PlayerPrefs.SetFloat(vehicle.transform.name + "_FrontSuspensionsDistance", vehicle.FrontLeftWheelCollider.WheelCollider.suspensionDistance);
        PlayerPrefs.SetFloat(vehicle.transform.name + "_RearSuspensionsDistance", vehicle.RearLeftWheelCollider.WheelCollider.suspensionDistance);

        PlayerPrefs.SetFloat(vehicle.transform.name + "_FrontSuspensionsSpring", vehicle.FrontLeftWheelCollider.WheelCollider.suspensionSpring.spring);
        PlayerPrefs.SetFloat(vehicle.transform.name + "_RearSuspensionsSpring", vehicle.RearLeftWheelCollider.WheelCollider.suspensionSpring.spring);

        PlayerPrefs.SetFloat(vehicle.transform.name + "_FrontSuspensionsDamper", vehicle.FrontLeftWheelCollider.WheelCollider.suspensionSpring.damper);
        PlayerPrefs.SetFloat(vehicle.transform.name + "_RearSuspensionsDamper", vehicle.RearLeftWheelCollider.WheelCollider.suspensionSpring.damper);

        PlayerPrefs.SetFloat(vehicle.transform.name + "_MaximumSpeed", vehicle.maxspeed);
        PlayerPrefs.SetFloat(vehicle.transform.name + "_MaximumBrake", vehicle.brakeTorque);
        PlayerPrefs.SetFloat(vehicle.transform.name + "_MaximumTorque", vehicle.maxEngineTorque);

        PlayerPrefs.SetString(vehicle.transform.name + "_DrivetrainMode", vehicle.wheelTypeChoise.ToString());

        PlayerPrefs.SetFloat(vehicle.transform.name + "_GearShiftingThreshold", vehicle.gearShiftingThreshold);
        PlayerPrefs.SetFloat(vehicle.transform.name + "_ClutchingThreshold", vehicle.clutchInertia);

        RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "_CounterSteering", vehicle.useCounterSteering);

        foreach (RCC_Light _light in vehicle.GetComponentsInChildren<RCC_Light>()) {

            if (_light.lightType == RCC_Light.LightType.HeadLight) {

                RCC_PlayerPrefsX.SetColor(vehicle.transform.name + "_HeadlightsColor", _light.GetComponentInChildren<Light>().color);
                break;

            }

        }

        ParticleSystem ps = vehicle.RearLeftWheelCollider.allWheelParticles[0];
        ParticleSystem.MainModule psmain = ps.main;

        RCC_PlayerPrefsX.SetColor(vehicle.transform.name + "_WheelsSmokeColor", psmain.startColor.color);

        RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "_ABS", vehicle.ABS);
        RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "_ESP", vehicle.ESP);
        RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "_TCS", vehicle.TCS);
        RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "_SH", vehicle.steeringHelper);

        RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "NOS", vehicle.useNOS);
        RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "Turbo", vehicle.useTurbo);
        RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "ExhaustFlame", vehicle.useExhaustFlame);
        RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "RevLimiter", vehicle.useRevLimiter);

    }

    /// <summary>
    /// Load all stats with PlayerPrefs.
    /// </summary>
    public static void LoadStats(RCC_CarControllerV3 vehicle) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  Loading all major settings of the vehicle with PlayerPrefs.
        SetFrontCambers(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_FrontCamber", vehicle.FrontLeftWheelCollider.camber));
        SetRearCambers(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_RearCamber", vehicle.RearLeftWheelCollider.camber));

        SetFrontSuspensionsDistances(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_FrontSuspensionsDistance", vehicle.FrontLeftWheelCollider.WheelCollider.suspensionDistance));
        SetRearSuspensionsDistances(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_RearSuspensionsDistance", vehicle.RearLeftWheelCollider.WheelCollider.suspensionDistance));

        SetFrontSuspensionsSpringForce(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_FrontSuspensionsSpring", vehicle.FrontLeftWheelCollider.WheelCollider.suspensionSpring.spring));
        SetRearSuspensionsSpringForce(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_RearSuspensionsSpring", vehicle.RearLeftWheelCollider.WheelCollider.suspensionSpring.spring));

        SetFrontSuspensionsSpringDamper(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_FrontSuspensionsDamper", vehicle.FrontLeftWheelCollider.WheelCollider.suspensionSpring.damper));
        SetRearSuspensionsSpringDamper(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_RearSuspensionsDamper", vehicle.RearLeftWheelCollider.WheelCollider.suspensionSpring.damper));

        SetMaximumSpeed(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_MaximumSpeed", vehicle.maxspeed));
        SetMaximumBrake(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_MaximumBrake", vehicle.brakeTorque));
        SetMaximumTorque(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_MaximumTorque", vehicle.maxEngineTorque));

        string drvtrn = PlayerPrefs.GetString(vehicle.transform.name + "_DrivetrainMode", vehicle.wheelTypeChoise.ToString());

        switch (drvtrn) {

            case "FWD":
                vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.FWD;
                break;

            case "RWD":
                vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.RWD;
                break;

            case "AWD":
                vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.AWD;
                break;

        }

        SetGearShiftingThreshold(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_GearShiftingThreshold", vehicle.gearShiftingThreshold));
        SetClutchThreshold(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_ClutchingThreshold", vehicle.clutchInertia));

        SetCounterSteering(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "_CounterSteering", vehicle.useCounterSteering));

        SetABS(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "_ABS", vehicle.ABS));
        SetESP(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "_ESP", vehicle.ESP));
        SetTCS(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "_TCS", vehicle.TCS));
        SetSH(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "_SH", vehicle.steeringHelper));

        SetNOS(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "NOS", vehicle.useNOS));
        SetTurbo(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "Turbo", vehicle.useTurbo));
        SetUseExhaustFlame(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "ExhaustFlame", vehicle.useExhaustFlame));
        SetRevLimiter(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "RevLimiter", vehicle.useRevLimiter));

        if (PlayerPrefs.HasKey(vehicle.transform.name + "_WheelsSmokeColor"))
            SetSmokeColor(vehicle, 0, RCC_PlayerPrefsX.GetColor(vehicle.transform.name + "_WheelsSmokeColor"));

        if (PlayerPrefs.HasKey(vehicle.transform.name + "_HeadlightsColor"))
            SetHeadlightsColor(vehicle, RCC_PlayerPrefsX.GetColor(vehicle.transform.name + "_HeadlightsColor"));

    }

    /// <summary>
    /// Resets all stats and saves default values with PlayerPrefs.
    /// </summary>
    public static void ResetStats(RCC_CarControllerV3 vehicle, RCC_CarControllerV3 defaultCar) {

        //  If no vehicle found, return.
        if (!CheckVehicle(vehicle))
            return;

        //  If no default vehicle found, return.
        if (!CheckVehicle(defaultCar))
            return;

        SetFrontCambers(vehicle, defaultCar.FrontLeftWheelCollider.camber);
        SetRearCambers(vehicle, defaultCar.RearLeftWheelCollider.camber);

        SetFrontSuspensionsDistances(vehicle, defaultCar.FrontLeftWheelCollider.WheelCollider.suspensionDistance);
        SetRearSuspensionsDistances(vehicle, defaultCar.RearLeftWheelCollider.WheelCollider.suspensionDistance);

        SetFrontSuspensionsSpringForce(vehicle, defaultCar.FrontLeftWheelCollider.WheelCollider.suspensionSpring.spring);
        SetRearSuspensionsSpringForce(vehicle, defaultCar.RearLeftWheelCollider.WheelCollider.suspensionSpring.spring);

        SetFrontSuspensionsSpringDamper(vehicle, defaultCar.FrontLeftWheelCollider.WheelCollider.suspensionSpring.damper);
        SetRearSuspensionsSpringDamper(vehicle, defaultCar.RearLeftWheelCollider.WheelCollider.suspensionSpring.damper);

        SetMaximumSpeed(vehicle, defaultCar.maxspeed);
        SetMaximumBrake(vehicle, defaultCar.brakeTorque);
        SetMaximumTorque(vehicle, defaultCar.maxEngineTorque);

        string drvtrn = defaultCar.wheelTypeChoise.ToString();

        switch (drvtrn) {

            case "FWD":
                vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.FWD;
                break;

            case "RWD":
                vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.RWD;
                break;

            case "AWD":
                vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.AWD;
                break;

        }

        SetGearShiftingThreshold(vehicle, defaultCar.gearShiftingThreshold);
        SetClutchThreshold(vehicle, defaultCar.clutchInertia);

        SetCounterSteering(vehicle, defaultCar.useCounterSteering);

        SetABS(vehicle, defaultCar.ABS);
        SetESP(vehicle, defaultCar.ESP);
        SetTCS(vehicle, defaultCar.TCS);
        SetSH(vehicle, defaultCar.steeringHelper);

        SetNOS(vehicle, defaultCar.useNOS);
        SetTurbo(vehicle, defaultCar.useTurbo);
        SetUseExhaustFlame(vehicle, defaultCar.useExhaustFlame);
        SetRevLimiter(vehicle, defaultCar.useRevLimiter);

        SetSmokeColor(vehicle, 0, Color.white);
        SetHeadlightsColor(vehicle, Color.white);

        SaveStats(vehicle);

    }

    /// <summary>
    /// Checks the player vehicle.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public static bool CheckVehicle(RCC_CarControllerV3 vehicle) {

        //  If no vehicle found, return with an error.
        if (!vehicle) {

            Debug.LogError("Vehicle is missing!");
            return false;

        }

        return true;

    }

}
