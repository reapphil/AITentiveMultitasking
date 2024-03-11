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
/// Rotates the brake caliper.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Visual Brake Caliper")]
public class RCC_Caliper : MonoBehaviour {

    public RCC_WheelCollider wheelCollider;     //  Actual WheelCollider.
    private GameObject newPivot;        //  Creating new center pivot for correct position.
    private Quaternion defLocalRotation;        //  Default local rotation.

    private void Awake() {

        //	No need to go further if no wheelcollider found.
        if (!wheelCollider) {

            Debug.LogError("WheelCollider is not selected for this caliper named " + transform.name);
            enabled = false;
            return;

        }

        //	Creating new center pivot for correct position.
        newPivot = new GameObject("Pivot_" + transform.name);
        newPivot.transform.SetParent(wheelCollider.WheelCollider.transform, false);
        transform.SetParent(newPivot.transform, true);

        //	Assigning default rotation.
        defLocalRotation = newPivot.transform.localRotation;

    }

    private void LateUpdate() {

        //	No need to go further if no wheelcollider or no wheelmodel found.
        if (!wheelCollider.wheelModel || !wheelCollider.WheelCollider)
            return;

        // Left or right side?
        int side = 1;

        //  If left side...
        if (wheelCollider.transform.localPosition.x < 0)
            side = -1;

        //	Re-positioning camber pivot.
        newPivot.transform.position = wheelCollider.wheelPosition;
        newPivot.transform.localPosition += Vector3.up * wheelCollider.WheelCollider.suspensionDistance / 2f;

        //	Re-rotationing camber pivot.
        newPivot.transform.localRotation = defLocalRotation * Quaternion.Euler(wheelCollider.caster * side, wheelCollider.WheelCollider.steerAngle, wheelCollider.camber * side);

    }

}
