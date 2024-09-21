//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RCC_DemoMaterials))]
public class RCC_DemoMaterialsEditor : Editor {

    RCC_DemoMaterials prop;

    public override void OnInspectorGUI() {

        prop = (RCC_DemoMaterials)target;
        serializedObject.Update();

        DrawDefaultInspector();

        if (GUILayout.Button("Select All Demo Materials For Converting To URP")) {

            EditorUtility.DisplayDialog("Converting All Demo Materials To URP", "All demo materials will be selected in your project now. After that, you'll need to convert them to URP shaders while they have been selected. You can convert them from the Edit --> Render Pipeline --> Universal Render Pipeline --> Convert Selected Materials.", "Close");

            UnityEngine.Object[] objects = new UnityEngine.Object[prop.demoMaterials.Length];

            for (int i = 0; i < objects.Length; i++)
                objects[i] = prop.demoMaterials[i];

            Selection.objects = objects;

        }

        EditorGUILayout.LabelField("Created by Buğra Özdoğanlar\nBoneCrackerGames", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

}
