//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Applies emission texture to the target renderer.
/// </summary>
[System.Serializable]
public class RCC_Emission {

    public Renderer lightRenderer;      //  Renderer of the light.

    public int materialIndex = 0;       //  Index of the material.
    public bool noTexture = false;      //  Material has no texture.
    public bool applyAlpha = false;     //  Apply alpha channel.
    [Range(.1f, 10f)] public float multiplier = 1f;     //  Emission multiplier.
    private int emissionColorID;        //  ID of the emission color.

    private Material material;
    private Color targetColor;

    private bool initialized = false;

    /// <summary>
    /// Initializes the emission.
    /// </summary>
    public void Init() {

        //  If no renderer selected, return.
        if (!lightRenderer) {

            Debug.LogError("No renderer selected for emission! Selected a renderer for this light, or disable emission.");
            return;

        }

        material = lightRenderer.materials[materialIndex];      //  Getting correct material index.
        material.EnableKeyword("_EMISSION");        //  Enabling keyword of the material for emission.
        emissionColorID = Shader.PropertyToID("_EmissionColor");        //  Getting ID of the emission color.

        //  If material has no property for emission color, return.
        if (!material.HasProperty(emissionColorID))
            Debug.LogError("Material has no emission color id!");

        initialized = true;     //  Emission initialized.

    }

    /// <summary>
    /// Sets emissive strength of the material.
    /// </summary>
    /// <param name="sharedLight"></param>
    public void Emission(Light sharedLight) {

        //  If not initialized, initialize and return.
        if (!initialized) {

            Init();
            return;

        }

        //  If light is not enabled, return with 0 intensity.
        if (!sharedLight.enabled)
            targetColor = Color.white * 0f;

        //  If intensity of the light is close to 0, return with 0 intensity.
        if (Mathf.Approximately(sharedLight.intensity, 0f))
            targetColor = Color.white * 0f;

        //  If no texture option is enabled, set target color with light color. Otherwise, set target color with Color.white.
        if (!noTexture)
            targetColor = Color.white * sharedLight.intensity * multiplier;
        else
            targetColor = sharedLight.color * sharedLight.intensity * multiplier;

        //  If apply alpha is enabled, set color of the material with alpha channel.
        if (applyAlpha)
            targetColor = new Color(targetColor.r, targetColor.g, targetColor.b, sharedLight.intensity * multiplier);

        //  And finally, set color of the material with correct ID.
        if (material.GetColor(emissionColorID) != (targetColor))
            material.SetColor(emissionColorID, targetColor);

    }

}
