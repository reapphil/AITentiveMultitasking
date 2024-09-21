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
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI Steering Wheel controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/Mobile/RCC UI Steering Wheel")]
public class RCC_UISteeringWheelController : MonoBehaviour {

    private GameObject steeringWheelGameObject;
    private Image steeringWheelTexture;

    public float input = 0f;

    private float steeringWheelAngle = 0f;
    public float steeringWheelMaximumsteerAngle = 270f;
    public float steeringWheelResetPosSpeed = 20f;
    public float steeringWheelCenterDeadZoneRadius = 5f;

    private RectTransform steeringWheelRect;
    private CanvasGroup steeringWheelCanvasGroup;

    private float steeringWheelTempAngle, steeringWheelNewAngle = 0f;
    private bool steeringWheelPressed = false;

    private Vector2 steeringWheelCenter = new Vector2();
    private Vector2 steeringWheelTouchPos = new Vector2();

    private EventTrigger eventTrigger;

    private void Awake() {

        //	Initializing the ui wheel with proper event triggers.
        steeringWheelTexture = GetComponent<Image>();

        if (steeringWheelRect && !steeringWheelTexture)
            return;

        steeringWheelGameObject = steeringWheelTexture.gameObject;
        steeringWheelRect = steeringWheelTexture.rectTransform;
        steeringWheelCanvasGroup = steeringWheelTexture.GetComponent<CanvasGroup>();
        steeringWheelCenter = steeringWheelRect.position;

        SteeringWheelEventsInit();

    }

    private void OnEnable() {

        //  Make sure input is 0 on enable / disable.
        steeringWheelPressed = false;
        input = 0f;

    }

    private void LateUpdate() {

        //	No need to go further if current controller is not mobile controller.
        if (RCC_Settings.Instance.mobileController != RCC_Settings.MobileController.SteeringWheel)
            return;

        //	Receiving input.
        input = GetSteeringWheelInput();

        //	Visual steering wheel controlling.
        SteeringWheelControlling();

    }

    /// <summary>
    /// Events Initialization For Steering Wheel.
    /// </summary>
    private void SteeringWheelEventsInit() {

        eventTrigger = steeringWheelGameObject.GetComponent<EventTrigger>();

        var a = new EventTrigger.TriggerEvent();
        a.AddListener(data => {
            var evData = (PointerEventData)data;
            data.Use();

            steeringWheelPressed = true;
            steeringWheelTouchPos = evData.position;
            steeringWheelTempAngle = Vector2.Angle(Vector2.up, evData.position - steeringWheelCenter);
        });

        eventTrigger.triggers.Add(new EventTrigger.Entry { callback = a, eventID = EventTriggerType.PointerDown });


        var b = new EventTrigger.TriggerEvent();
        b.AddListener(data => {
            var evData = (PointerEventData)data;
            data.Use();
            steeringWheelTouchPos = evData.position;
        });

        eventTrigger.triggers.Add(new EventTrigger.Entry { callback = b, eventID = EventTriggerType.Drag });


        var c = new EventTrigger.TriggerEvent();
        c.AddListener(data => {
            steeringWheelPressed = false;
        });

        eventTrigger.triggers.Add(new EventTrigger.Entry { callback = c, eventID = EventTriggerType.EndDrag });

    }

    /// <summary>
    /// Get input of the steering wheel.
    /// </summary>
    /// <returns></returns>
    public float GetSteeringWheelInput() {

        return Mathf.Round(steeringWheelAngle / steeringWheelMaximumsteerAngle * 100) / 100;

    }

    /// <summary>
    /// Is steering wheel pressed right now?
    /// </summary>
    /// <returns></returns>
    public bool IsPressed() {

        return steeringWheelPressed;

    }

    /// <summary>
    /// Steering wheel controlling.
    /// </summary>
    private void SteeringWheelControlling() {

        if (!steeringWheelCanvasGroup || !steeringWheelRect || RCC_Settings.Instance.mobileController != RCC_Settings.MobileController.SteeringWheel) {

            if (steeringWheelGameObject.activeSelf)
                steeringWheelGameObject.SetActive(false);

            return;

        }

        if (!steeringWheelGameObject.activeSelf)
            steeringWheelGameObject.SetActive(true);

        if (steeringWheelPressed) {

            steeringWheelNewAngle = Vector2.Angle(Vector2.up, steeringWheelTouchPos - steeringWheelCenter);

            if (Vector2.Distance(steeringWheelTouchPos, steeringWheelCenter) > steeringWheelCenterDeadZoneRadius) {

                if (steeringWheelTouchPos.x > steeringWheelCenter.x)
                    steeringWheelAngle += steeringWheelNewAngle - steeringWheelTempAngle;
                else
                    steeringWheelAngle -= steeringWheelNewAngle - steeringWheelTempAngle;

            }

            if (steeringWheelAngle > steeringWheelMaximumsteerAngle)
                steeringWheelAngle = steeringWheelMaximumsteerAngle;
            else if (steeringWheelAngle < -steeringWheelMaximumsteerAngle)
                steeringWheelAngle = -steeringWheelMaximumsteerAngle;

            steeringWheelTempAngle = steeringWheelNewAngle;

        } else {

            if (!Mathf.Approximately(0f, steeringWheelAngle)) {

                float deltaAngle = steeringWheelResetPosSpeed;

                if (Mathf.Abs(deltaAngle) > Mathf.Abs(steeringWheelAngle)) {
                    steeringWheelAngle = 0f;
                    return;
                }

                steeringWheelAngle = Mathf.MoveTowards(steeringWheelAngle, 0f, deltaAngle * (Time.deltaTime * 100f));

            }

        }

        steeringWheelRect.eulerAngles = new Vector3(0f, 0f, -steeringWheelAngle);

    }

    private void OnDisable() {

        //  Make sure input is 0 on enable / disable.
        steeringWheelPressed = false;
        input = 0f;

    }

}
