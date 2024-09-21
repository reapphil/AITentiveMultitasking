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
/// Showroom camera while selecting the vehicles.
/// </summary>
public class RCC_CameraCarSelection : MonoBehaviour {

    public Transform target;        //  Camera target.
    public float distance = 10.0f;      //  Distance to the target.

    public float xSpeed = 250f;     //  X speed of the camera.
    public float ySpeed = 120f;     //  Y speed of the camera.

    public float yMinLimit = -20f;      //  Minimum Y angle of the camera.
    public float yMaxLimit = 80f;       //  Maximum Y angle of the camera.

    private float x = 0f;       //  Current X input.
    private float y = 0f;       //  Current Y input.

    private bool selfTurn = true;       //  Camera should turn around the target?
    private float selfTurnTime = 0f;

    private void Start() {

        //  Getting initial X and Y angles.
        x = transform.eulerAngles.y;
        y = transform.eulerAngles.x;

    }

    private void LateUpdate() {

        //  If there is no target, return.
        if (!target)
            return;

        //  If self turn is enabled, increase X related to time with multiplier.
        if (selfTurn)
            x += xSpeed / 2f * Time.deltaTime;

        //  Clamping Y angle.
        y = ClampAngle(y, yMinLimit, yMaxLimit);

        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(0f, 0f, -distance) + target.position;

        //  Setting position and rotation of the camera.
        transform.SetPositionAndRotation(position, rotation);

        //  Increasing self turn time with time.deltatime.
        if (selfTurnTime <= 1f)
            selfTurnTime += Time.deltaTime;

        if (selfTurnTime >= 1f)
            selfTurn = true;

    }

    /// <summary>
    /// Clamping angle.
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    private float ClampAngle(float angle, float min, float max) {

        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;

        return Mathf.Clamp(angle, min, max);

    }

    /// <summary>
    /// When player uses UI drag to rotate the camera.
    /// </summary>
    /// <param name="data"></param>
    public void OnDrag(BaseEventData data) {

        PointerEventData pointerData = data as PointerEventData;

        x += pointerData.delta.x * xSpeed * 0.02f;
        y -= pointerData.delta.y * ySpeed * 0.02f;

        y = ClampAngle(y, yMinLimit, yMaxLimit);

        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(0f, 0f, -distance) + target.position;

        transform.SetPositionAndRotation(position, rotation);

        selfTurn = false;
        selfTurnTime = 0f;

    }

}