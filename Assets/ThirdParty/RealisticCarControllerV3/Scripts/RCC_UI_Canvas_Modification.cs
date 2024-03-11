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

/// <summary>
/// RCC Canvas for modification.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/RCC Canvas Modification")]
public class RCC_UI_Canvas_Modification : MonoBehaviour {

    //UI Panels.
    [Header("Modify Panels")]
    public GameObject colorClass;
    public GameObject wheelClass;
    public GameObject modificationClass;
    public GameObject upgradesClass;
    public GameObject decalsClass;
    public GameObject neonsClass;
    public GameObject spoilerClass;
    public GameObject sirenClass;

    //UI Buttons.
    [Header("Modify Buttons")]
    public Button bodyPaintButton;
    public Button rimButton;
    public Button customizationButton;
    public Button upgradeButton;
    public Button decalsButton;
    public Button neonsButton;
    public Button spoilersButton;
    public Button sirensButton;

    private Color orgButtonColor;

    //UI Texts.
    [Header("Upgrade Levels Texts")]
    public Text speedUpgradeLevel;
    public Text handlingUpgradeLevel;
    public Text brakeUpgradeLevel;

    private void Awake() {

        //Getting original color of the button.
        orgButtonColor = bodyPaintButton.image.color;

    }

    private void Update() {

        RCC_CustomizationApplier currentApplier = RCC_CustomizationManager.Instance.vehicle;

        // If no any player vehicle, disable all buttons and return.
        if (!currentApplier) {

            if (upgradeButton)
                upgradeButton.interactable = false;

            if (spoilersButton)
                spoilersButton.interactable = false;

            if (customizationButton)
                customizationButton.interactable = false;

            if (sirensButton)
                sirensButton.interactable = false;

            if (rimButton)
                rimButton.interactable = false;

            if (bodyPaintButton)
                bodyPaintButton.interactable = false;

            return;

        }

        // Setting interactable states of the buttons depending on upgrade managers. 
        //	Ex. If spoiler manager not found, spoiler button will be disabled.
        if (upgradeButton)
            upgradeButton.interactable = currentApplier.UpgradeManager;

        if (spoilersButton)
            spoilersButton.interactable = currentApplier.SpoilerManager;

        if (sirensButton)
            sirensButton.interactable = currentApplier.SirenManager;

        if (rimButton)
            rimButton.interactable = currentApplier.WheelManager;

        if (bodyPaintButton)
            bodyPaintButton.interactable = currentApplier.PaintManager;

        // Feeding upgrade level texts for engine, brake, and handling.
        if (upgradeButton.interactable) {

            if (speedUpgradeLevel)
                speedUpgradeLevel.text = currentApplier.UpgradeManager.engineLevel.ToString("F0");
            if (handlingUpgradeLevel)
                handlingUpgradeLevel.text = currentApplier.UpgradeManager.handlingLevel.ToString("F0");
            if (brakeUpgradeLevel)
                brakeUpgradeLevel.text = currentApplier.UpgradeManager.brakeLevel.ToString("F0");

        }

    }

    /// <summary>
    /// Opens up the target class panel.
    /// </summary>
    /// <param name="activeClass"></param>
    public void ChooseClass(GameObject activeClass) {

        if (colorClass)
            colorClass.SetActive(false);

        if (wheelClass)
            wheelClass.SetActive(false);

        if (modificationClass)
            modificationClass.SetActive(false);

        if (upgradesClass)
            upgradesClass.SetActive(false);

        if (decalsButton)
            decalsClass.SetActive(false);

        if (neonsClass)
            neonsClass.SetActive(false);

        if (spoilerClass)
            spoilerClass.SetActive(false);

        if (sirenClass)
            sirenClass.SetActive(false);

        if (activeClass)
            activeClass.SetActive(true);

    }

    /// <summary>
    /// Checks colors of the UI buttons. Ex. If paint class is enabled, color of the button will be green. 
    /// </summary>
    /// <param name="activeButton"></param>
    public void CheckButtonColors(Button activeButton) {

        if (bodyPaintButton)
            bodyPaintButton.image.color = orgButtonColor;

        if (rimButton)
            rimButton.image.color = orgButtonColor;

        if (customizationButton)
            customizationButton.image.color = orgButtonColor;

        if (upgradeButton)
            upgradeButton.image.color = orgButtonColor;

        if (decalsButton)
            decalsButton.image.color = orgButtonColor;

        if (neonsButton)
            neonsButton.image.color = orgButtonColor;

        if (spoilersButton)
            spoilersButton.image.color = orgButtonColor;

        if (sirensButton)
            sirensButton.image.color = orgButtonColor;

        activeButton.image.color = new Color(0f, 1f, 0f);

    }

    /// <summary>
    /// Sets auto rotation of the showrooom camera.
    /// </summary>
    /// <param name="state"></param>
    public void ToggleAutoRotation(bool state) {

        RCC_ShowroomCamera showroomCamera = FindObjectOfType<RCC_ShowroomCamera>();

        // If no any showroom camera, return.
        if (!showroomCamera)
            return;

        showroomCamera.ToggleAutoRotation(state);

    }

    /// <summary>
    /// Sets horizontal angle of the showroom camera.
    /// </summary>
    /// <param name="hor"></param>
    public void SetHorizontal(float hor) {

        RCC_ShowroomCamera showroomCamera = FindObjectOfType<RCC_ShowroomCamera>();

        // If no any showroom camera, return.
        if (!showroomCamera)
            return;

        showroomCamera.orbitX = hor;

    }
    /// <summary>
    /// Sets vertical angle of the showroom camera.
    /// </summary>
    /// <param name="ver"></param>
    public void SetVertical(float ver) {

        RCC_ShowroomCamera showroomCamera = FindObjectOfType<RCC_ShowroomCamera>();

        // If no any showroom camera, return.
        if (!showroomCamera)
            return;

        showroomCamera.orbitY = ver;

    }

    public void DisableCustomization() {

        if (RCC_CustomizationDemo.Instance)
            RCC_CustomizationDemo.Instance.DisableCustomization();

    }

}
