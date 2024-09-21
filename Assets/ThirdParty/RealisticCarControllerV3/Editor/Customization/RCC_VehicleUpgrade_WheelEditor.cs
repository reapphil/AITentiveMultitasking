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

[CustomEditor(typeof(RCC_VehicleUpgrade_WheelManager))]
public class RCC_VehicleUpgrade_WheelEditor : Editor {

    RCC_VehicleUpgrade_WheelManager prop;

    public override void OnInspectorGUI() {

        prop = (RCC_VehicleUpgrade_WheelManager)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox("All wheels are stored in the configure wheels section", MessageType.None);

        DrawDefaultInspector();

        if (GUILayout.Button("Configure Wheels"))
            Selection.activeObject = RCC_ChangableWheels.Instance;

        serializedObject.ApplyModifiedProperties();

    }

}
