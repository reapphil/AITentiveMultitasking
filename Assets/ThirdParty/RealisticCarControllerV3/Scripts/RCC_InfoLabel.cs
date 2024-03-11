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
/// Displays UI info.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/RCC UI Info Displayer")]
[RequireComponent(typeof(Text))]
public class RCC_InfoLabel : RCC_Singleton<RCC_InfoLabel> {

    private Text text;      //  UI text.
    private float timer = 1.5f;       //  Timeout to close the info panel.

    private void Awake() {

        //  Getting text component and disabling it.
        text = GetComponent<Text>();
        text.enabled = false;

    }

    private void OnEnable() {

        text.text = "";
        timer = 1.5f;

    }

    private void Update() {

        //  If timer is below 1.5, text is enabled. Otherwise disable.
        if (timer < 1.5f) {

            if (!text.enabled)
                text.enabled = true;

        } else {

            if (text.enabled)
                text.enabled = false;

        }

        //  Increasing timer.
        timer += Time.deltaTime;

    }

    /// <summary>
    /// Shows info.
    /// </summary>
    /// <param name="info"></param>
    public void ShowInfo(string info) {

        //  If no text found, return.
        if (!text)
            return;

        //  Display info.
        text.text = info;
        timer = 0f;

    }

    private void OnDisable() {

        text.text = "";
        timer = 1.5f;

    }

}
