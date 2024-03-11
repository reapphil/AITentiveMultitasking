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
/// Checks the overall setup and configuration of the vehicle.
/// </summary>
public class RCC_CheckUp {

    /// <summary>
    /// Gets all mesh colliders with trigger enabled.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public static MeshCollider[] GetTriggerMeshColliders(GameObject vehicle) {

        MeshCollider[] meshcolliders = vehicle.GetComponentsInChildren<MeshCollider>(true);
        List<MeshCollider> triggers = new List<MeshCollider>();

        for (int i = 0; i < meshcolliders.Length; i++) {

            if (meshcolliders[i].isTrigger == true)
                triggers.Add(meshcolliders[i]);

        }

        return triggers.ToArray();

    }

    /// <summary>
    /// Gets all colliders.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public static Collider[] GetColliders(GameObject vehicle) {

        Collider[] colliders = vehicle.GetComponentsInChildren<Collider>(true);
        List<Collider> coll = new List<Collider>();

        for (int i = 0; i < colliders.Length; i++)
            coll.Add(colliders[i]);

        return coll.ToArray();

    }

    /// <summary>
    /// Vehicle has a collider or not?
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public static bool HaveCollider(GameObject vehicle) {

        Collider[] colliders = vehicle.GetComponentsInChildren<Collider>(true);
        List<Collider> coll = new List<Collider>();

        for (int i = 0; i < colliders.Length; i++) {

            if (colliders[i].enabled && colliders[i].GetType() != typeof(WheelCollider))
                coll.Add(colliders[i]);

        }

        if (coll.Count >= 1)
            return true;
        else
            return false;

    }

    /// <summary>
    /// Gets all rigidbodies.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public static Rigidbody[] GetRigids(GameObject vehicle) {

        Rigidbody[] rigidbodies = vehicle.GetComponentsInChildren<Rigidbody>(true);
        List<Rigidbody> rigids = new List<Rigidbody>();

        for (int i = 0; i < rigidbodies.Length; i++) {

            if (rigidbodies[i] != vehicle.GetComponent<Rigidbody>() && rigidbodies[i].GetComponent<RCC_HoodCamera>() == null && rigidbodies[i].GetComponent<RCC_WheelCamera>() == null && rigidbodies[i] != vehicle.GetComponent<RCC_DetachablePart>())
                rigids.Add(rigidbodies[i]);

        }

        return rigids.ToArray();

    }

    /// <summary>
    /// Gets all wheelcolliders.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public static WheelCollider[] GetWheelColliders(GameObject vehicle) {

        WheelCollider[] wheelColliders = vehicle.GetComponentsInChildren<WheelCollider>(true);
        List<WheelCollider> wheels = new List<WheelCollider>();

        for (int i = 0; i < wheelColliders.Length; i++) {

            if (wheelColliders[i].radius == 0 || wheelColliders[i].suspensionDistance <= 0.01)
                wheels.Add(wheelColliders[i]);

        }

        return wheels.ToArray();

    }

    /// <summary>
    /// Gets all sphere colliders.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public static SphereCollider[] GetSphereColliders(GameObject vehicle) {

        SphereCollider[] sphereColliders = vehicle.GetComponentsInChildren<SphereCollider>(true);
        List<SphereCollider> spheres = new List<SphereCollider>();

        for (int i = 0; i < sphereColliders.Length; i++) {

            if (sphereColliders[i].enabled)
                spheres.Add(sphereColliders[i]);

        }

        return spheres.ToArray();

    }

    /// <summary>
    /// Checks incorrect configurations.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public static string[] IncorrectConfiguration(RCC_CarControllerV3 vehicle) {

        float minEngineRPM = vehicle.minEngineRPM;
        float maxEngineRPM = vehicle.maxEngineRPM;

        float gearDownRPM = vehicle.gearShiftDownRPM;
        float gearUpRPM = vehicle.gearShiftUpRPM;

        List<string> errorMessages = new List<string>();

        if (minEngineRPM >= maxEngineRPM)
            errorMessages.Add("Min engine rpm must be lower than max engine rpm!");

        if (maxEngineRPM <= minEngineRPM)
            errorMessages.Add("Max engine rpm must be higher than min engine rpm!");

        if (gearDownRPM >= gearUpRPM)
            errorMessages.Add("Gear shift down rpm must be lower than gear shift up rpm!");

        if (gearUpRPM <= gearDownRPM)
            errorMessages.Add("Gear shift up rpm must be higher than gear shift down rpm!");

        if (gearDownRPM <= minEngineRPM)
            errorMessages.Add("Gear shift down rpm must be higher than min engine rpm!");

        if (gearUpRPM >= maxEngineRPM)
            errorMessages.Add("Gear shift up rpm must be lower than max engine rpm!");

        if ((Mathf.Abs(maxEngineRPM) - Mathf.Abs(minEngineRPM)) < 3000f)
            errorMessages.Add("Max and min engine rpms are too close to each other!");

        if ((Mathf.Abs(gearUpRPM) - Mathf.Abs(gearDownRPM)) < 1000f)
            errorMessages.Add("Gear shift up and down rpms are too close to each other!");

        return errorMessages.ToArray();

    }

    /// <summary>
    /// Checks meshes with wrong axes.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <param name="meshes"></param>
    /// <returns></returns>
    public static bool HaveWrongAxis(GameObject vehicle, MeshFilter[] meshes) {

        for (int i = 0; i < meshes.Length; i++) {

            if (meshes[i]) {

                if (1 - Mathf.Abs(Quaternion.Dot(meshes[i].transform.rotation, vehicle.transform.rotation)) > .01f)
                    return true;

            }

        }

        return false;

    }

    /// <summary>
    /// Checks the wheel configuration.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public static bool[] HaveWrongOverride(RCC_CarControllerV3 vehicle) {

        RCC_WheelCollider[] allWheels = vehicle.GetComponentsInChildren<RCC_WheelCollider>(true);

        bool powerFound = false;
        bool steerFound = false;
        bool brakeFround = false;
        bool ebrakeFound = false;

        for (int i = 0; i < allWheels.Length; i++) {

            if (allWheels[i].canPower)
                powerFound = true;

            if (allWheels[i].canSteer)
                steerFound = true;

            if (allWheels[i].canBrake)
                brakeFround = true;

            if (allWheels[i].canHandbrake)
                ebrakeFound = true;

        }

        bool[] found = { powerFound, steerFound, brakeFround, ebrakeFound };
        return found;

    }

}
