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

[CustomEditor(typeof(RCC_CustomizationApplier))]
public class RCC_CustomizationApplierEditor : Editor {

    RCC_CustomizationApplier prop;

    public override void OnInspectorGUI() {

        prop = (RCC_CustomizationApplier)target;
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("saveFileName"), new GUIContent("Save File Name", "Save File Name."), false);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoLoadLoadout"), new GUIContent("Auto Load Loadout", "Loads all last changes."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loadout"), new GUIContent("Loadout", "Loadout."), true);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (!prop.SpoilerManager) {

            EditorGUILayout.HelpBox("Spoiler Manager not found!", MessageType.Error);

            if (GUILayout.Button("Create")) {

                GameObject create = Instantiate(Resources.Load<GameObject>("Customization Setups/Spoilers"), prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(Root().transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = Resources.Load<GameObject>("Customization Setups/Spoilers").name;

            }

        } else {

            EditorGUILayout.HelpBox("Spoiler Manager found!", MessageType.None);

            if (GUILayout.Button("Select", GUILayout.Width(150f)))
                Selection.activeObject = prop.SpoilerManager.gameObject;

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!prop.SirenManager) {

            EditorGUILayout.HelpBox("Siren Manager not found!", MessageType.Error);

            if (GUILayout.Button("Create")) {

                GameObject create = Instantiate(Resources.Load<GameObject>("Customization Setups/Sirens"), prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(Root().transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = Resources.Load<GameObject>("Customization Setups/Sirens").name;

            }

        } else {

            EditorGUILayout.HelpBox("Siren Manager found!", MessageType.None);

            if (GUILayout.Button("Select", GUILayout.Width(150f)))
                Selection.activeObject = prop.SirenManager.gameObject;

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!prop.UpgradeManager) {

            EditorGUILayout.HelpBox("Upgrade Manager not found!", MessageType.Error);

            if (GUILayout.Button("Create")) {

                GameObject create = Instantiate(Resources.Load<GameObject>("Customization Setups/Upgrades"), prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(Root().transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = Resources.Load<GameObject>("Customization Setups/Upgrades").name;

            }

        } else {

            EditorGUILayout.HelpBox("Upgrade Manager found!", MessageType.None);

            if (GUILayout.Button("Select", GUILayout.Width(150f)))
                Selection.activeObject = prop.UpgradeManager.gameObject;

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!prop.PaintManager) {

            EditorGUILayout.HelpBox("Paint Manager not found!", MessageType.Error);

            if (GUILayout.Button("Create")) {

                GameObject create = Instantiate(Resources.Load<GameObject>("Customization Setups/Paints"), prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(Root().transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = Resources.Load<GameObject>("Customization Setups/Paints").name;

            }

        } else {

            EditorGUILayout.HelpBox("Paint Manager found!", MessageType.None);

            if (GUILayout.Button("Select", GUILayout.Width(150f)))
                Selection.activeObject = prop.PaintManager.gameObject;

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!prop.WheelManager) {

            EditorGUILayout.HelpBox("Wheel Manager not found!", MessageType.Error);

            if (GUILayout.Button("Create")) {

                GameObject create = Instantiate(Resources.Load<GameObject>("Customization Setups/Wheels"), prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(Root().transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = Resources.Load<GameObject>("Customization Setups/Wheels").name;

            }

        } else {

            EditorGUILayout.HelpBox("Wheel Manager found!", MessageType.None);

            if (GUILayout.Button("Select", GUILayout.Width(150f)))
                Selection.activeObject = prop.WheelManager.gameObject;

        }

        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();

    }

    private GameObject Root() {

        GameObject root = null;

        if(prop.transform.Find("Customizations"))
            root = prop.transform.Find("Customizations").gameObject;

        if (root) {

            return root;

        } else {

            root = new GameObject("Customizations");
            root.transform.SetParent(prop.transform);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            return root;

        }

    }

}
