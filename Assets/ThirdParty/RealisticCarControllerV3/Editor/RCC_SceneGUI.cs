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

public class RCC_SceneGUI : EditorWindow {

    static bool enabled;
    static GUISkin skin;

    static Texture2D cameraIcon;
    static Texture2D canvasIcon;
    static Texture2D hoodCameraIcon;
    static Texture2D wheelCameraIcon;
    static Texture2D headlightIcon;
    static Texture2D brakelightIcon;
    static Texture2D reverselightIcon;
    static Texture2D indicatorlightIcon;
    static Texture2D exhaustIcon;
    static Texture2D mirrorIcon;

    public static void GetImages() {

        skin = Resources.Load("RCC_WindowSkin") as GUISkin;

        cameraIcon = Resources.Load("Editor/CameraIcon", typeof(Texture2D)) as Texture2D;
        canvasIcon = Resources.Load("Editor/CanvasIcon", typeof(Texture2D)) as Texture2D;
        hoodCameraIcon = Resources.Load("Editor/HoodCameraIcon", typeof(Texture2D)) as Texture2D;
        wheelCameraIcon = Resources.Load("Editor/WheelCameraIcon", typeof(Texture2D)) as Texture2D;
        headlightIcon = Resources.Load("Editor/HeadlightIcon", typeof(Texture2D)) as Texture2D;
        brakelightIcon = Resources.Load("Editor/BrakelightIcon", typeof(Texture2D)) as Texture2D;
        reverselightIcon = Resources.Load("Editor/ReverselightIcon", typeof(Texture2D)) as Texture2D;
        indicatorlightIcon = Resources.Load("Editor/IndicatorlightIcon", typeof(Texture2D)) as Texture2D;
        exhaustIcon = Resources.Load("Editor/ExhaustIcon", typeof(Texture2D)) as Texture2D;
        mirrorIcon = Resources.Load("Editor/MirrorIcon", typeof(Texture2D)) as Texture2D;

    }

#if RCC_SHORTCUTS
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Enable In-Scene Buttons #e", false, 5000)]
    public static void Enable() {

        GetImages();

        if (!enabledMenu)
            SceneView.duringSceneGui += OnScene;

    }
#else
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Enable In-Scene Buttons", false, 5000)]
    public static void Enable() {

        if (enabled)
            return;

        GetImages();
        SceneView.duringSceneGui += OnScene;
        enabled = true;

    }
#endif

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Disable In-Scene Buttons", false, 5000)]
    public static void Disable() {

        SceneView.duringSceneGui -= OnScene;
        enabled = false;

    }

    private static void OnScene(SceneView sceneview) {

        GUI.skin = skin;

        Handles.BeginGUI();

        //	Scene buttons panel.
        GUILayout.BeginArea(new Rect(10f, 10f, 75f, 200f));

        GUILayout.BeginVertical("window");

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Scene");
        GUILayout.EndHorizontal();
        GUILayout.Space(4);

        if (GUILayout.Button(new GUIContent(cameraIcon, "Add / Select RCC Camera")))
            RCC_EditorWindows.CreateRCCCamera();

        if (GUILayout.Button(new GUIContent(canvasIcon, "Add / Select RCC Canvas")))
            RCC_EditorWindows.CreateRCCCanvas();



        Color defColor = GUI.color;
        GUI.color = Color.red;

        if (GUILayout.Button(new GUIContent(" X ", "Close the in-scene window. You can enable in Tools --> BCG --> RCC")))
            Disable();

        GUI.color = defColor;

        GUILayout.EndVertical();

        GUILayout.EndArea();

        //	Vehicle buttons panel
        GUILayout.BeginArea(new Rect(10f, 200f, 75f, 1000f));

        GUILayout.BeginVertical("window");

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Vehicle");
        GUILayout.EndHorizontal();

        if (Selection.activeGameObject != null) {

            if (Selection.activeGameObject.GetComponentInParent<RCC_CarControllerV3>()) {

                if (GUILayout.Button(new GUIContent(hoodCameraIcon, "Add/Select Hood Camera attached to selected vehicle")))
                    RCC_EditorWindows.CreateHoodCamera();

                if (GUILayout.Button(new GUIContent(wheelCameraIcon, "Add/Select Wheel Camera attached to selected vehicle")))
                    RCC_EditorWindows.CreateWheelCamera();

                if (GUILayout.Button(new GUIContent(headlightIcon, "Add headlights to selected vehicle")))
                    RCC_EditorWindows.CreateHeadLight();

                if (GUILayout.Button(new GUIContent(brakelightIcon, "Add brakelights to selected vehicle")))
                    RCC_EditorWindows.CreateBrakeLight();

                if (GUILayout.Button(new GUIContent(reverselightIcon, "Add reverselights to selected vehicle")))
                    RCC_EditorWindows.CreateReverseLight();

                if (GUILayout.Button(new GUIContent(indicatorlightIcon, "Add Indicatorlights to selected vehicle")))
                    RCC_EditorWindows.CreateIndicatorLight();

                if (GUILayout.Button(new GUIContent(exhaustIcon, "Add exhaust attached to selected vehicle")))
                    RCC_EditorWindows.CreateExhaust();

                if (GUILayout.Button(new GUIContent(mirrorIcon, "Add/Select mirrors attached to selected vehicle")))
                    RCC_EditorWindows.CreateMirrors(Selection.activeGameObject.GetComponentInParent<RCC_CarControllerV3>().gameObject);

                if (Selection.activeGameObject.GetComponent<RCC_Light>()) {

                    if (GUILayout.Button(new GUIContent(indicatorlightIcon, "Duplicate light to opposite direction")))
                        RCC_EditorWindows.DuplicateLight();

                }

            } else {

                GUILayout.Label("Select a RCC vehicle first");

            }

        } else {

            GUILayout.Label("Select a RCC vehicle first");

        }

        GUILayout.EndVertical();

        GUILayout.EndArea();

        Handles.EndGUI();

    }

}
