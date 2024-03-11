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
using UnityEditor;

[CustomEditor(typeof(RCC_VehicleUpgrade_SpoilerManager))]
public class RCC_VehicleUpgrade_SpoilerEditor : Editor {

    RCC_VehicleUpgrade_SpoilerManager prop;

    public override void OnInspectorGUI() {

        prop = (RCC_VehicleUpgrade_SpoilerManager)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox("All spoilers can be used under this manager. Each spoiler has target index and paintable renderer. Click 'Get All Spoilers' after editing spoilers.", MessageType.None);

        DrawDefaultInspector();

        if (GUILayout.Button("Get All Spoilers"))
            prop.spoilers = prop.GetComponentsInChildren<RCC_VehicleUpgrade_Spoiler>(true);

        serializedObject.ApplyModifiedProperties();

    }

}
