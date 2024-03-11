//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

#pragma warning disable 0414

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// Mobile UI Drag used for orbiting RCC Camera.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/Mobile/RCC UI Drag")]
public class RCC_MobileUIDrag : MonoBehaviour, IDragHandler, IEndDragHandler {

    private void Awake() {

        //  If mobile controller is not enabled disable the gameobject and return.
        if (!RCC_Settings.Instance.mobileControllerEnabled) {

            gameObject.SetActive(false);
            return;

        }

    }

    /// <summary>
    /// While dragging.
    /// </summary>
    /// <param name="data"></param>
    public void OnDrag(PointerEventData data) {

        //  If mobile controller is not enabled, return.
        if (!RCC_Settings.Instance.mobileControllerEnabled)
            return;

        //  Return if no player camera found.
        if (!RCC_SceneManager.Instance.activePlayerCamera)
            return;

        RCC_SceneManager.Instance.activePlayerCamera.OnDrag(data);

    }

    public void OnEndDrag(PointerEventData data) {

        //  If mobile controller is not enabled, return.
        if (!RCC_Settings.Instance.mobileControllerEnabled)
            return;

    }

}
