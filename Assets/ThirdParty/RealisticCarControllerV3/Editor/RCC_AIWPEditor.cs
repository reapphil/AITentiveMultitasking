//----------------------------------------------
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

[CustomEditor(typeof(RCC_AIWaypointsContainer))]
public class RCC_AIWPEditor : Editor {

    RCC_AIWaypointsContainer wpScript;

    public override void OnInspectorGUI() {

        wpScript = (RCC_AIWaypointsContainer)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox("Create Waypoints By Shift + Left Mouse Button On Your Road", MessageType.Info);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("waypoints"), new GUIContent("Waypoints", "Waypoints"), true);

        foreach (Transform item in wpScript.transform) {

            if (item.gameObject.GetComponent<RCC_Waypoint>() == null)
                item.gameObject.AddComponent<RCC_Waypoint>();

        }

        if (GUILayout.Button("Delete Waypoints")) {

            foreach (RCC_Waypoint t in wpScript.waypoints)
                DestroyImmediate(t.gameObject);

            wpScript.waypoints.Clear();
            EditorUtility.SetDirty(wpScript);

        }

        if (GUI.changed)
            EditorUtility.SetDirty(wpScript);

        serializedObject.ApplyModifiedProperties();

    }

    private void OnSceneGUI() {

        Event e = Event.current;
        wpScript = (RCC_AIWaypointsContainer)target;

        if (e != null) {

            if (e.isMouse && e.shift && e.type == EventType.MouseDown) {

                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(ray, out hit, 5000.0f)) {

                    Vector3 newTilePosition = hit.point;

                    GameObject wp = new GameObject("Waypoint " + wpScript.waypoints.Count.ToString());
                    wp.AddComponent<RCC_Waypoint>();
                    wp.transform.position = newTilePosition;
                    wp.transform.SetParent(wpScript.transform);

                    GetWaypoints();

                }

            }

            if (wpScript)
                Selection.activeGameObject = wpScript.gameObject;

        }

        GetWaypoints();

    }

    public void GetWaypoints() {

        wpScript.waypoints = new List<RCC_Waypoint>();

        RCC_Waypoint[] allTransforms = wpScript.transform.GetComponentsInChildren<RCC_Waypoint>();

        foreach (RCC_Waypoint t in allTransforms) {

            if (t != wpScript.transform)
                wpScript.waypoints.Add(t);

        }

    }

}
