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
using UnityEngine.EventSystems;

/// <summary>
/// Receiving inputs from the UI Joystick.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/Mobile/RCC UI Joystick")]
public class RCC_UIJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler {

    public RectTransform backgroundSprite;
    public RectTransform handleSprite;

    private Vector2 inputVector = Vector2.zero;
    public float InputHorizontal { get { return inputVector.x; } }
    public float InputVertical { get { return inputVector.y; } }

    private Vector2 joystickPosition = Vector2.zero;
    private readonly Camera _refCam;

    private void Start() {

        joystickPosition = RectTransformUtility.WorldToScreenPoint(_refCam, backgroundSprite.position);

    }

    private void OnEnable() {

        //  Resetting inputs of the joystick on enable / disable.
        inputVector = Vector2.zero;
        handleSprite.anchoredPosition = Vector2.zero;

    }

    /// <summary>
    /// While pressing and dragging the joystick.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrag(PointerEventData eventData) {

        Vector2 direction = eventData.position - joystickPosition;
        inputVector = (direction.magnitude > backgroundSprite.sizeDelta.x / 2f) ? direction.normalized : direction / (backgroundSprite.sizeDelta.x / 2f);
        handleSprite.anchoredPosition = (inputVector * backgroundSprite.sizeDelta.x / 2f) * 1f;

    }

    public void OnPointerUp(PointerEventData eventData) {

        inputVector = Vector2.zero;
        handleSprite.anchoredPosition = Vector2.zero;

    }

    public virtual void OnPointerDown(PointerEventData eventData) {

        //

    }

    private void OnDisable() {

        //  Resetting inputs of the joystick on enable / disable.
        inputVector = Vector2.zero;
        handleSprite.anchoredPosition = Vector2.zero;

    }

}
