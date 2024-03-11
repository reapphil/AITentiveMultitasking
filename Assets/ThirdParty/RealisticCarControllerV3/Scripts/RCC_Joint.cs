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
/// Stores properties, sets properties, and locks motions of the Configurable Joint.
/// </summary>
public class RCC_Joint {

    public Rigidbody connectedBody;     //  Connected body of the configurable joint.

    public Vector3 anchor;      //  Anchor of the configurable joint.
    public Vector3 axis;        //  Axis of the configurable joint.

    //  Joint configurations.
    public ConfigurableJointMotion jointMotionAngularX;
    public ConfigurableJointMotion jointMotionAngularY;
    public ConfigurableJointMotion jointMotionAngularZ;

    public ConfigurableJointMotion jointMotionX;
    public ConfigurableJointMotion jointMotionY;
    public ConfigurableJointMotion jointMotionZ;

    //  Joint limitations.
    public SoftJointLimit linearLimit;
    public SoftJointLimit lowAngularXLimit;
    public SoftJointLimit highAngularXLimit;
    public SoftJointLimit angularYLimit;
    public SoftJointLimit angularZLimit;

    //  Original position and rotation of the joint.
    public Vector3 orgLocalPosition;
    public Quaternion orgLocalRotation;
    public Transform orgParent;

    /// <summary>
    /// Sets the target Configurable Joint properties to the stored one.
    /// </summary>
    /// <param name="joint"></param>
    public void SetProperties(ConfigurableJoint targetJoint) {

        targetJoint.transform.SetParent(orgParent);
        targetJoint.transform.localPosition = orgLocalPosition;
        targetJoint.transform.localRotation = orgLocalRotation;

        targetJoint.connectedBody = connectedBody;
        targetJoint.anchor = anchor;
        targetJoint.axis = axis;

        targetJoint.angularXMotion = jointMotionAngularX;
        targetJoint.angularYMotion = jointMotionAngularY;
        targetJoint.angularZMotion = jointMotionAngularZ;

        targetJoint.xMotion = jointMotionX;
        targetJoint.yMotion = jointMotionY;
        targetJoint.zMotion = jointMotionZ;

        targetJoint.linearLimit = linearLimit;
        targetJoint.lowAngularXLimit = lowAngularXLimit;
        targetJoint.highAngularXLimit = highAngularXLimit;
        targetJoint.angularYLimit = angularYLimit;
        targetJoint.angularZLimit = angularZLimit;

    }

    /// <summary>
    /// Gets default properties of the Configurable Joint.
    /// </summary>
    /// <param name="joint"></param>
    public void GetProperties(ConfigurableJoint joint) {

        connectedBody = joint.connectedBody;
        anchor = joint.anchor;
        axis = joint.axis;

        jointMotionAngularX = joint.angularXMotion;
        jointMotionAngularY = joint.angularYMotion;
        jointMotionAngularZ = joint.angularZMotion;

        jointMotionX = joint.xMotion;
        jointMotionY = joint.yMotion;
        jointMotionZ = joint.zMotion;

        linearLimit = joint.linearLimit;
        lowAngularXLimit = joint.lowAngularXLimit;
        highAngularXLimit = joint.highAngularXLimit;
        angularYLimit = joint.angularYLimit;
        angularZLimit = joint.angularZLimit;

        orgLocalPosition = joint.transform.localPosition;
        orgLocalRotation = joint.transform.localRotation;
        orgParent = joint.transform.parent;

    }

    /// <summary>
    /// //	Locks the Configurable Joint.
    /// </summary>
    /// <param name="joint"></param>
    public static void LockPart(ConfigurableJoint joint) {

        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

    }

}
