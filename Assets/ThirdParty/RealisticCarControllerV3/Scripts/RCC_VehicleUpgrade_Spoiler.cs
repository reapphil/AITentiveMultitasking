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

/// <summary>
/// Upgradable spoiler.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Customization/RCC Vehicle Upgrade Spoiler")]
public class RCC_VehicleUpgrade_Spoiler : MonoBehaviour {

    //  Mod applier.
    private RCC_CustomizationApplier modApplier;
    public RCC_CustomizationApplier ModApplier {

        get {

            if (modApplier == null)
                modApplier = GetComponentInParent<RCC_CustomizationApplier>();

            return modApplier;

        }

    }

    public MeshRenderer bodyRenderer;       //  Renderer of the spoiler.
    public int index = -1;       //  Material index of the renderer.
    private Color color = Color.gray;     //  Default color.

    private void OnEnable() {

        //  If index is set to -1, no need to paint it.
        if (index == -1)
            return;

        //  Getting saved color of the spoiler.
        if (ModApplier.loadout.paint != new Color(1f, 1f, 1f, 0f))
            color = ModApplier.loadout.paint;

        //  Painting target material.
        if (bodyRenderer)
            bodyRenderer.materials[index].color = color;
        else
            Debug.LogError("Body renderer of this spoiler is not selected!");

    }

    /// <summary>
    /// Painting.
    /// </summary>
    /// <param name="newColor"></param>
    public void UpdatePaint(Color newColor) {

        if (index == -1)
            return;

        if (bodyRenderer)
            bodyRenderer.materials[index].color = newColor;
        else
            Debug.LogError("Body renderer of this spoiler is not selected!");

    }

    private void Reset() {

        bodyRenderer = GetComponent<MeshRenderer>();

    }

}
