//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Stored all general shared RCC settings here.
/// </summary>
[System.Serializable]
public class RCC_InitialSettings : ScriptableObject {

    #region singleton
    private static RCC_InitialSettings instance;
    public static RCC_InitialSettings Instance { get { if (instance == null) instance = Resources.Load("RCC Assets/RCC_InitialSettings") as RCC_InitialSettings; return instance; } }
    #endregion

    //  Wheel frictions.
    [Header("Wheel Frictions Forward")]
    public float forwardExtremumSlip = .375f;
    public float forwardExtremumValue = 1f;
    public float forwardAsymptoteSlip = .8f;
    public float forwardAsymptoteValue = .5f;
    public float forwardStiffness = 1.25f;

    [Header("Wheel Frictions Sideways")]
    public float sidewaysExtremumSlip = .275f;
    public float sidewaysExtremumValue = 1f;
    public float sidewaysAsymptoteSlip = .5f;
    public float sidewaysAsymptoteValue = .75f;
    public float sidewaysStiffness = 1.25f;

    [Header("Wheel Suspensions")]
    public float suspensionSpring = 45000f;
    public float suspensionDamping = 2500f;
    public float suspensionDistance = .2f;
    public float forceAppPoint = .1f;

    [Header("Rigidbody")]
    public float mass = 1500;
    public float drag = .01f;
    public float angularDrag = .5f;
    public RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate;

}
