//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// UI input (float) receiver from UI Button. 
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/Mobile/RCC UI Controller Button")]
public class RCC_UIController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    private Button button;      //  UI button.
    private Slider slider;      //  UI slider.

    public float input = 0f;       //  Input as float.
    private float Sensitivity { get { return RCC_Settings.Instance.UIButtonSensitivity; } }     //  Sensitivity.
    private float Gravity { get { return RCC_Settings.Instance.UIButtonGravity; } }     //  Gravity.

    public bool pressing = false;       //  Is pressing now?

    private void Awake() {

        //  Getting components.
        button = GetComponent<Button>();
        slider = GetComponent<Slider>();

    }

    private void OnEnable() {

        //  Resetting on enable.
        input = 0f;
        pressing = false;

        if (slider)
            slider.value = 0f;

    }

    /// <summary>
    /// When pushed down the button.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData) {

        pressing = true;

    }

    /// <summary>
    /// When released the button.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(PointerEventData eventData) {

        pressing = false;

    }

    private void LateUpdate() {

        //  If button is not interactable, return with 0.
        if (button && !button.interactable) {

            pressing = false;
            input = 0f;
            return;

        }

        //  If slider is not interactable, return with 0.
        if (slider && !slider.interactable) {

            pressing = false;
            input = 0f;
            slider.value = 0f;
            return;

        }

        //  If slider selected, receive input. Otherwise, it's a button.
        if (slider) {

            if (pressing)
                input = slider.value;
            else
                input = 0f;

            slider.value = input;

        } else {

            if (pressing)
                input += Time.deltaTime * Sensitivity;
            else
                input -= Time.deltaTime * Gravity;

        }

        //  Clamping input.
        if (input < 0f)
            input = 0f;

        if (input > 1f)
            input = 1f;

    }

    private void OnDisable() {

        //  Resetting on disable.
        input = 0f;
        pressing = false;

        if (slider)
            slider.value = 0f;

    }

}
