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
using UnityEngine.Audio;

public class RCC_Core : MonoBehaviour {

    #region Create AudioSource

    /// <summary>
    /// Creates new audiosource with specified settings.
    /// </summary>
	public static AudioSource NewAudioSource(AudioMixerGroup audioMixer, GameObject go, string audioName, float minDistance, float maxDistance, float volume, AudioClip audioClip, bool loop, bool playNow, bool destroyAfterFinished) {

        GameObject audioSourceObject = new GameObject(audioName);

        if (go.transform.Find("All Audio Sources")) {

            audioSourceObject.transform.SetParent(go.transform.Find("All Audio Sources"));

        } else {

            GameObject allAudioSources = new GameObject("All Audio Sources");
            allAudioSources.transform.SetParent(go.transform, false);
            audioSourceObject.transform.SetParent(allAudioSources.transform, false);

        }

        audioSourceObject.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);

        audioSourceObject.AddComponent<AudioSource>();
        AudioSource source = audioSourceObject.GetComponent<AudioSource>();

        if (audioMixer)
            source.outputAudioMixerGroup = audioMixer;

        //audioSource.GetComponent<AudioSource>().priority =1;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.volume = volume;
        source.clip = audioClip;
        source.loop = loop;
        source.dopplerLevel = .5f;
        source.ignoreListenerPause = false;
        source.ignoreListenerVolume = false;

        if (minDistance == 0 && maxDistance == 0)
            source.spatialBlend = 0f;
        else
            source.spatialBlend = 1f;

        if (playNow) {

            source.playOnAwake = true;
            source.Play();

        } else {

            source.playOnAwake = false;

        }

        if (destroyAfterFinished) {

            if (audioClip)
                Destroy(audioSourceObject, audioClip.length);
            else
                Destroy(audioSourceObject);

        }

        return source;

    }

    /// <summary>
    /// Creates new audiosource with specified settings.
    /// </summary>
    public static AudioSource NewAudioSource(GameObject go, string audioName, float minDistance, float maxDistance, float volume, AudioClip audioClip, bool loop, bool playNow, bool destroyAfterFinished) {

        GameObject audioSourceObject = new GameObject(audioName);

        if (go.transform.Find("All Audio Sources")) {

            audioSourceObject.transform.SetParent(go.transform.Find("All Audio Sources"));

        } else {

            GameObject allAudioSources = new GameObject("All Audio Sources");
            allAudioSources.transform.SetParent(go.transform, false);
            audioSourceObject.transform.SetParent(allAudioSources.transform, false);

        }

        audioSourceObject.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);

        audioSourceObject.AddComponent<AudioSource>();
        AudioSource source = audioSourceObject.GetComponent<AudioSource>();

        //audioSource.GetComponent<AudioSource>().priority =1;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.volume = volume;
        source.clip = audioClip;
        source.loop = loop;
        source.dopplerLevel = .5f;

        if (minDistance == 0 && maxDistance == 0)
            source.spatialBlend = 0f;
        else
            source.spatialBlend = 1f;

        if (playNow) {

            source.playOnAwake = true;
            source.Play();

        } else {

            source.playOnAwake = false;

        }

        if (destroyAfterFinished) {

            if (audioClip)
                Destroy(audioSourceObject, audioClip.length);
            else
                Destroy(audioSourceObject);

        }

        return source;

    }

    /// <summary>
    /// Creates new audiosource with specified settings.
    /// </summary>
    public static AudioSource NewAudioSource(AudioMixerGroup audioMixer, GameObject go, Vector3 localPosition, string audioName, float minDistance, float maxDistance, float volume, AudioClip audioClip, bool loop, bool playNow, bool destroyAfterFinished) {

        GameObject audioSourceObject = new GameObject(audioName);

        if (go.transform.Find("All Audio Sources")) {

            audioSourceObject.transform.SetParent(go.transform.Find("All Audio Sources"));

        } else {

            GameObject allAudioSources = new GameObject("All Audio Sources");
            allAudioSources.transform.SetParent(go.transform, false);
            audioSourceObject.transform.SetParent(allAudioSources.transform, false);

        }

        audioSourceObject.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);
        audioSourceObject.transform.localPosition = localPosition;

        audioSourceObject.AddComponent<AudioSource>();
        AudioSource source = audioSourceObject.GetComponent<AudioSource>();

        if (audioMixer)
            source.outputAudioMixerGroup = audioMixer;

        //audioSource.GetComponent<AudioSource>().priority =1;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.volume = volume;
        source.clip = audioClip;
        source.loop = loop;
        source.dopplerLevel = .5f;

        if (minDistance == 0 && maxDistance == 0)
            source.spatialBlend = 0f;
        else
            source.spatialBlend = 1f;

        if (playNow) {

            source.playOnAwake = true;
            source.Play();

        } else {

            source.playOnAwake = false;

        }

        if (destroyAfterFinished) {

            if (audioClip)
                Destroy(audioSourceObject, audioClip.length);
            else
                Destroy(audioSourceObject);

        }

        return source;

    }

    /// <summary>
    /// Creates new audiosource with specified settings.
    /// </summary>
    public static AudioSource NewAudioSource(GameObject go, Vector3 localPosition, string audioName, float minDistance, float maxDistance, float volume, AudioClip audioClip, bool loop, bool playNow, bool destroyAfterFinished) {

        GameObject audioSourceObject = new GameObject(audioName);

        if (go.transform.Find("All Audio Sources")) {

            audioSourceObject.transform.SetParent(go.transform.Find("All Audio Sources"));

        } else {

            GameObject allAudioSources = new GameObject("All Audio Sources");
            allAudioSources.transform.SetParent(go.transform, false);
            audioSourceObject.transform.SetParent(allAudioSources.transform, false);

        }

        audioSourceObject.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);
        audioSourceObject.transform.localPosition = localPosition;

        audioSourceObject.AddComponent<AudioSource>();
        AudioSource source = audioSourceObject.GetComponent<AudioSource>();

        //audioSource.GetComponent<AudioSource>().priority =1;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.volume = volume;
        source.clip = audioClip;
        source.loop = loop;
        source.dopplerLevel = .5f;

        if (minDistance == 0 && maxDistance == 0)
            source.spatialBlend = 0f;
        else
            source.spatialBlend = 1f;

        if (playNow) {

            source.playOnAwake = true;
            source.Play();

        } else {

            source.playOnAwake = false;

        }

        if (destroyAfterFinished) {

            if (audioClip)
                Destroy(audioSourceObject, audioClip.length);
            else
                Destroy(audioSourceObject);

        }

        return source;

    }

    /// <summary>
    /// Adds High Pass Filter to audiosource. Used for turbo.
    /// </summary>
    public static void NewHighPassFilter(AudioSource source, float freq, int level) {

        if (source == null)
            return;

        AudioHighPassFilter highFilter = source.gameObject.AddComponent<AudioHighPassFilter>();
        highFilter.cutoffFrequency = freq;
        highFilter.highpassResonanceQ = level;

    }

    /// <summary>
    /// Adds Low Pass Filter to audiosource. Used for engine off sounds.
    /// </summary>
    public static void NewLowPassFilter(AudioSource source, float freq) {

        if (source == null)
            return;

        AudioLowPassFilter lowFilter = source.gameObject.AddComponent<AudioLowPassFilter>();
        lowFilter.cutoffFrequency = freq;
        //      lowFilter.highpassResonanceQ = level;

    }

    #endregion

    #region Create WheelColliders

    /// <summary>
    /// Creates the wheel colliders.
    /// </summary>
    public void CreateWheelColliders(RCC_CarControllerV3 carController) {

        // Creating a list for all wheel models.
        List<Transform> allWheelModels = new List<Transform>();
        allWheelModels.Add(carController.FrontLeftWheelTransform); allWheelModels.Add(carController.FrontRightWheelTransform); allWheelModels.Add(carController.RearLeftWheelTransform); allWheelModels.Add(carController.RearRightWheelTransform);

        // If we have additional rear wheels, add them too.
        if (carController.ExtraRearWheelsTransform != null && carController.ExtraRearWheelsTransform.Length > 0 && carController.ExtraRearWheelsTransform[0] != null) {

            foreach (Transform t in carController.ExtraRearWheelsTransform)
                allWheelModels.Add(t);

        }

        // If we don't have any wheelmodels, throw an error.
        bool missingWheelFound = false;

        for (int i = 0; i < allWheelModels.Count; i++) {

            if (allWheelModels[i] == null) {

                missingWheelFound = true;
                break;

            }

        }

        if (missingWheelFound) {

            Debug.LogError("You haven't choosen your Wheel Models. Please select all of your Wheel Models before creating Wheel Colliders. Script needs to know their sizes and positions, aye?");
            return;

        }

        // Holding default rotation.
        Quaternion currentRotation = carController.transform.rotation;

        // Resetting rotation.
        carController.transform.rotation = Quaternion.identity;

        // Creating a new gameobject called Wheel Colliders for all Wheel Colliders, and parenting it to this gameobject.
        GameObject WheelColliders = new GameObject("Wheel Colliders");
        WheelColliders.transform.SetParent(carController.transform, false);
        WheelColliders.transform.localRotation = Quaternion.identity;
        WheelColliders.transform.localPosition = Vector3.zero;
        WheelColliders.transform.localScale = Vector3.one;

        // Creating WheelColliders.
        foreach (Transform wheel in allWheelModels) {

            GameObject wheelcollider = new GameObject(wheel.transform.name);

            wheelcollider.transform.SetPositionAndRotation(RCC_GetBounds.GetBoundsCenter(wheel.transform), carController.transform.rotation);
            wheelcollider.transform.name = wheel.transform.name;
            wheelcollider.transform.SetParent(WheelColliders.transform);
            wheelcollider.transform.localScale = Vector3.one;
            wheelcollider.AddComponent<WheelCollider>();
            wheelcollider.AddComponent<RCC_WheelCollider>();

            Bounds biggestBound = new Bounds();
            Renderer[] renderers = wheel.GetComponentsInChildren<Renderer>();

            foreach (Renderer render in renderers) {
                if (render != GetComponent<Renderer>()) {
                    if (render.bounds.size.z > biggestBound.size.z)
                        biggestBound = render.bounds;
                }
            }

            wheelcollider.GetComponent<WheelCollider>().radius = (biggestBound.extents.y) / carController.transform.localScale.y;
            JointSpring spring = wheelcollider.GetComponent<WheelCollider>().suspensionSpring;

            spring.spring = RCC_InitialSettings.Instance.suspensionSpring;
            spring.damper = RCC_InitialSettings.Instance.suspensionDamping;
            spring.targetPosition = .5f;

            wheelcollider.GetComponent<WheelCollider>().suspensionSpring = spring;
            wheelcollider.GetComponent<WheelCollider>().suspensionDistance = RCC_InitialSettings.Instance.suspensionDistance;
            wheelcollider.GetComponent<WheelCollider>().forceAppPointDistance = RCC_InitialSettings.Instance.forceAppPoint;
            wheelcollider.GetComponent<WheelCollider>().mass = 40f;
            wheelcollider.GetComponent<WheelCollider>().wheelDampingRate = 1f;

            WheelFrictionCurve sidewaysFriction;
            WheelFrictionCurve forwardFriction;

            sidewaysFriction = wheelcollider.GetComponent<WheelCollider>().sidewaysFriction;
            forwardFriction = wheelcollider.GetComponent<WheelCollider>().forwardFriction;

            forwardFriction.extremumSlip = RCC_InitialSettings.Instance.forwardExtremumSlip;
            forwardFriction.extremumValue = RCC_InitialSettings.Instance.forwardExtremumValue;
            forwardFriction.asymptoteSlip = RCC_InitialSettings.Instance.forwardAsymptoteSlip;
            forwardFriction.asymptoteValue = RCC_InitialSettings.Instance.forwardAsymptoteValue;
            forwardFriction.stiffness = RCC_InitialSettings.Instance.forwardStiffness;

            sidewaysFriction.extremumSlip = RCC_InitialSettings.Instance.sidewaysExtremumSlip;
            sidewaysFriction.extremumValue = RCC_InitialSettings.Instance.sidewaysExtremumValue;
            sidewaysFriction.asymptoteSlip = RCC_InitialSettings.Instance.sidewaysAsymptoteSlip;
            sidewaysFriction.asymptoteValue = RCC_InitialSettings.Instance.sidewaysAsymptoteValue;
            sidewaysFriction.stiffness = RCC_InitialSettings.Instance.sidewaysStiffness;

            wheelcollider.GetComponent<WheelCollider>().sidewaysFriction = sidewaysFriction;
            wheelcollider.GetComponent<WheelCollider>().forwardFriction = forwardFriction;

        }

        RCC_WheelCollider[] allWheelColliders = new RCC_WheelCollider[allWheelModels.Count];
        allWheelColliders = GetComponentsInChildren<RCC_WheelCollider>();

        carController.FrontLeftWheelCollider = allWheelColliders[0];
        carController.FrontRightWheelCollider = allWheelColliders[1];
        carController.RearLeftWheelCollider = allWheelColliders[2];
        carController.RearRightWheelCollider = allWheelColliders[3];

        if (carController.ExtraRearWheelsTransform != null) {

            carController.ExtraRearWheelsCollider = new RCC_WheelCollider[carController.ExtraRearWheelsTransform.Length];

            for (int i = 0; i < carController.ExtraRearWheelsTransform.Length; i++)
                carController.ExtraRearWheelsCollider[i] = allWheelColliders[i + 4];

        }

        carController.transform.rotation = currentRotation;

    }

    #endregion

    #region Set Behavior

    /// <summary>
    /// Overrides the behavior.
    /// </summary>
    public void SetBehavior(RCC_CarControllerV3 carController) {

        if (RCC_Settings.Instance.selectedBehaviorType == null)
            return;

        RCC_Settings.BehaviorType currentBehaviorType = RCC_Settings.Instance.selectedBehaviorType;

        carController.steeringHelper = currentBehaviorType.steeringHelper;
        carController.tractionHelper = currentBehaviorType.tractionHelper;
        carController.angularDragHelper = currentBehaviorType.angularDragHelper;
        carController.useSteeringLimiter = currentBehaviorType.limitSteering;
        carController.useSteeringSensitivity = currentBehaviorType.steeringSensitivity;
        carController.steeringSensitivityFactor = Mathf.Clamp(carController.steeringSensitivityFactor, currentBehaviorType.steeringSensitivityMinimum, currentBehaviorType.steeringSensitivityMaximum);
        carController.steeringType = currentBehaviorType.steeringType;
        carController.COMAssister = currentBehaviorType.comAssister;

        if (carController.steeringType == RCC_CarControllerV3.SteeringType.Curve)
            carController.steerAngleCurve = currentBehaviorType.steeringCurve;

        carController.useCounterSteering = currentBehaviorType.counterSteering;
        carController.ABS = currentBehaviorType.ABS;
        carController.ESP = currentBehaviorType.ESP;
        carController.TCS = currentBehaviorType.TCS;

        carController.highspeedsteerAngle = Mathf.Clamp(carController.highspeedsteerAngle, currentBehaviorType.highSpeedSteerAngleMinimum, currentBehaviorType.highSpeedSteerAngleMaximum);
        carController.highspeedsteerAngleAtspeed = Mathf.Clamp(carController.highspeedsteerAngleAtspeed, currentBehaviorType.highSpeedSteerAngleAtspeedMinimum, currentBehaviorType.highSpeedSteerAngleAtspeedMaximum);
        carController.counterSteeringFactor = Mathf.Clamp(carController.counterSteeringFactor, currentBehaviorType.counterSteeringMinimum, currentBehaviorType.counterSteeringMaximum);
        carController.counterSteerInput = 0f;

        carController.steerHelperAngularVelStrength = Mathf.Clamp(carController.steerHelperAngularVelStrength, currentBehaviorType.steerHelperAngularVelStrengthMinimum, currentBehaviorType.steerHelperAngularVelStrengthMaximum);
        carController.steerHelperLinearVelStrength = Mathf.Clamp(carController.steerHelperLinearVelStrength, currentBehaviorType.steerHelperLinearVelStrengthMinimum, currentBehaviorType.steerHelperLinearVelStrengthMaximum);

        carController.tractionHelperStrength = Mathf.Clamp(carController.tractionHelperStrength, currentBehaviorType.tractionHelperStrengthMinimum, currentBehaviorType.tractionHelperStrengthMaximum);
        carController.antiRollFrontHorizontal = Mathf.Clamp(carController.antiRollFrontHorizontal, currentBehaviorType.antiRollFrontHorizontalMinimum, Mathf.Infinity);
        carController.antiRollRearHorizontal = Mathf.Clamp(carController.antiRollRearHorizontal, currentBehaviorType.antiRollRearHorizontalMinimum, Mathf.Infinity);

        carController.gearShiftingDelay = Mathf.Clamp(carController.gearShiftingDelay, 0f, currentBehaviorType.gearShiftingDelayMaximum);
        carController.Rigid.angularDrag = currentBehaviorType.angularDrag;

        carController.angularDragHelperStrength = Mathf.Clamp(carController.angularDragHelperStrength, currentBehaviorType.angularDragHelperMinimum, currentBehaviorType.angularDragHelperMaximum);

    }

    #endregion

}

