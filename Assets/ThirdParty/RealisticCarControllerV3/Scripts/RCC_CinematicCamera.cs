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
/// Tracks the player vehicle and keeps orientation nicely for cinematic angles. It has a pivot gameobject named "Animation Pivot". This pivot gameobject has 3 animations itself. 
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Camera/RCC Cinematic Camera")]
public class RCC_CinematicCamera : RCC_Singleton<RCC_CinematicCamera> {

    public GameObject pivot;        // Animation Pivot.
    private Vector3 targetPosition;     // Target position for tracking.
    public float targetFOV = 60f;       // Target field of view.

    private void Start() {

        // If pivot is not selected in the Inspector Panel, create it.
        if (!pivot) {

            pivot = new GameObject("Pivot");
            pivot.transform.SetParent(transform);
            pivot.transform.localPosition = Vector3.zero;
            pivot.transform.localRotation = Quaternion.identity;

        }

    }

    private void Update() {

        // If current camera is null, return.
        if (!RCC_SceneManager.Instance.activePlayerCamera)
            return;

        // If current camera is null, return.
        if (RCC_SceneManager.Instance.activePlayerCamera.cameraTarget == null)
            return;

        // If current camera is null, return.
        if (RCC_SceneManager.Instance.activePlayerCamera.cameraTarget.playerVehicle == null)
            return;

        Transform target = RCC_SceneManager.Instance.activePlayerCamera.cameraTarget.playerVehicle.transform;

        // Rotates smoothly towards to vehicle.
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, target.eulerAngles.y + 180f, transform.eulerAngles.z), Time.deltaTime * 3f);

        // Calculating target position.
        targetPosition = target.position;
        targetPosition -= transform.rotation * Vector3.forward * 10f;

        // Assigning transform.position to targetPosition.
        transform.position = targetPosition;

    }

}
