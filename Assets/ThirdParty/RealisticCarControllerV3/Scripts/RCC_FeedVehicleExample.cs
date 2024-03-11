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
using UnityEngine.UI;

public class RCC_FeedVehicleExample : MonoBehaviour {

    public RCC_CarControllerV3 targetVehicle;
    public bool takePlayerVehicle = true;
    public RCC_Inputs newInputs = new RCC_Inputs();

    private bool overrideNow = false;

    public Slider throttle;
    public Slider brake;
    public Slider steering;
    public Slider handbrake;
    public Slider nos;

    private void Update() {

        newInputs.throttleInput = throttle.value;
        newInputs.brakeInput = brake.value;
        newInputs.steerInput = steering.value;
        newInputs.handbrakeInput = handbrake.value;
        newInputs.boostInput = nos.value;

        if (takePlayerVehicle)
            targetVehicle = RCC_SceneManager.Instance.activePlayerVehicle;

        if (targetVehicle && overrideNow)
            targetVehicle.OverrideInputs(newInputs);

    }

    public void EnableOverride() {

        if (!targetVehicle)
            return;

        overrideNow = true;

        if (targetVehicle)
            targetVehicle.OverrideInputs(newInputs);

    }

    public void DisableOverride() {

        if (!targetVehicle)
            return;

        overrideNow = false;

        if (targetVehicle)
            targetVehicle.DisableOverrideInputs();

    }

}
