//----------------------------------------------
//			Realistic Car Controller
//
// Copyright © 2023 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI paint button. 
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/Modification/RCC UI Color Button")]
public class RCC_UI_Color : MonoBehaviour {

    public PickedColor _pickedColor = PickedColor.Orange;
    public enum PickedColor { Orange, Red, Green, Blue, Black, White, Cyan, Magenta, Pink }

    public void OnClick() {

        RCC_CustomizationManager handler = RCC_CustomizationManager.Instance;

        if (!handler) {

            Debug.LogError("You are trying to customize the vehicle, but there is no ''RCC_CustomizationManager'' in your scene yet.");
            return;

        }

        Color selectedColor = new Color();

        switch (_pickedColor) {

            case PickedColor.Orange:
                selectedColor = Color.red + (Color.green / 2f);
                break;

            case PickedColor.Red:
                selectedColor = Color.red;
                break;

            case PickedColor.Green:
                selectedColor = Color.green;
                break;

            case PickedColor.Blue:
                selectedColor = Color.blue;
                break;

            case PickedColor.Black:
                selectedColor = Color.black;
                break;

            case PickedColor.White:
                selectedColor = Color.white;
                break;

            case PickedColor.Cyan:
                selectedColor = Color.cyan;
                break;

            case PickedColor.Magenta:
                selectedColor = Color.magenta;
                break;

            case PickedColor.Pink:
                selectedColor = new Color(1, 0f, .5f);
                break;

        }

        handler.Paint(selectedColor);

    }

}
