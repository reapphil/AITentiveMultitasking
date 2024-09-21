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

/// <summary>
/// Color Picker with UI Sliders.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/RCC Color Picker By UI Sliders")]
public class RCC_ColorPickerBySliders : MonoBehaviour {

    public Color color;     // Main color.

    // Sliders per color channel.
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;

    private void Update() {

        // Assigning new color to main color.
        color = new Color(redSlider.value, greenSlider.value, blueSlider.value);

    }

}
