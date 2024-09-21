﻿//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RCC_AIBrakeZonesContainer))]
public class RCC_AIBZEditor : Editor {

    RCC_AIBrakeZonesContainer bzScript;

    public override void OnInspectorGUI() {

        bzScript = (RCC_AIBrakeZonesContainer)target;
        serializedObject.Update();

        if (GUILayout.Button("Delete Brake Zones")) {

            foreach (Transform t in bzScript.brakeZones)
                DestroyImmediate(t.gameObject);

            bzScript.brakeZones.Clear();
            EditorUtility.SetDirty(bzScript);

        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("brakeZones"), new GUIContent("Brake Zones", "Brake Zones"), true);

        EditorGUILayout.HelpBox("Create BrakeZones By Shift + Left Mouse Button On Your Road", MessageType.Info);

        if (GUI.changed)
            EditorUtility.SetDirty(bzScript);

        serializedObject.ApplyModifiedProperties();

    }

    private void OnSceneGUI() {

        Event e = Event.current;
        bzScript = (RCC_AIBrakeZonesContainer)target;

        if (e != null) {

            if (e.isMouse && e.shift && e.type == EventType.MouseDown) {

                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(ray, out hit, 5000.0f)) {

                    Vector3 newTilePosition = hit.point;

                    GameObject wp = new GameObject("Brake Zone " + bzScript.brakeZones.Count.ToString());

                    wp.transform.position = newTilePosition;
                    wp.transform.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                    wp.AddComponent<RCC_AIBrakeZone>();
                    wp.AddComponent<BoxCollider>();
                    wp.GetComponent<BoxCollider>().isTrigger = true;
                    wp.GetComponent<BoxCollider>().size = new Vector3(1, 1, 1);
                    wp.transform.SetParent(bzScript.transform);
                    GetBrakeZones();
                    Event.current.Use();

                }

            }

            if (bzScript)
                Selection.activeGameObject = bzScript.gameObject;

        }

        GetBrakeZones();

    }

    public void GetBrakeZones() {

        bzScript.brakeZones = new List<Transform>();

        Transform[] allTransforms = bzScript.transform.GetComponentsInChildren<Transform>();

        foreach (Transform t in allTransforms) {

            if (t != bzScript.transform)
                bzScript.brakeZones.Add(t);

        }

    }

}
