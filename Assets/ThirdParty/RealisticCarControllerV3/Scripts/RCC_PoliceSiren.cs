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
/// Police siren with operated lights.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Police Siren")]
public class RCC_PoliceSiren : MonoBehaviour {

    private RCC_AICarController AI;     //  If attached to the AI vehicle.

    //  Siren modes. On and Off.
    public SirenMode sirenMode = SirenMode.Off;
    public enum SirenMode { Off, On }

    //  Lights.
    public Light[] redLights;
    public Light[] blueLights;

    private void Awake() {

        //  Getting AI controller if attached.
        AI = GetComponentInParent<RCC_AICarController>();

    }

    private void Update() {

        //  If AI found, enable or disable the siren lights with chase mode. If AI is chasing a target, siren will be enabled.
        if (AI) {

            if (AI.targetChase != null)
                sirenMode = SirenMode.On;
            else
                sirenMode = SirenMode.Off;

        }

        //  If siren mode is set to off, set all intensity of the lights to 0. Otherwise, set to 1 with timer.
        switch (sirenMode) {

            case SirenMode.Off:

                for (int i = 0; i < redLights.Length; i++)
                    redLights[i].intensity = Mathf.Lerp(redLights[i].intensity, 0f, Time.deltaTime * 50f);

                for (int i = 0; i < blueLights.Length; i++)
                    blueLights[i].intensity = Mathf.Lerp(blueLights[i].intensity, 0f, Time.deltaTime * 50f);

                break;

            case SirenMode.On:

                if (Mathf.Approximately((int)(Time.time) % 2, 0) && Mathf.Approximately((int)(Time.time * 20) % 3, 0)) {

                    for (int i = 0; i < redLights.Length; i++)
                        redLights[i].intensity = Mathf.Lerp(redLights[i].intensity, 1f, Time.deltaTime * 50f);

                } else {

                    for (int i = 0; i < redLights.Length; i++)
                        redLights[i].intensity = Mathf.Lerp(redLights[i].intensity, 0f, Time.deltaTime * 10f);

                    if (Mathf.Approximately((int)(Time.time * 20) % 3, 0)) {

                        for (int i = 0; i < blueLights.Length; i++)
                            blueLights[i].intensity = Mathf.Lerp(blueLights[i].intensity, 1f, Time.deltaTime * 50f);

                    } else {

                        for (int i = 0; i < blueLights.Length; i++)
                            blueLights[i].intensity = Mathf.Lerp(blueLights[i].intensity, 0f, Time.deltaTime * 10f);

                    }

                }

                break;

        }

    }

    /// <summary>
    /// Sets the siren mode to on or off.
    /// </summary>
    /// <param name="state"></param>
    public void SetSiren(bool state) {

        if (state)
            sirenMode = SirenMode.On;
        else
            sirenMode = SirenMode.Off;

    }

}
