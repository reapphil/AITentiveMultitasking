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
/// Manager for all upgradable sirens.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Customization/RCC Vehicle Upgrade Siren Manager")]
public class RCC_VehicleUpgrade_SirenManager : MonoBehaviour {

    //  Mod applier.
    private RCC_CustomizationApplier modApplier;
    public RCC_CustomizationApplier ModApplier {

        get {

            if (modApplier == null)
                modApplier = GetComponentInParent<RCC_CustomizationApplier>();

            return modApplier;

        }

    }

    public RCC_VehicleUpgrade_Siren[] sirens;        //  All sirens
    private int selectedIndex = -1;     //  Last selected siren index.

    private void Awake() {

        //  Disabling all sirens.
        for (int i = 0; i < sirens.Length; i++)
            sirens[i].gameObject.SetActive(false);

    }

    public void Initialize() {

        //  If sirens is null, return.
        if (sirens == null)
            return;

        //  Disabling all sirens.
        for (int i = 0; i < sirens.Length; i++)
            sirens[i].gameObject.SetActive(false);

        //  Getting index of the selected siren.
        selectedIndex = ModApplier.loadout.siren;

        //  If index is not -1, enable the corresponding siren.
        if (selectedIndex != -1)
            sirens[selectedIndex].gameObject.SetActive(true);

    }

    /// <summary>
    /// Unlocks the target index and saves it.
    /// </summary>
    /// <param name="index"></param>
    public void Upgrade(int index) {

        //  Index.
        selectedIndex = index;

        //  Disabling all sirens.
        for (int i = 0; i < sirens.Length; i++)
            sirens[i].gameObject.SetActive(false);

        //  If index is not -1, enable the corresponding siren.
        if (selectedIndex != -1)
            sirens[index].gameObject.SetActive(true);

        //  Assign index of the siren to loadout and save it.
        ModApplier.loadout.siren = selectedIndex;
        ModApplier.SaveLoadout();

    }

}
