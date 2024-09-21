//----------------------------------------------
//			Realistic Car Controller
//
// Copyright © 2023 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

#pragma warning disable 0414

using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// Mobile UI Drag used for orbiting Showroom Camera.
/// </summary>
public class RCC_UI_MobileDrag : MonoBehaviour, IDragHandler, IEndDragHandler {

    private RCC_ShowroomCamera showroomCamera;

    private void Awake() {

        showroomCamera = FindObjectOfType<RCC_ShowroomCamera>();

    }

    public void OnDrag(PointerEventData data) {

        if (showroomCamera)
            showroomCamera.OnDrag(data);

    }

    public void OnEndDrag(PointerEventData data) {



    }

}
