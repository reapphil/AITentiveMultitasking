//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Stored all general shared RCC settings here.
/// </summary>
[System.Serializable]
public class RCC_Settings : ScriptableObject {

    #region singleton
    private static RCC_Settings instance;
    public static RCC_Settings Instance { get { if (instance == null) instance = Resources.Load("RCC Assets/RCC_Settings") as RCC_Settings; return instance; } }
    #endregion

    public int behaviorSelectedIndex = 0;       //  Current selected behavior index.

    public BehaviorType selectedBehaviorType {

        get {

            if (overrideBehavior)
                return behaviorTypes[behaviorSelectedIndex];
            else
                return null;

        }

    }

    public bool overrideBehavior = true;
    public bool overrideFPS = true;     //  Override FPS?
    public bool overrideFixedTimeStep = true;       //  Override fixed timestep?
    [Range(.005f, .06f)] public float fixedTimeStep = .02f;     //  Overrided fixed timestep value.
    [Range(.5f, 20f)] public float maxAngularVelocity = 6;      //  Maximum angular velocity.
    public int maxFPS = 60;     //  Maximum FPS.
    public bool useShortcuts = false;       //  Use shortcuts. Shift + E = In-Scene GUI, Shift + R = Add Main Car Controller, Shift + S = RCC Settings.

    /// <summary>
    /// Behavior Types
    /// </summary>
    [System.Serializable]
    public class BehaviorType {

        public string behaviorName = "New Behavior";        //  Behavior name.

        //  Driving helpers.
        [Header("Steering Helpers")]
        public bool steeringHelper = true;
        public bool tractionHelper = true;
        public bool angularDragHelper = false;
        public bool counterSteering = true;
        public bool limitSteering = true;
        public bool steeringSensitivity = true;
        public bool ABS = false;
        public bool ESP = false;
        public bool TCS = false;
        public bool applyExternalWheelFrictions = false;
        public bool applyRelativeTorque = false;

        //  Steering.
        public RCC_CarControllerV3.SteeringType steeringType = RCC_CarControllerV3.SteeringType.Curve;
        public AnimationCurve steeringCurve = new AnimationCurve(new Keyframe(0f, 40f), new Keyframe(50f, 20f), new Keyframe(100f, 11f), new Keyframe(150f, 6f), new Keyframe(200f, 5f));

        public RCC_CarControllerV3.COMAssisterTypes comAssister = RCC_CarControllerV3.COMAssisterTypes.Off;

        //  High speed steer angle limitations.
        [Space()]
        public float highSpeedSteerAngleMinimum = 20f;
        public float highSpeedSteerAngleMaximum = 40f;

        //  High speed steer angle at speed limitations.
        public float highSpeedSteerAngleAtspeedMinimum = 100f;
        public float highSpeedSteerAngleAtspeedMaximum = 200f;

        //  Counter steering limitations.
        [Space()]
        public float counterSteeringMinimum = .1f;
        public float counterSteeringMaximum = 1f;

        //  Steering sensitivity limitations.
        [Space()]
        public float steeringSensitivityMinimum = .5f;
        public float steeringSensitivityMaximum = 1f;

        //  Steering helper angular velocity limitations.
        [Space()]
        [Range(0f, 1f)] public float steerHelperAngularVelStrengthMinimum = .1f;
        [Range(0f, 1f)] public float steerHelperAngularVelStrengthMaximum = 1;

        //  Steering helper linear velocity limitations.
        [Range(0f, 1f)] public float steerHelperLinearVelStrengthMinimum = .1f;
        [Range(0f, 1f)] public float steerHelperLinearVelStrengthMaximum = 1f;

        //  Traction helper strength limitations.
        [Range(0f, 1f)] public float tractionHelperStrengthMinimum = .1f;
        [Range(0f, 1f)] public float tractionHelperStrengthMaximum = 1f;

        //  Anti roll horizontal limitations.
        [Space()]
        public float antiRollFrontHorizontalMinimum = 1000f;
        public float antiRollRearHorizontalMinimum = 1000f;

        //  Gear shifting delat limitation.
        [Space()]
        [Range(0f, 1f)] public float gearShiftingDelayMaximum = .15f;

        //  Angular drag limitations.
        [Range(0f, 10f)] public float angularDrag = .1f;
        [Range(0f, 1f)] public float angularDragHelperMinimum = .1f;
        [Range(0f, 1f)] public float angularDragHelperMaximum = 1f;

        //  Wheel frictions.
        [Header("Wheel Frictions Forward")]
        public float forwardExtremumSlip = .4f;
        public float forwardExtremumValue = 1f;
        public float forwardAsymptoteSlip = .8f;
        public float forwardAsymptoteValue = .5f;

        [Header("Wheel Frictions Sideways")]
        public float sidewaysExtremumSlip = .2f;
        public float sidewaysExtremumValue = 1f;
        public float sidewaysAsymptoteSlip = .5f;
        public float sidewaysAsymptoteValue = .75f;

    }

    public bool useFixedWheelColliders = true;      //  Fixed wheelcolliders with higher mass will be used.
    public bool lockAndUnlockCursor = true;     //  Locks cursor.

    // Behavior Types
    public BehaviorType[] behaviorTypes;

    // Main Controller Settings
    public bool useAutomaticGear = true;        //  All vehicles will use automatic gear.
    public bool useAutomaticClutch = true;      //  All vehicles will use automatic clutch.
    public bool runEngineAtAwake = true;        //  All vehicles will start with engine running.
    public bool autoReverse = true;     //  All vehicles can go reverse while pressing brake.
    public bool autoReset = true;       //  All vehicles can be resetted if upside down.

    //  Particles.
    public GameObject contactParticles;
    public GameObject scratchParticles;
    public GameObject wheelDeflateParticles;

    //  Units as kmh or mph.
    public Units units = Units.KMH;
    public enum Units { KMH, MPH }

    // UI Dashboard Type
    public UIType uiType = UIType.UI;
    public enum UIType { UI, NGUI, None }

    // Information telemetry about current vehicle
    public bool useTelemetry = false;

    // For mobile inputs
    public enum MobileController { TouchScreen, Gyro, SteeringWheel, Joystick }
    public MobileController mobileController = MobileController.TouchScreen;
    public bool mobileControllerEnabled = false;

    // Mobile controller buttons and accelerometer sensitivity
    public float UIButtonSensitivity = 10f;
    public float UIButtonGravity = 10f;
    public float gyroSensitivity = 2f;

    // Used for using the lights more efficent and realistic
    public bool useHeadLightsAsVertexLights = false;
    public bool useBrakeLightsAsVertexLights = true;
    public bool useReverseLightsAsVertexLights = true;
    public bool useIndicatorLightsAsVertexLights = true;
    public bool useOtherLightsAsVertexLights = true;

    public bool setLayers = true;       //  Setting layers.
    public string RCCLayer = "RCC";     //  Layer of the vehicle.
    public string WheelColliderLayer = "RCC_WheelCollider";     //  Wheelcollider layer.
    public string DetachablePartLayer = "RCC_DetachablePart";       //  Detachable part's layer.

    //  Other prefabs.
    public GameObject exhaustGas;
    public RCC_SkidmarksManager skidmarksManager;

    // Light prefabs.
    public GameObject headLights;
    public GameObject brakeLights;
    public GameObject reverseLights;
    public GameObject indicatorLights;
    public GameObject lightTrailers;
    public GameObject mirrors;

    //  Camera prefabs.
    public RCC_Camera RCCMainCamera;
    public GameObject hoodCamera;
    public GameObject cinematicCamera;

    //  UI prefabs.
    public GameObject RCCCanvas;
    public GameObject RCCTelemetry;
    public GameObject RCCModificationCanvas;

    public bool dontUseAnyParticleEffects = false;      //  Particles will not be used.
    public bool dontUseSkidmarks = false;       //  Skidmarks will not be used.

    // Sound FX.
    public AudioMixerGroup audioMixer;
    public AudioClip[] gearShiftingClips;
    public AudioClip[] crashClips;
    public AudioClip reversingClip;
    public AudioClip windClip;
    public AudioClip brakeClip;
    public AudioClip wheelDeflateClip;
    public AudioClip wheelInflateClip;
    public AudioClip wheelFlatClip;
    public AudioClip indicatorClip;
    public AudioClip bumpClip;
    public AudioClip NOSClip;
    public AudioClip turboClip;
    public AudioClip[] blowoutClip;
    public AudioClip[] exhaustFlameClips;

    //  Volume limitations.
    [Range(0f, 1f)] public float maxGearShiftingSoundVolume = .25f;
    [Range(0f, 1f)] public float maxCrashSoundVolume = 1f;
    [Range(0f, 1f)] public float maxWindSoundVolume = .1f;
    [Range(0f, 1f)] public float maxBrakeSoundVolume = .1f;

    // Used for folding sections of RCC Settings.
    public bool foldGeneralSettings = false;
    public bool foldBehaviorSettings = false;
    public bool foldControllerSettings = false;
    public bool foldUISettings = false;
    public bool foldWheelPhysics = false;
    public bool foldSFX = false;
    public bool foldOptimization = false;
    public bool foldTagsAndLayers = false;

}
