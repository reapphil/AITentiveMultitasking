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
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RCC_AICarController)), CanEditMultipleObjects]
public class RCC_AIEditor : Editor {

    RCC_AICarController aiController;



    public override void OnInspectorGUI() {

        aiController = (RCC_AICarController)target;
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("navigationMode"), new GUIContent("Navigation Mode", "Navigation Mode."), false);

        EditorGUI.indentLevel++;

        if (aiController.navigationMode == RCC_AICarController.NavigationMode.FollowWaypoints) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("waypointsContainer"), new GUIContent("Waypoints Container", "Waypoints Container."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentWaypointIndex"), new GUIContent("Current Waypoint Index", "Current Waypoint Index."), false);

        } else {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetTag"), new GUIContent("Target Tag For Chase/Follow", "Target Tag For Chase/Follow."), false);

        }

        switch (aiController.navigationMode) {

            case RCC_AICarController.NavigationMode.FollowWaypoints:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopAfterLap"), new GUIContent("Stop After Target Lap", "Stops the vehicle if target lap achieved."), false);
                if (aiController.stopAfterLap)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("stopLap"), new GUIContent("Stop After Target Lap", "Stops the vehicle if target lap achieved."), false);
                break;

            case RCC_AICarController.NavigationMode.ChaseTarget:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("detectorRadius"), new GUIContent("Detector Radius", "Detector radius."), false);
                break;

            case RCC_AICarController.NavigationMode.FollowTarget:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("startFollowDistance"), new GUIContent("Start Follow Distance", "Start follow distance."), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopFollowDistance"), new GUIContent("Stop Follow Distance", "Stop follow distance."), false);
                break;

        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("useRaycasts"), new GUIContent("Use Raycasts", "Use Raycasts For Avoid Dynamic Objects."), false);

        if (aiController.useRaycasts) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacleLayers"), new GUIContent("Obstacle Layers", "Obstacle Layers For Avoid Dynamic Objects."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("raycastLength"), new GUIContent("Ray Distance", "Rays For Avoid Dynamic Objects."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("raycastAngle"), new GUIContent("Ray Angle", "Ray Angles."), false);

        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("limitSpeed"), new GUIContent("Limit Speed", "Limits The Speed."), false);

        if (aiController.limitSpeed)
            EditorGUILayout.Slider(serializedObject.FindProperty("maximumSpeed"), 0f, aiController.GetComponent<RCC_CarControllerV3>().maxspeed);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothedSteer"), new GUIContent("Smooth Steering", "Smooth Steering."), false);

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Current Waypoint: ", aiController.currentWaypointIndex.ToString());
        EditorGUILayout.LabelField("Laps: ", aiController.lap.ToString());
        EditorGUILayout.LabelField("Total Waypoints Passed: ", aiController.totalWaypointPassed.ToString());
        EditorGUILayout.LabelField("Obstacle: ", aiController.obstacle != null ? aiController.obstacle.ToString() : "None");
        EditorGUILayout.LabelField("Ignoring Waypoint Due To Unexpected Obstacle: ", aiController.ignoreWaypointNow.ToString());
        EditorGUILayout.Separator();

        if (GUI.changed)
            EditorUtility.SetDirty(aiController);

        serializedObject.ApplyModifiedProperties();

    }

}
