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
/// Manager for upgradable spoilers.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Customization/RCC Vehicle Upgrade Spoiler Manager")]
public class RCC_VehicleUpgrade_SpoilerManager : MonoBehaviour {

    //  Mod applier.
    private RCC_CustomizationApplier modApplier;
    public RCC_CustomizationApplier ModApplier {

        get {

            if (modApplier == null)
                modApplier = GetComponentInParent<RCC_CustomizationApplier>();

            return modApplier;

        }

    }

    public RCC_VehicleUpgrade_Spoiler[] spoilers;        //  All upgradable spoilers.

    private int selectedIndex = -1;     //  Last selected spoiler index.
    public bool paintSpoilers = true;       //  Painting the spoilers?

    private void Awake() {

        //  Disabling all spoilers.
        for (int i = 0; i < spoilers.Length; i++)
            spoilers[i].gameObject.SetActive(false);

    }

    public void Initialize() {

        //  Disabling all spoilers.
        for (int i = 0; i < spoilers.Length; i++)
            spoilers[i].gameObject.SetActive(false);

        //  Getting index of the loadouts spoiler.
        selectedIndex = ModApplier.loadout.spoiler;

        //  If index is not -1, enable the corresponding spoiler.
        if (selectedIndex != -1)
            spoilers[selectedIndex].gameObject.SetActive(true);

    }

    /// <summary>
    /// Unlocks target spoiler index and saves it.
    /// </summary>
    /// <param name="index"></param>
    public void Upgrade(int index) {

        //  Index of the spoiler.
        selectedIndex = index;

        //  Disabling all spoilers.
        for (int i = 0; i < spoilers.Length; i++)
            spoilers[i].gameObject.SetActive(false);

        //  If index is not -1, enable the corresponding spoiler.
        if (index != -1)
            spoilers[index].gameObject.SetActive(true);
        
        //  Assign the spoiler index to loadout and save it.
        ModApplier.loadout.spoiler = selectedIndex;
        ModApplier.SaveLoadout();

    }

    /// <summary>
    /// Painting.
    /// </summary>
    /// <param name="newColor"></param>
    public void Paint(Color newColor) {

        //  Painting all spoilers.
        for (int i = 0; i < spoilers.Length; i++) {

            if(spoilers[i].bodyRenderer)
                spoilers[i].UpdatePaint(newColor);

        }

    }

}
