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
/// Truck trailer has additional wheelcolliders. This script handles center of mass of the trailer, wheelcolliders, ligths, etc...
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Truck Trailer")]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ConfigurableJoint))]
public class RCC_TruckTrailer : MonoBehaviour {

    private RCC_CarControllerV3 carController;      //  Car controller of this trailer.
    private Rigidbody rigid;        //  Rigidbody.
    private ConfigurableJoint joint;        //  Configurable joint of this trailer.

    /// <summary>
    /// Wheel colliders and models.
    /// </summary>
    [System.Serializable]
    public class TrailerWheel {

        public WheelCollider wheelCollider;
        public Transform wheelModel;

        public void Torque(float torque) {

            wheelCollider.motorTorque = torque;

        }

        public void Brake(float torque) {

            wheelCollider.brakeTorque = torque;

        }

    }
    public TrailerWheel[] trailerWheels;        //  All trailer wheels.

    public Transform COM;       //  Center of mass.
    public GameObject legs;     //  Legs will be enabled when trailer is detached.
    private bool isSleeping = false;        //  Is rigidbody of the trailer is sleeping?

    private float timer = 0f;       //  Timer for attach / detach process.
    public bool attached = false;       //  Is this trailer attached now?

    public bool brakeWhenDetached = false;
    public float brakeForce = 5000f;

    /// <summary>
    /// Joint restrictions of the trailer.
    /// </summary>
    private class JointRestrictions {

        public ConfigurableJointMotion motionX;
        public ConfigurableJointMotion motionY;
        public ConfigurableJointMotion motionZ;

        public ConfigurableJointMotion angularMotionX;
        public ConfigurableJointMotion angularMotionY;
        public ConfigurableJointMotion angularMotionZ;

        public void Get(ConfigurableJoint configurableJoint) {

            motionX = configurableJoint.xMotion;
            motionY = configurableJoint.yMotion;
            motionZ = configurableJoint.zMotion;

            angularMotionX = configurableJoint.angularXMotion;
            angularMotionY = configurableJoint.angularYMotion;
            angularMotionZ = configurableJoint.angularZMotion;

        }

        public void Set(ConfigurableJoint configurableJoint) {

            configurableJoint.xMotion = motionX;
            configurableJoint.yMotion = motionY;
            configurableJoint.zMotion = motionZ;

            configurableJoint.angularXMotion = angularMotionX;
            configurableJoint.angularYMotion = angularMotionY;
            configurableJoint.angularZMotion = angularMotionZ;

        }

        public void Reset(ConfigurableJoint configurableJoint) {

            configurableJoint.xMotion = ConfigurableJointMotion.Free;
            configurableJoint.yMotion = ConfigurableJointMotion.Free;
            configurableJoint.zMotion = ConfigurableJointMotion.Free;

            configurableJoint.angularXMotion = ConfigurableJointMotion.Free;
            configurableJoint.angularYMotion = ConfigurableJointMotion.Free;
            configurableJoint.angularZMotion = ConfigurableJointMotion.Free;

        }

    }
    private JointRestrictions jointRestrictions = new JointRestrictions();
    private RCC_Light[] lights;

    private void Start() {

        rigid = GetComponent<Rigidbody>();      //	Getting rigidbody.
        joint = GetComponentInParent<ConfigurableJoint>();      //	Getting configurable joint.
        jointRestrictions.Get(joint);       //	Getting current limitations of the joint.

        // Fixing stutering bug of the rigid.
        rigid.interpolation = RigidbodyInterpolation.None;
        rigid.interpolation = RigidbodyInterpolation.Interpolate;
        joint.configuredInWorldSpace = true;

        //	If joint is connected as default, attach the trailer. Otherwise detach.
        if (joint.connectedBody) {

            AttachTrailer(joint.connectedBody.gameObject.GetComponent<RCC_CarControllerV3>());

        } else {

            carController = null;
            joint.connectedBody = null;
            jointRestrictions.Reset(joint);

        }

    }

    private void FixedUpdate() {

        attached = joint.connectedBody;     //	Is trailer attached now?
        rigid.centerOfMass = transform.InverseTransformPoint(COM.transform.position);       //	Setting center of mass.

        //	Applying torque to the wheels.
        for (int i = 0; i < trailerWheels.Length; i++) {

            if (carController) {

                trailerWheels[i].Torque(carController.throttleInput * (attached ? 1f : 0f));
                trailerWheels[i].Brake((attached ? 0f : 5000f));

            } else {

                trailerWheels[i].Torque(0f);
                trailerWheels[i].Brake((brakeWhenDetached ? brakeForce : 0f));

            }

        }

    }

    private void Update() {

        //	If trailer is not moving, enable sleeping mode.
        if (rigid.velocity.magnitude < .01f && Mathf.Abs(rigid.angularVelocity.magnitude) < .01f)
            isSleeping = true;
        else
            isSleeping = false;

        // Timer was used for attach/detach delay.
        if (timer > 0f)
            timer -= Time.deltaTime;

        timer = Mathf.Clamp01(timer);       //	Clamping timer between 0f - 1f.

        WheelAlign();  // Aligning wheel model position and rotation.

    }

    /// <summary>
    /// Aligning wheel model position and rotation.
    /// </summary>
    private void WheelAlign() {

        //	If trailer is sleeping, return.
        if (isSleeping)
            return;

        for (int i = 0; i < trailerWheels.Length; i++) {

            // Return if no wheel model selected.
            if (!trailerWheels[i].wheelModel) {

                Debug.LogError(transform.name + " wheel of the " + transform.name + " is missing wheel model. This wheel is disabled");
                enabled = false;
                return;

            }

            // Locating correct position and rotation for the wheel.
            Vector3 wheelPosition;
            Quaternion wheelRotation;

            trailerWheels[i].wheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);

            //	Assigning position and rotation to the wheel model.
            trailerWheels[i].wheelModel.transform.SetPositionAndRotation(wheelPosition, wheelRotation);

        }

    }

    /// <summary>
    /// Detach the trailer.
    /// </summary>
    public void DetachTrailer() {

        foreach (RCC_Light item in lights) {

            item.CarController = null;

        }

        // Resetting attachedTrailer of car controller.
        carController.attachedTrailer = null;
        carController = null;
        lights = null;
        timer = 1f;
        joint.connectedBody = null;
        jointRestrictions.Reset(joint);

        if (legs)
            legs.SetActive(true);

        if (RCC_SceneManager.Instance.activePlayerCamera && RCC_SceneManager.Instance.activePlayerCamera.TPSAutoFocus)
            StartCoroutine(RCC_SceneManager.Instance.activePlayerCamera.AutoFocus());

    }

    /// <summary>
    /// Attach the trailer.
    /// </summary>
    /// <param name="vehicle"></param>
    public void AttachTrailer(RCC_CarControllerV3 vehicle) {

        // If delay is short, return.
        if (timer > 0)
            return;

        carController = vehicle;        //	Assigning car controller.
        lights = GetComponentsInChildren<RCC_Light>();       //	Getting car controller lights.
        timer = 1f;     //	Setting timer.

        joint.connectedBody = vehicle.Rigid;        //	Connecting joint.
                                                    //joint.autoConfigureConnectedAnchor = false;		//	Setting auto configuration off of the joint.
                                                    //Vector3 jointVector = joint.connectedAnchor;		//	Resetting X axis of the connected anchor on attachment.
                                                    //jointVector.x = 0f;
                                                    //joint.connectedAnchor = jointVector;
        jointRestrictions.Set(joint);       //	Enabling limitations of the joint.

        // If trailer has legs, disable on attach.
        if (legs)
            legs.SetActive(false);

        //	Initializing lights of the trailer. Parent car controller will take control of them.
        foreach (RCC_Light item in lights) {

            item.CarController = carController;

        }

        // Assigning attachedTrailer of car controller.
        vehicle.attachedTrailer = this;
        rigid.isKinematic = false;

        // If autofocus is enabled on RCC Camera, run it.
        if (RCC_SceneManager.Instance.activePlayerCamera && RCC_SceneManager.Instance.activePlayerCamera.TPSAutoFocus)
            StartCoroutine(RCC_SceneManager.Instance.activePlayerCamera.AutoFocus(transform, carController.transform));

    }

    private void Reset() {

        if (COM == null) {

            GameObject com = new GameObject("COM");
            com.transform.SetParent(transform, false);
            com.transform.localPosition = Vector3.zero;
            com.transform.localRotation = Quaternion.identity;
            com.transform.localScale = Vector3.one;
            COM = com.transform;

        }

        if (transform.Find("Wheel Models") == null) {

            GameObject com = new GameObject("Wheel Models");
            com.transform.SetParent(transform, false);
            com.transform.localPosition = Vector3.zero;
            com.transform.localRotation = Quaternion.identity;
            com.transform.localScale = Vector3.one;

        }

        if (transform.Find("Wheel Colliders") == null) {

            GameObject com = new GameObject("Wheel Colliders");
            com.transform.SetParent(transform, false);
            com.transform.localPosition = Vector3.zero;
            com.transform.localRotation = Quaternion.identity;
            com.transform.localScale = Vector3.one;

        }

        GetComponent<Rigidbody>().mass = 5000;

    }

}
