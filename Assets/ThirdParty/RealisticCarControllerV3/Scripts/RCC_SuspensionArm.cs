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
/// Rotates and moves suspension arms based on wheelcollider suspension distance.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Visual Axle (Suspension Distance Based)")]
public class RCC_SuspensionArm : MonoBehaviour {

    public RCC_WheelCollider wheelcollider;     //  Target wheelcollider.

    public SuspensionType suspensionType;       //  Suspension type.
    public enum SuspensionType { Position, Rotation }

    public Axis axis;       //  Axis of the rotation.
    public enum Axis { X, Y, Z }

    //  Default position and rotation of the arm.
    private Vector3 orgPos;
    private Vector3 orgRot;

    //  Total suspension distance.
    private float totalSuspensionDistance = 0;

    //  Offset and angle factor.
    public float offsetAngle = 30;
    public float angleFactor = 150;

    private void Start() {

        //  Getting default position and rotation of the arm.
        orgPos = transform.localPosition;
        orgRot = transform.localEulerAngles;

        //  Getting suspension distance.
        totalSuspensionDistance = GetSuspensionDistance();

    }

    private void Update() {

        //  If no wheelcollider found, return.
        if (!wheelcollider)
            return;

        //  If wheelcollider is not active, return.
        if (!wheelcollider.gameObject.activeSelf)
            return;

        float suspensionCourse = GetSuspensionDistance() - totalSuspensionDistance;

        //  Setting position and rotation of the arm to original.
        transform.localPosition = orgPos;
        transform.localEulerAngles = orgRot;

        //  And then change position or rotation of the arm related to calculated suspension distance.
        switch (suspensionType) {

            case SuspensionType.Position:

                switch (axis) {

                    case Axis.X:
                        transform.position += transform.right * suspensionCourse;
                        break;
                    case Axis.Y:
                        transform.position += transform.up * suspensionCourse;
                        break;
                    case Axis.Z:
                        transform.position += transform.forward * suspensionCourse;
                        break;

                }

                break;

            case SuspensionType.Rotation:

                switch (axis) {

                    case Axis.X:
                        transform.Rotate(Vector3.right, suspensionCourse * angleFactor - offsetAngle, Space.Self);
                        break;
                    case Axis.Y:
                        transform.Rotate(Vector3.up, suspensionCourse * angleFactor - offsetAngle, Space.Self);
                        break;
                    case Axis.Z:
                        transform.Rotate(Vector3.forward, suspensionCourse * angleFactor - offsetAngle, Space.Self);
                        break;

                }

                break;

        }

    }

    /// <summary>
    /// Calculates suspension distance.
    /// </summary>
    /// <returns></returns>
    private float GetSuspensionDistance() {

        wheelcollider.WheelCollider.GetWorldPose(out Vector3 position, out Quaternion quat);
        Vector3 local = wheelcollider.transform.InverseTransformPoint(position);
        return local.y;

    }

}
