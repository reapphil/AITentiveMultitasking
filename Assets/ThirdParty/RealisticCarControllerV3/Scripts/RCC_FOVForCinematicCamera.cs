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
/// Animation attached to "Animation Pivot" of the Cinematic Camera is feeding FOV float value.
/// </summary>
public class RCC_FOVForCinematicCamera : MonoBehaviour {

    private RCC_CinematicCamera cinematicCamera;        //  Cinematic camera.
    public float FOV = 30f;     //  Target field of view.

    private void Awake() {

        //  Getting cinematic camera.
        cinematicCamera = GetComponentInParent<RCC_CinematicCamera>();

    }

    private void Update() {

        //  Setting field of view of the cinematic camera.
        cinematicCamera.targetFOV = FOV;

    }

}
