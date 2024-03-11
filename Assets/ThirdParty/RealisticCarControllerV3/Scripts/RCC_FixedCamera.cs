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
/// Fixed camera system for RCC Camera. It simply parents the RCC Camera, and calculates target position, rotation, FOV, etc...
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Camera/RCC Fixed Camera")]
public class RCC_FixedCamera : RCC_Singleton<RCC_FixedCamera> {

    private Vector3 targetPosition;     //  Target position.
    public float maxDistance = 50f;     //  Max distance.
    private float distance;     //  Current distance.

    public float minimumFOV = 20f;      //  FOV limits.
    public float maximumFOV = 60f;
    public bool canTrackNow = false;        //  Can track now?

    private void LateUpdate() {

        //  If can't track now, return.
        if (!canTrackNow)
            return;

        // If current camera is null, return.
        if (!RCC_SceneManager.Instance.activePlayerCamera)
            return;

        // If current camera is null, return.
        if (RCC_SceneManager.Instance.activePlayerCamera.cameraTarget == null)
            return;

        // If current camera is null, return.
        if (RCC_SceneManager.Instance.activePlayerCamera.cameraTarget.playerVehicle == null)
            return;

        //  Getting camera target.
        Transform target = RCC_SceneManager.Instance.activePlayerCamera.cameraTarget.playerVehicle.transform;

        //  Getting speed of the vehicle and calculating the distance.
        float speed = RCC_SceneManager.Instance.activePlayerCamera.cameraTarget.Speed;
        distance = Vector3.Distance(transform.position, target.position);

        //  Calculating and setting field of view of the camera.
        RCC_SceneManager.Instance.activePlayerCamera.targetFieldOfView = Mathf.Lerp(distance > maxDistance / 10f ? maximumFOV : 70f, minimumFOV, (distance * 1.5f) / maxDistance);

        //  Setting target position.
        targetPosition = target.transform.position;
        targetPosition += target.transform.rotation * Vector3.forward * (speed * .05f);

        //  Moving camera to the correct position.
        transform.Translate((-target.forward * speed) / 50f * Time.deltaTime);

        //  Always look at the target.
        transform.LookAt(targetPosition);

        //  If distance exceeds max distance, change position.
        if (distance > maxDistance)
            ChangePosition();

    }

    /// <summary>
    /// Changes position of the camera.
    /// </summary>
    public void ChangePosition() {

        //  If can't track now, return.
        if (!canTrackNow)
            return;

        // If current camera is null, return.
        if (!RCC_SceneManager.Instance.activePlayerCamera)
            return;

        // If current camera is null, return.
        if (RCC_SceneManager.Instance.activePlayerCamera.cameraTarget == null)
            return;

        // If current camera is null, return.
        if (RCC_SceneManager.Instance.activePlayerCamera.cameraTarget.playerVehicle == null)
            return;

        //  Getting camera target.
        Transform target = RCC_SceneManager.Instance.activePlayerCamera.cameraTarget.playerVehicle.transform;

        //  Creating random angle.
        float randomizedAngle = Random.Range(-15f, 15f);
        RaycastHit hit;

        //  Raycasting. If hits, translate the camera to the hit point.
        if (Physics.Raycast(target.position + Vector3.up * 3f, Quaternion.AngleAxis(randomizedAngle, target.up) * target.forward, out hit, maxDistance) && !hit.transform.IsChildOf(target) && !hit.collider.isTrigger) {
            
            transform.position = hit.point + Vector3.up * Random.Range(.5f, 2.5f);
            transform.LookAt(target.position);
            transform.position += transform.forward * 5f;

        } else {

            transform.position = target.position + Vector3.up * Random.Range(.5f, 2.5f);
            transform.rotation = target.rotation * Quaternion.AngleAxis(randomizedAngle, Vector3.up);
            transform.position += transform.forward * (maxDistance * .9f);
            transform.LookAt(target.position);
            transform.position += transform.forward * 5f;

        }

    }

}
