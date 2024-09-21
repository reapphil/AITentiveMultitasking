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
/// Customization loadout.
/// </summary>
[System.Serializable]
public class RCC_CustomizationLoadout{

    public Color paint = new Color(1f, 1f, 1f, 0f);
    public int spoiler = -1;
    public int siren = -1;
    public int wheel = -1;

    public int engineLevel = 0;
    public int handlingLevel = 0;
    public int brakeLevel = 0;

}
