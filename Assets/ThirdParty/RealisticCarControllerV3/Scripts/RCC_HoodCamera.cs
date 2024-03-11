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

/// <summary>
/// RCC Camera will be parented to this gameobject when current camera mode is Hood Camera.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Camera/RCC Hood Camera")]
public class RCC_HoodCamera : MonoBehaviour {

    private void Awake() {

        CheckJoint();

    }

    /// <summary>
    /// Fixing shake bug of the rigid.
    /// </summary>
    public void FixShake() {

        StartCoroutine(FixShakeDelayed());

    }

    IEnumerator FixShakeDelayed() {

        //  If no rigid found, return.
        if (!GetComponent<Rigidbody>())
            yield break;

        yield return new WaitForFixedUpdate();
        GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
        yield return new WaitForFixedUpdate();
        GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;

    }

    /// <summary>
    /// Checking configurable joint.
    /// </summary>
    private void CheckJoint() {

        //  Getting configurable joint.
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();

        //  If no joint found, return.
        if (!joint)
            return;

        //  If connected body of the joint is null, set it to car controller itself.
        if (joint.connectedBody == null) {

            RCC_CarControllerV3 carController = GetComponentInParent<RCC_CarControllerV3>();

            if (carController) {

                joint.connectedBody = carController.GetComponent<Rigidbody>();

            } else {

                Debug.LogError("Hood camera of the " + transform.root.name + " has configurable joint with no connected body! Disabling rigid and joint of the camera.");
                Destroy(joint);

                Rigidbody rigid = GetComponent<Rigidbody>();

                if (rigid)
                    Destroy(rigid);

            }

        }

    }

    private void Reset() {

        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();

        if (!joint)
            return;

        RCC_CarControllerV3 carController = GetComponentInParent<RCC_CarControllerV3>();

        if (!carController)
            return;

        joint.connectedBody = carController.GetComponent<Rigidbody>();
        joint.connectedMassScale = 0f;

    }

}

