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
/// Customization demo used in the demo scene. Enables disables cameras and canvases.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Customization/RCC Customization Demo")]
public class RCC_CustomizationDemo : MonoBehaviour{

    private static RCC_CustomizationDemo instance;
    public static RCC_CustomizationDemo Instance {

        get {

            if (instance == null)
                instance = FindObjectOfType<RCC_CustomizationDemo>();

            return instance;

        }

    }

    private RCC_CarControllerV3 vehicle;
    public RCC_ShowroomCamera showroomCamera;
    public RCC_Camera RCCCamera;
    public GameObject RCCCanvas;
    public GameObject modificationCanvas;
    public Transform location;

    public void EnableCustomization(RCC_CarControllerV3 carController) {

        vehicle = carController;
        RCCCamera.gameObject.SetActive(false);
        showroomCamera.gameObject.SetActive(true);
        RCCCanvas.SetActive(false);
        modificationCanvas.SetActive(true);
        RCC.Transport(vehicle, location.position, location.rotation);
        RCC.SetControl(vehicle, false);

    }

    public void DisableCustomization() {

        RCCCamera.gameObject.SetActive(true);
        showroomCamera.gameObject.SetActive(false);
        RCCCanvas.SetActive(true);
        modificationCanvas.SetActive(false);
        RCC.SetControl(vehicle, true);
        vehicle = null;

    }

}
