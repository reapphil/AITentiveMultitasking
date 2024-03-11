//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------


using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Based on Unity's WheelCollider. Modifies forward and sideways curves, settings in order to get stable and realistic physics depends on selected behavior in RCC Settings.
/// </summary>
[RequireComponent(typeof(WheelCollider))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Main/RCC Wheel Collider")]
public class RCC_WheelCollider : RCC_Core {

    #region OBSOLETE VARIABLES

    [System.Obsolete("Use CarController instead of carController")]
    public RCC_CarControllerV3 carController {

        get {

            return CarController;

        }

    }

    [System.Obsolete("Use WheelCollider instead of wheelCollider")]
    public WheelCollider wheelCollider {

        get {

            return WheelCollider;

        }

    }

    [System.Obsolete("Use Rigid instead of rigid")]
    public Rigidbody rigid {

        get {

            return Rigid;

        }

    }

    #endregion

    // Car controller.
    public RCC_CarControllerV3 CarController {
        get {
            if (_carController == null)
                _carController = GetComponentInParent<RCC_CarControllerV3>();
            return _carController;
        }
    }
    private RCC_CarControllerV3 _carController;

    // WheelCollider.
    public WheelCollider WheelCollider {
        get {
            if (_wheelCollider == null)
                _wheelCollider = GetComponent<WheelCollider>();
            return _wheelCollider;
        }
    }
    private WheelCollider _wheelCollider;

    // Rigidbody of the vehicle.
    private Rigidbody Rigid {
        get {
            if (_rigid == null)
                _rigid = CarController.Rigid;
            return _rigid;
        }
    }
    private Rigidbody _rigid;

    public Transform wheelModel;        // Wheel model for animating and aligning.

    public WheelHit wheelHit;       //  Wheelhit data.
    public bool isGrounded = false;     //  Is wheel grounded or not?
    public int groundIndex = 0;        //  Current ground index of wheelhit.

    public bool alignWheel = true;      //  Align the wheelmodel with wheelcollider position and rotation.
    public bool drawSkid = true;        //  Draw skidmarks.

    // Locating correct position and rotation for the wheel.
    [HideInInspector] public Vector3 wheelPosition = Vector3.zero;
    [HideInInspector] public Quaternion wheelRotation = Quaternion.identity;

    [Space()]
    public bool canPower = false;       //	Can this wheel apply power?
    [Range(-1f, 1f)] public float powerMultiplier = 1f;
    public bool canSteer = false;       //	Can this wheel apply steer?
    [Range(-1f, 1f)] public float steeringMultiplier = 1f;
    public bool canBrake = false;       //	Can this wheel apply brake?
    [Range(0f, 1f)] public float brakingMultiplier = 1f;
    public bool canHandbrake = false;       //	Can this wheel apply handbrake?
    [Range(0f, 1f)] public float handbrakeMultiplier = 1f;

    [Space()]
    public float wheelWidth = .275f; //	Width of the wheel.
    public float wheelOffset = 0f;     // Offset by X axis.

    private float wheelRPM2Speed = 0f;     // Wheel RPM to Speed in km/h unit.

    [Space()]
    [Range(-5f, 5f)] public float camber = 0f;      // Camber angle.
    [Range(-5f, 5f)] public float caster = 0f;      // Caster angle.
    [Range(-5f, 5f)] public float toe = 0f;              // Toe angle.
    [Space()]

    //	Skidmarks
    private int lastSkidmark = -1;

    //	Slips
    [HideInInspector] public float wheelSlipAmountForward = 0f;       // Forward slip.
    [HideInInspector] public float wheelSlipAmountSideways = 0f;  // Sideways slip.
    [HideInInspector] public float totalSlip = 0f;                              // Total amount of forward and sideways slips.

    //	WheelFriction Curves and Stiffness.
    private WheelFrictionCurve forwardFrictionCurve;        //	Forward friction curve.
    private WheelFrictionCurve sidewaysFrictionCurve;   //	Sideways friction curve.

    //	Original WheelFriction Curves and Stiffness.
    private WheelFrictionCurve forwardFrictionCurve_Org;        //	Forward friction curve original.
    private WheelFrictionCurve sidewaysFrictionCurve_Org;   //	Sideways friction curve original.

    //	Audio
    private AudioSource audioSource;        // Audiosource for tire skid SFX.
    private AudioClip audioClip;                    // Audioclip for tire skid SFX.
    private float audioVolume = 1f;         //	Maximum volume for tire skid SFX.

    // List for all particle systems.
    [HideInInspector] public List<ParticleSystem> allWheelParticles = new List<ParticleSystem>();
    private ParticleSystem.EmissionModule emission;

    //	Tractions used for smooth drifting.
    [HideInInspector] public float tractionHelpedSidewaysStiffness = 1f;
    private readonly float minForwardStiffness = .9f;
    private readonly float maxForwardStiffness = 1f;
    private readonly float minSidewaysStiffness = .5f;
    private readonly float maxSidewaysStiffness = 1f;

    // Getting bump force.
    [HideInInspector] public float bumpForce, oldForce, RotationValue = 0f;

    private bool deflated = false;      //  Deflated or not?

    [Space()]
    public float deflateRadiusMultiplier = .8f;     //  Deflated radius multiplier. Radius of the wheelcollider will be multiplied by this value on deflate.
    public float deflatedStiffnessMultiplier = .5f;     //  Deflated stiffness of the wheelcollider.
    private float defRadius = -1f;      //  Original radius of the wheelcollider.

    //  Getting audioclips from the RCC Settings.
    public AudioClip DeflateAudio {

        get {

            return RCC_Settings.Instance.wheelDeflateClip;

        }

    }
    public AudioClip InflateAudio {

        get {

            return RCC_Settings.Instance.wheelInflateClip;

        }

    }

    private AudioSource flatSource;
    public AudioClip FlatAudio {

        get {

            return RCC_Settings.Instance.wheelFlatClip;

        }

    }

    private ParticleSystem _wheelDeflateParticles;
    public ParticleSystem WheelDeflateParticles {

        get {

            return RCC_Settings.Instance.wheelDeflateParticles.GetComponent<ParticleSystem>();

        }

    }

    private void Start() {

        // Increasing wheelcollider mass for avoiding unstable behavior.
        if (RCC_Settings.Instance.useFixedWheelColliders)
            WheelCollider.mass = Rigid.mass / 15f;

        CreatePivotOfTheWheel();
        UpdateWheelFrictions();
        OverrideWheelSettings();
        CreateAudio();
        CreateParticles();

    }

    /// <summary>
    /// Creating pivot point of the wheel.
    /// </summary>
    private void CreatePivotOfTheWheel() {

        //	Creating pivot position of the wheel at correct position and rotation.
        GameObject newPivot = new GameObject("Pivot_" + wheelModel.transform.name);
        newPivot.transform.position = RCC_GetBounds.GetBoundsCenter(wheelModel.transform);
        newPivot.transform.rotation = transform.rotation;
        newPivot.transform.SetParent(wheelModel.transform.parent, true);

        //	Assigning temporary created wheel to actual wheel.
        wheelModel.SetParent(newPivot.transform, true);
        wheelModel = newPivot.transform;

    }

    /// <summary>
    /// Creating pacticles.
    /// </summary>
    private void CreateParticles() {

        if (RCC_Settings.Instance.dontUseAnyParticleEffects)
            return;

        for (int i = 0; i < RCC_GroundMaterials.Instance.frictions.Length; i++) {

            GameObject ps = Instantiate(RCC_GroundMaterials.Instance.frictions[i].groundParticles, transform.position, transform.rotation);
            emission = ps.GetComponent<ParticleSystem>().emission;
            emission.enabled = false;
            ps.transform.SetParent(transform, false);
            ps.transform.localPosition = Vector3.zero;
            ps.transform.localRotation = Quaternion.identity;
            allWheelParticles.Add(ps.GetComponent<ParticleSystem>());

        }

    }

    /// <summary>
    /// Creating audiosource.
    /// </summary>
    private void CreateAudio() {

        // Creating audiosource for skid SFX.
        audioSource = NewAudioSource(RCC_Settings.Instance.audioMixer, CarController.gameObject, "Skid Sound AudioSource", 5f, 50f, 0f, audioClip, true, true, false);
        audioSource.transform.position = transform.position;

    }

    /// <summary>
    /// Overriding wheel settings.
    /// </summary>
    private void OverrideWheelSettings() {

        // Override wheels automatically if enabled.
        if (!CarController.overrideAllWheels) {

            // Overriding canPower, canSteer, canBrake, canHandbrake.
            if (this == CarController.FrontLeftWheelCollider || this == CarController.FrontRightWheelCollider) {

                canSteer = true;
                canBrake = true;
                brakingMultiplier = 1f;

            }

            // Overriding canPower, canSteer, canBrake, canHandbrake.
            if (this == CarController.RearLeftWheelCollider || this == CarController.RearRightWheelCollider) {

                canHandbrake = true;
                canBrake = true;
                brakingMultiplier = .75f;

            }

        }

    }

    private void OnEnable() {

        // Listening an event when main behavior changed.
        RCC_SceneManager.OnBehaviorChanged += UpdateWheelFrictions;

        //  If wheel model is assigned but not enabled, enable it.
        if (wheelModel && !wheelModel.gameObject.activeSelf)
            wheelModel.gameObject.SetActive(true);

        //  Resetting values on enable.
        wheelSlipAmountForward = 0f;
        wheelSlipAmountSideways = 0f;
        totalSlip = 0f;
        bumpForce = 0f;
        oldForce = 0f;

        if (audioSource) {

            audioSource.volume = 0f;
            audioSource.Stop();

        }

        WheelCollider.motorTorque = 0f;
        WheelCollider.brakeTorque = 0f;
        WheelCollider.steerAngle = 0f;

    }

    /// <summary>
    /// Checks the selected behavior in RCC Settings and applies changes.
    /// </summary>
    private void UpdateWheelFrictions() {

        //  Getting forward and sideways frictions of the wheelcollider.
        forwardFrictionCurve = WheelCollider.forwardFriction;
        sidewaysFrictionCurve = WheelCollider.sidewaysFriction;

        //	Getting behavior if selected.
        RCC_Settings.BehaviorType behavior = RCC_Settings.Instance.selectedBehaviorType;

        //	If there is a selected behavior, override friction curves.
        if (!CarController.overrideBehavior && behavior != null) {

            forwardFrictionCurve = SetFrictionCurves(forwardFrictionCurve, behavior.forwardExtremumSlip, behavior.forwardExtremumValue, behavior.forwardAsymptoteSlip, behavior.forwardAsymptoteValue);
            sidewaysFrictionCurve = SetFrictionCurves(sidewaysFrictionCurve, behavior.sidewaysExtremumSlip, behavior.sidewaysExtremumValue, behavior.sidewaysAsymptoteSlip, behavior.sidewaysAsymptoteValue);

        }

        // Assigning new frictons.
        WheelCollider.forwardFriction = forwardFrictionCurve;
        WheelCollider.sidewaysFriction = sidewaysFrictionCurve;

        // Override original frictions.
        forwardFrictionCurve_Org = WheelCollider.forwardFriction;
        sidewaysFrictionCurve_Org = WheelCollider.sidewaysFriction;

    }

    private void Update() {

        // Return if RCC is disabled.
        if (!CarController.enabled)
            return;

        // Setting position and rotation of the wheel model.
        if (alignWheel)
            WheelAlign();

    }

    private void FixedUpdate() {

        // Return if RCC is disabled.
        if (!CarController.enabled)
            return;

        float circumFerence = 2.0f * 3.14f * WheelCollider.radius; // Finding circumFerence 2 Pi R.
        wheelRPM2Speed = (circumFerence * WheelCollider.rpm) * 60f; // Finding MPH and converting to KMH.
        wheelRPM2Speed = Mathf.Clamp(wheelRPM2Speed / 1000f, 0f, Mathf.Infinity);

        // Setting power state of the wheels depending on drivetrain mode. Only overrides them if overrideWheels is enabled for the vehicle.
        if (!CarController.overrideAllWheels) {

            switch (CarController.wheelTypeChoise) {

                case RCC_CarControllerV3.WheelType.AWD:
                    canPower = true;
                    break;

                case RCC_CarControllerV3.WheelType.BIASED:
                    canPower = true;
                    break;

                case RCC_CarControllerV3.WheelType.FWD:

                    if (this == CarController.FrontLeftWheelCollider || this == CarController.FrontRightWheelCollider)
                        canPower = true;
                    else
                        canPower = false;

                    break;

                case RCC_CarControllerV3.WheelType.RWD:

                    if (this == CarController.RearLeftWheelCollider || this == CarController.RearRightWheelCollider)
                        canPower = true;
                    else
                        canPower = false;

                    break;

            }

        }

        GroundMaterial();
        Frictions();
        TotalSlip();
        SkidMarks();
        Particles();
        Audio();
        CheckDeflate();
        ESP();

    }

    /// <summary>
    /// ESP System. All wheels have individual brakes. In case of loosing control of the vehicle, corresponding wheel will brake for gaining the control again.
    /// </summary>
    private void ESP() {

        if (CarController.ESP && CarController.brakeInput < .5f) {

            if (CarController.handbrakeInput < .5f) {

                if (CarController.underSteering) {

                    if (this == CarController.FrontLeftWheelCollider)
                        ApplyBrakeTorque((CarController.brakeTorque * CarController.ESPStrength) * Mathf.Clamp(-CarController.rearSlip, 0f, Mathf.Infinity));

                    if (this == CarController.FrontRightWheelCollider)
                        ApplyBrakeTorque((CarController.brakeTorque * CarController.ESPStrength) * Mathf.Clamp(CarController.rearSlip, 0f, Mathf.Infinity));

                }

                if (CarController.overSteering) {

                    if (this == CarController.RearLeftWheelCollider)
                        ApplyBrakeTorque((CarController.brakeTorque * CarController.ESPStrength) * Mathf.Clamp(-CarController.frontSlip, 0f, Mathf.Infinity));

                    if (this == CarController.RearRightWheelCollider)
                        ApplyBrakeTorque((CarController.brakeTorque * CarController.ESPStrength) * Mathf.Clamp(CarController.frontSlip, 0f, Mathf.Infinity));

                }

            }

        }

    }

    /// <summary>
    /// Aligning wheel model position and rotation.
    /// </summary>
    private void WheelAlign() {

        // Return if no wheel model selected.
        if (!wheelModel) {

            Debug.LogWarning(transform.name + " wheel of the " + CarController.transform.name + " is missing wheel model.");
            return;

        }

        //  Getting position and rotation of the wheelcollider and assigning wheelPosition and wheelRotation.
        WheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);

        //Increase the rotation value.
        RotationValue += WheelCollider.rpm * (360f / 60f) * Time.deltaTime;

        //	Assigning position and rotation to the wheel model.
        wheelModel.SetPositionAndRotation(wheelPosition, transform.rotation * Quaternion.Euler(RotationValue, WheelCollider.steerAngle, 0f));

        // Adjusting camber angle by Z axis.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, -camber);
        else
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, camber);

        // Adjusting caster angle by X axis.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, -caster);
        else
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, caster);

    }

    /// <summary>
    /// Drawing skidmarks if wheel is skidding.
    /// </summary>
    private void SkidMarks() {

        //  Return if not drawing skidmarks.
        if (!drawSkid)
            return;

        // If scene has skidmarks manager...
        if (!RCC_Settings.Instance.dontUseSkidmarks) {

            // If slips are bigger than target value, draw the skidmarks.
            if (totalSlip > RCC_GroundMaterials.Instance.frictions[groundIndex].slip) {

                Vector3 skidPoint = wheelHit.point + 1f * Time.deltaTime * (Rigid.velocity);

                if (Rigid.velocity.magnitude > 1f && isGrounded && wheelHit.normal != Vector3.zero && wheelHit.point != Vector3.zero && skidPoint != Vector3.zero && Mathf.Abs(skidPoint.x) > 1f && Mathf.Abs(skidPoint.z) > 1f)
                    lastSkidmark = RCC_SkidmarksManager.Instance.AddSkidMark(skidPoint, wheelHit.normal, totalSlip, wheelWidth, lastSkidmark, groundIndex);
                else
                    lastSkidmark = -1;

            } else {

                lastSkidmark = -1;

            }

        }

    }

    /// <summary>
    /// Sets forward and sideways frictions.
    /// </summary>
    private void Frictions() {

        // Handbrake input clamped 0f - 1f.
        float hbInput = CarController.handbrakeInput;

        if (canHandbrake && hbInput > .75f)
            hbInput = .75f;
        else
            hbInput = 1f;

        // Setting wheel stiffness to ground physic material stiffness.
        forwardFrictionCurve.stiffness = RCC_GroundMaterials.Instance.frictions[groundIndex].forwardStiffness;
        sidewaysFrictionCurve.stiffness = (RCC_GroundMaterials.Instance.frictions[groundIndex].sidewaysStiffness * hbInput * tractionHelpedSidewaysStiffness);

        //  If deflated, apply deflated stiffness.
        if (deflated) {

            forwardFrictionCurve.stiffness *= deflatedStiffnessMultiplier;
            sidewaysFrictionCurve.stiffness *= deflatedStiffnessMultiplier;

        }

        // If drift mode is selected, apply specific frictions.
        if (!CarController.overrideBehavior && RCC_Settings.Instance.selectedBehaviorType != null && RCC_Settings.Instance.selectedBehaviorType.applyExternalWheelFrictions)
            Drift();

        // Setting new friction curves to wheels.
        WheelCollider.forwardFriction = forwardFrictionCurve;
        WheelCollider.sidewaysFriction = sidewaysFrictionCurve;

        // Also damp too.
        WheelCollider.wheelDampingRate = RCC_GroundMaterials.Instance.frictions[groundIndex].damp;

    }

    /// <summary>
    /// Total amount of wheel slip.
    /// </summary>
    private void TotalSlip() {

        // Forward, sideways, and total slips.
        if (isGrounded && wheelHit.point != Vector3.zero) {

            wheelSlipAmountForward = wheelHit.forwardSlip;
            wheelSlipAmountSideways = wheelHit.sidewaysSlip;

        } else {

            wheelSlipAmountForward = 0f;
            wheelSlipAmountSideways = 0f;

        }

        totalSlip = Mathf.Lerp(totalSlip, ((Mathf.Abs(wheelSlipAmountSideways) + Mathf.Abs(wheelSlipAmountForward)) / 2f), Time.fixedDeltaTime * 10f);

    }

    /// <summary>
    /// Particles.
    /// </summary>
    private void Particles() {

        if (RCC_Settings.Instance.dontUseAnyParticleEffects)
            return;

        // If wheel slip is bigger than ground physic material slip, enable particles. Otherwise, disable particles.
        for (int i = 0; i < allWheelParticles.Count; i++) {

            if (totalSlip > RCC_GroundMaterials.Instance.frictions[groundIndex].slip) {

                if (i != groundIndex) {

                    ParticleSystem.EmissionModule em;

                    em = allWheelParticles[i].emission;
                    em.enabled = false;

                } else {

                    ParticleSystem.EmissionModule em;

                    em = allWheelParticles[i].emission;
                    em.enabled = true;

                }

            } else {

                ParticleSystem.EmissionModule em;

                em = allWheelParticles[i].emission;
                em.enabled = false;

            }

            if (isGrounded && wheelHit.point != Vector3.zero)
                allWheelParticles[i].transform.position = wheelHit.point + (.05f * transform.up);

        }

    }

    /// <summary>
    /// Drift.
    /// </summary>
    private void Drift() {

        Vector3 relativeVelocity = transform.InverseTransformDirection(Rigid.velocity);
        float sqrVel = (relativeVelocity.x * relativeVelocity.x) / 50f;

        if (wheelHit.forwardSlip > 0)
            sqrVel += (Mathf.Abs(wheelHit.forwardSlip));

        if (CarController.wheelTypeChoise == RCC_CarControllerV3.WheelType.RWD) {

            // Forward
            if (WheelCollider == CarController.FrontLeftWheelCollider.WheelCollider || WheelCollider == CarController.FrontRightWheelCollider.WheelCollider) {

                forwardFrictionCurve.extremumValue = Mathf.Clamp(forwardFrictionCurve_Org.extremumValue - sqrVel, minForwardStiffness / 1f, maxForwardStiffness);
                forwardFrictionCurve.asymptoteValue = Mathf.Clamp(forwardFrictionCurve_Org.asymptoteValue - (sqrVel / 1f), minForwardStiffness / 1f, maxForwardStiffness); ;

            } else {

                forwardFrictionCurve.extremumValue = Mathf.Clamp(forwardFrictionCurve_Org.extremumValue - sqrVel, minForwardStiffness, maxForwardStiffness);
                forwardFrictionCurve.asymptoteValue = Mathf.Clamp(forwardFrictionCurve_Org.asymptoteValue - (sqrVel / 1f), minForwardStiffness, maxForwardStiffness);

            }

            // Sideways
            if (WheelCollider == CarController.FrontLeftWheelCollider.WheelCollider || WheelCollider == CarController.FrontRightWheelCollider.WheelCollider) {

                sidewaysFrictionCurve.extremumValue = Mathf.Clamp(sidewaysFrictionCurve_Org.extremumValue - sqrVel, minSidewaysStiffness, maxSidewaysStiffness);
                sidewaysFrictionCurve.asymptoteValue = Mathf.Clamp(sidewaysFrictionCurve_Org.asymptoteValue - (sqrVel / 1f), minSidewaysStiffness, maxSidewaysStiffness);

            } else {

                sidewaysFrictionCurve.extremumValue = Mathf.Clamp(sidewaysFrictionCurve_Org.extremumValue - sqrVel, minSidewaysStiffness, maxSidewaysStiffness);
                sidewaysFrictionCurve.asymptoteValue = Mathf.Clamp(sidewaysFrictionCurve_Org.asymptoteValue - (sqrVel / 1f), minSidewaysStiffness, maxSidewaysStiffness);

            }

        } else {

            if (WheelCollider == CarController.FrontLeftWheelCollider.WheelCollider || WheelCollider == CarController.FrontRightWheelCollider.WheelCollider) {

                // Forward
                forwardFrictionCurve.extremumValue = Mathf.Clamp(forwardFrictionCurve_Org.extremumValue - sqrVel, minForwardStiffness / 1f, maxForwardStiffness);
                forwardFrictionCurve.asymptoteValue = Mathf.Clamp(forwardFrictionCurve_Org.asymptoteValue - (sqrVel / 1f), minForwardStiffness / 1f, maxForwardStiffness);

            } else {

                forwardFrictionCurve.extremumValue = Mathf.Clamp(forwardFrictionCurve_Org.extremumValue - sqrVel, minForwardStiffness, maxForwardStiffness);
                forwardFrictionCurve.asymptoteValue = Mathf.Clamp(forwardFrictionCurve_Org.asymptoteValue - (sqrVel / 1f), minForwardStiffness, maxForwardStiffness);

            }

            // Sideways
            sidewaysFrictionCurve.extremumValue = Mathf.Clamp(sidewaysFrictionCurve_Org.extremumValue - sqrVel, minSidewaysStiffness, maxSidewaysStiffness);
            sidewaysFrictionCurve.asymptoteValue = Mathf.Clamp(sidewaysFrictionCurve_Org.asymptoteValue - (sqrVel / 1f), minSidewaysStiffness, maxSidewaysStiffness);

        }

    }

    /// <summary>
    /// Audio.
    /// </summary>
    private void Audio() {

        // Set audioclip to ground physic material sound.
        audioClip = RCC_GroundMaterials.Instance.frictions[groundIndex].groundSound;
        audioVolume = RCC_GroundMaterials.Instance.frictions[groundIndex].volume;

        // If total slip is high enough...
        if (totalSlip > RCC_GroundMaterials.Instance.frictions[groundIndex].slip) {

            // Assigning corresponding audio clip.
            if (audioSource.clip != audioClip)
                audioSource.clip = audioClip;

            // Playing it.
            if (!audioSource.isPlaying)
                audioSource.Play();

            // If vehicle is moving, set volume and pitch. Otherwise set them to 0.
            if (Rigid.velocity.magnitude > 1f) {

                audioSource.volume = Mathf.Lerp(0f, audioVolume, totalSlip);
                audioSource.pitch = Mathf.Lerp(1f, .8f, audioSource.volume);

            } else {

                audioSource.volume = 0f;

            }

        } else {

            audioSource.volume = 0f;

            // If volume is minimal and audio is still playing, stop.
            if (audioSource.volume <= .05f && audioSource.isPlaying)
                audioSource.Stop();

        }

        // Calculating bump force.
        bumpForce = wheelHit.force - oldForce;

        //	If bump force is high enough, play bump SFX.
        if ((bumpForce) >= 5000f) {

            // Creating and playing audiosource for bump SFX.
            AudioSource bumpSound = NewAudioSource(RCC_Settings.Instance.audioMixer, CarController.gameObject, "Bump Sound AudioSource", 5f, 50f, (bumpForce - 5000f) / 3000f, RCC_Settings.Instance.bumpClip, false, true, true);
            bumpSound.pitch = Random.Range(.9f, 1.1f);

        }

        oldForce = wheelHit.force;

    }

    /// <summary>
    /// Returns true if one of the wheel is slipping.
    /// </summary>
    /// <returns><c>true</c>, if skidding was ised, <c>false</c> otherwise.</returns>
    public bool IsSkidding() {

        for (int i = 0; i < CarController.AllWheelColliders.Length; i++) {

            if (CarController.AllWheelColliders[i].totalSlip > RCC_GroundMaterials.Instance.frictions[groundIndex].slip)
                return true;

        }

        return false;

    }

    /// <summary>
    /// Applies the motor torque.
    /// </summary>
    /// <param name="torque">Torque.</param>
    public void ApplyMotorTorque(float torque) {

        //	If TCS is enabled, checks forward slip. If wheel is losing traction, don't apply torque.
        if (CarController.TCS) {

            if (Mathf.Abs(WheelCollider.rpm) >= 1) {

                if (Mathf.Abs(wheelSlipAmountForward) > RCC_GroundMaterials.Instance.frictions[groundIndex].slip) {

                    CarController.TCSAct = true;

                    torque -= Mathf.Clamp(torque * (Mathf.Abs(wheelSlipAmountForward)) * CarController.TCSStrength, -Mathf.Infinity, Mathf.Infinity);

                    if (WheelCollider.rpm > 1) {

                        torque -= Mathf.Clamp(torque * (Mathf.Abs(wheelSlipAmountForward)) * CarController.TCSStrength, 0f, Mathf.Infinity);
                        torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

                    } else {

                        torque += Mathf.Clamp(-torque * (Mathf.Abs(wheelSlipAmountForward)) * CarController.TCSStrength, 0f, Mathf.Infinity);
                        torque = Mathf.Clamp(torque, -Mathf.Infinity, 0f);

                    }

                } else {

                    CarController.TCSAct = false;

                }

            } else {

                CarController.TCSAct = false;

            }

        }

        if (CheckOvertorque())
            torque = 0;

        if (Mathf.Abs(torque) > 1f)
            WheelCollider.motorTorque = torque;
        else
            WheelCollider.motorTorque = 0f;

    }

    /// <summary>
    /// Applies the steering.
    /// </summary>
    /// <param name="steerInput">Steer input.</param>
    /// <param name="angle">Angle.</param>
    public void ApplySteering(float steerInput, float angle) {

        //	Ackerman steering formula.
        if (steerInput > 0f) {

            if (transform.localPosition.x < 0)
                WheelCollider.steerAngle = (Mathf.Deg2Rad * angle * 2.55f) * (Mathf.Rad2Deg * Mathf.Atan(2.55f / (6 + (1.5f / 2))) * steerInput);
            else
                WheelCollider.steerAngle = (Mathf.Deg2Rad * angle * 2.55f) * (Mathf.Rad2Deg * Mathf.Atan(2.55f / (6 - (1.5f / 2))) * steerInput);

        } else if (steerInput < 0f) {

            if (transform.localPosition.x < 0)
                WheelCollider.steerAngle = (Mathf.Deg2Rad * angle * 2.55f) * (Mathf.Rad2Deg * Mathf.Atan(2.55f / (6 - (1.5f / 2))) * steerInput);
            else
                WheelCollider.steerAngle = (Mathf.Deg2Rad * angle * 2.55f) * (Mathf.Rad2Deg * Mathf.Atan(2.55f / (6 + (1.5f / 2))) * steerInput);

        } else {

            WheelCollider.steerAngle = 0f;

        }

        if (transform.localPosition.x < 0)
            WheelCollider.steerAngle += toe;
        else
            WheelCollider.steerAngle -= toe;

    }

    /// <summary>
    /// Applies the brake torque.
    /// </summary>
    /// <param name="torque">Torque.</param>
    public void ApplyBrakeTorque(float torque) {

        //	If ABS is enabled, checks forward slip. If wheel is losing traction, don't apply torque.
        if (CarController.ABS && CarController.handbrakeInput <= .1f) {

            if ((Mathf.Abs(wheelHit.forwardSlip) * Mathf.Clamp01(torque)) >= CarController.ABSThreshold) {

                CarController.ABSAct = true;
                torque = 0;

            } else {

                CarController.ABSAct = false;

            }

        }

        if (Mathf.Abs(torque) > 1f)
            WheelCollider.brakeTorque = torque;
        else
            WheelCollider.brakeTorque = 0f;

    }

    /// <summary>
    /// Converts to splat map coordinate.
    /// </summary>
    /// <returns>The to splat map coordinate.</returns>
    /// <param name="playerPos">Player position.</param>
    private Vector3 ConvertToSplatMapCoordinate(Terrain terrain, Vector3 playerPos) {

        Vector3 vecRet = new Vector3();
        Vector3 terPosition = terrain.transform.position;
        vecRet.x = ((playerPos.x - terPosition.x) / terrain.terrainData.size.x) * terrain.terrainData.alphamapWidth;
        vecRet.z = ((playerPos.z - terPosition.z) / terrain.terrainData.size.z) * terrain.terrainData.alphamapHeight;
        return vecRet;

    }

    /// <summary>
    /// Gets the index of the ground material.
    /// </summary>
    /// <returns>The ground material index.</returns>
    private void GroundMaterial() {

        isGrounded = WheelCollider.GetGroundHit(out wheelHit);

        if (!isGrounded || wheelHit.point == Vector3.zero || wheelHit.collider == null) {

            groundIndex = 0;
            return;

        }

        for (int i = 0; i < RCC_GroundMaterials.Instance.frictions.Length; i++) {

            if (wheelHit.collider.sharedMaterial == RCC_GroundMaterials.Instance.frictions[i].groundMaterial) {

                groundIndex = i;
                return;

            }

        }

        // If ground pyhsic material is not one of the ground material in Configurable Ground Materials, check if we are on terrain collider...
        if (!RCC_SceneManager.Instance.terrainsInitialized) {

            groundIndex = 0;
            return;

        }

        for (int i = 0; i < RCC_GroundMaterials.Instance.terrainFrictions.Length; i++) {

            if (wheelHit.collider.sharedMaterial == RCC_GroundMaterials.Instance.terrainFrictions[i].groundMaterial) {

                RCC_SceneManager.Terrains currentTerrain = null;

                for (int l = 0; l < RCC_SceneManager.Instance.terrains.Length; l++) {

                    if (RCC_SceneManager.Instance.terrains[l].terrainCollider == RCC_GroundMaterials.Instance.terrainFrictions[i].groundMaterial) {

                        currentTerrain = RCC_SceneManager.Instance.terrains[l];
                        break;

                    }

                }

                if (currentTerrain != null) {

                    Vector3 playerPos = transform.position;
                    Vector3 TerrainCord = ConvertToSplatMapCoordinate(currentTerrain.terrain, playerPos);
                    float comp = 0f;

                    for (int k = 0; k < currentTerrain.mNumTextures; k++) {

                        if (comp < currentTerrain.mSplatmapData[(int)TerrainCord.z, (int)TerrainCord.x, k])
                            groundIndex = k;

                    }

                    groundIndex = RCC_GroundMaterials.Instance.terrainFrictions[i].splatmapIndexes[groundIndex].index;
                    return;

                }

            }

        }

        groundIndex = 0;

    }

    /// <summary>
    /// Checking deflated wheel.
    /// </summary>
    private void CheckDeflate() {

        if (deflated) {

            if (!flatSource)
                flatSource = NewAudioSource(gameObject, FlatAudio.name, 1f, 15f, .5f, FlatAudio, true, false, false);

            flatSource.volume = Mathf.Clamp01(Mathf.Abs(WheelCollider.rpm * .001f));
            flatSource.volume *= isGrounded ? 1f : 0f;

            if (!flatSource.isPlaying)
                flatSource.Play();

        } else {

            if (flatSource && flatSource.isPlaying)
                flatSource.Stop();

        }

        if (_wheelDeflateParticles != null) {

            ParticleSystem.EmissionModule em = _wheelDeflateParticles.emission;

            if (deflated) {

                if (WheelCollider.rpm > 100f && isGrounded)
                    em.enabled = true;
                else
                    em.enabled = false;

            } else {

                em.enabled = false;

            }

        }

        if (!isGrounded || wheelHit.point == Vector3.zero || wheelHit.collider == null)
            return;

        for (int i = 0; i < RCC_GroundMaterials.Instance.frictions.Length; i++) {

            if (wheelHit.collider.sharedMaterial == RCC_GroundMaterials.Instance.frictions[i].groundMaterial) {

                if (RCC_GroundMaterials.Instance.frictions[i].deflate)
                    Deflate();

            }

        }

    }

    /// <summary>
    /// Checks if overtorque applying.
    /// </summary>
    /// <returns><c>true</c>, if torque was overed, <c>false</c> otherwise.</returns>
    private bool CheckOvertorque() {

        if (CarController.speed > CarController.maxspeed || !CarController.engineRunning)
            return true;

        if (CarController.speed > CarController.gears[CarController.currentGear].maxSpeed && CarController.engineRPM >= (CarController.maxEngineRPM * .985f))
            return true;

        return false;

    }

    /// <summary>
    /// Sets a new friction to WheelCollider.
    /// </summary>
    /// <returns>The friction curves.</returns>
    /// <param name="curve">Curve.</param>
    /// <param name="extremumSlip">Extremum slip.</param>
    /// <param name="extremumValue">Extremum value.</param>
    /// <param name="asymptoteSlip">Asymptote slip.</param>
    /// <param name="asymptoteValue">Asymptote value.</param>
    public WheelFrictionCurve SetFrictionCurves(WheelFrictionCurve curve, float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue) {

        WheelFrictionCurve newCurve = curve;

        newCurve.extremumSlip = extremumSlip;
        newCurve.extremumValue = extremumValue;
        newCurve.asymptoteSlip = asymptoteSlip;
        newCurve.asymptoteValue = asymptoteValue;

        return newCurve;

    }

    /// <summary>
    /// Deflates the wheel.
    /// </summary>
    public void Deflate() {

        if (deflated)
            return;

        deflated = true;

        if (defRadius == -1)
            defRadius = WheelCollider.radius;

        WheelCollider.radius = defRadius * deflateRadiusMultiplier;

        if (DeflateAudio)
            NewAudioSource(gameObject, DeflateAudio.name, 5f, 50f, 1f, DeflateAudio, false, true, true);

        if (_wheelDeflateParticles == null && WheelDeflateParticles) {

            GameObject ps = Instantiate(WheelDeflateParticles.gameObject, transform.position, transform.rotation);
            _wheelDeflateParticles = ps.GetComponent<ParticleSystem>();
            _wheelDeflateParticles.transform.SetParent(transform, false);
            _wheelDeflateParticles.transform.localPosition = new Vector3(0f, -.2f, 0f);
            _wheelDeflateParticles.transform.localRotation = Quaternion.identity;

        }

        CarController.Rigid.AddForceAtPosition(transform.right * Random.Range(-1f, 1f) * 30f, transform.position, ForceMode.Acceleration);

    }

    /// <summary>
    /// Inflates the wheel.
    /// </summary>
    public void Inflate() {

        if (!deflated)
            return;

        deflated = false;

        if (defRadius != -1)
            WheelCollider.radius = defRadius;

        if (InflateAudio)
            NewAudioSource(gameObject, InflateAudio.name, 5f, 50f, 1f, InflateAudio, false, true, true);

    }

    private void OnDisable() {

        RCC_SceneManager.OnBehaviorChanged -= UpdateWheelFrictions;

        if (wheelModel)
            wheelModel.gameObject.SetActive(false);

        //  Resetting values on disable.
        wheelSlipAmountForward = 0f;
        wheelSlipAmountSideways = 0f;
        totalSlip = 0f;
        bumpForce = 0f;
        oldForce = 0f;

        if (audioSource) {

            audioSource.volume = 0f;
            audioSource.Stop();

        }

        WheelCollider.motorTorque = 0f;
        WheelCollider.brakeTorque = 0f;
        WheelCollider.steerAngle = 0f;

    }

    /// <summary>
    /// Raises the draw gizmos event.
    /// </summary>
    private void OnDrawGizmos() {

#if UNITY_EDITOR
        if (Application.isPlaying) {

            WheelCollider.GetGroundHit(out WheelHit hit);

            // Drawing gizmos for wheel forces and slips.
            float extension = (-WheelCollider.transform.InverseTransformPoint(hit.point).y - (WheelCollider.radius * transform.lossyScale.y)) / WheelCollider.suspensionDistance;
            Debug.DrawLine(hit.point, hit.point + transform.up * (hit.force / Rigid.mass), extension <= 0.0 ? Color.magenta : Color.white);
            Debug.DrawLine(hit.point, hit.point - transform.forward * hit.forwardSlip * 2f, Color.green);
            Debug.DrawLine(hit.point, hit.point - transform.right * hit.sidewaysSlip * 2f, Color.red);

        }
#endif

    }

}