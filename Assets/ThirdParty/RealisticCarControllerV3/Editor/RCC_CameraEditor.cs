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

[CustomEditor(typeof(RCC_Camera))]
public class RCC_CameraEditor : Editor {

    RCC_Camera RCCCam;
    Color orgColor;

    public override void OnInspectorGUI() {

        RCCCam = (RCC_Camera)target;
        serializedObject.Update();
        orgColor = GUI.color;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Main Camera designed for RCC. Includes 6 different camera modes. Doesn't use many cameras for different modes like other assets. Just one single camera handles them.", MessageType.Info);
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraTarget"), new GUIContent("Camera Target", "Camera Target."), true);
        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pivot"), new GUIContent("Pivot of the Camera", "Pivot of the Camera."), false);
        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraMode"), new GUIContent("Current Camera Mode", "Current camera mode."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useAutoChangeCamera"), new GUIContent("Auto Change Camera Mode", "Auto changes camera mode with timer."), false);
        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("TPS", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSDistance"), new GUIContent("TPS Distance", "TPS distance."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSHeight"), new GUIContent("TPS Height", "TPS height."), false);
        EditorGUILayout.BeginHorizontal();
        float org = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 125f;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSLockX"), new GUIContent("TPS Lock X Angle", "TPS lock x angle."), false, GUILayout.MaxWidth(150f));
        GUILayout.FlexibleSpace();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSLockY"), new GUIContent("TPS Lock Y Angle", "TPS lock y angle."), false, GUILayout.MaxWidth(150f));
        GUILayout.FlexibleSpace();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSLockZ"), new GUIContent("TPS Lock Z Angle", "TPS lock z angle."), false, GUILayout.MaxWidth(150f));
        EditorGUIUtility.labelWidth = org;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSFreeFall"), new GUIContent("TPS Free Fall", "TPS free fall."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSDynamic"), new GUIContent("TPS Dynamic", "TPS dynamic."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSRotationDamping"), new GUIContent("TPS Rotation Damping", "TPS rotation damping."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSMinimumFOV"), new GUIContent("TPS Minimum FOV", "TPS minimum FOV."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSMaximumFOV"), new GUIContent("TPS Maximum FOV", "TPS maximum FOV."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSTiltMaximum"), new GUIContent("TPS Tilt Maximum", "TPS tilt maximum."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSTiltMultiplier"), new GUIContent("TPS Tilt Multiplier", "TPS tilt multiplier."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSYaw"), new GUIContent("TPS Yaw Angle", "TPS yaw angle."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSPitch"), new GUIContent("TPS Pitch Angle", "TPS pitch angle."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("zoomScrollMultiplier"), new GUIContent("Zoom Scroll Multiplier", "Zoom scroll multiplier."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumScroll"), new GUIContent("Zoom Scroll Minimum", "Zoom scroll minimum."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumScroll"), new GUIContent("Zoom Scroll Maximum", "Zoom scroll maximum."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSAutoFocus"), new GUIContent("Use Auto Focus", "Use auto focus."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSAutoReverse"), new GUIContent("Use Reverse", "Use reverse."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useOrbitInTPSCameraMode"), new GUIContent("Use Orbit", "Use Orbit in TPS camera mode."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSCollision"), new GUIContent("TPS Collision", "TPS collision."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSOffset"), new GUIContent("TPS Offset", "TPS offset."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TPSStartRotation"), new GUIContent("TPS Start Rotation", "TPS start rotation."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useOcclusion"), new GUIContent("Use Occlusion", "Use Occlusion."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("occlusionLayerMask"), new GUIContent("Occlusion LayerMask", "Occlusion layerMask."), false);

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("FPS", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useHoodCameraMode"), new GUIContent("Use Hood Camera Mode", "Shall we use hood damera mode?"), false);

        if (RCCCam.useHoodCameraMode) {

            EditorGUILayout.HelpBox("Be sure your vehicle has ''Hood Camera''. Camera will be parented to this gameobject. You can create it from Tools --> BCG --> RCC --> Camera --> Add Hood Camera.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hoodCameraFOV"), new GUIContent("Hood Camera FOV", "Hood damera FOV."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useOrbitInHoodCameraMode"), new GUIContent("Use Orbit", "Use orbit in hood camera mode."), false);

        }

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Wheel", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useWheelCameraMode"), new GUIContent("Use Wheel Camera Mode", "Shall we use wheel camera mode?"), false);

        if (RCCCam.useWheelCameraMode) {

            EditorGUILayout.HelpBox("Be sure your vehicle has ''Wheel Camera''. Camera will be parented to this gameobject. You can create it from Tools --> BCG --> RCC --> Camera --> Add Wheel Camera.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelCameraFOV"), new GUIContent("Wheel Camera FOV", "Wheel camera FOV."), false);

        }

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Fixed", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useFixedCameraMode"), new GUIContent("Use Fixed Camera Mode", "Shall we use fixed camera mode?"), false);

        if (RCCCam.useFixedCameraMode) {

            EditorGUILayout.HelpBox("Fixed Camera is overrided by ''Fixed Camera System'' on your scene.", MessageType.Info);

            EditorGUILayout.Space();

            if (!FindObjectOfType<RCC_FixedCamera>()) {

                GUI.color = Color.green;

                if (GUILayout.Button("Create Fixed Camera System")) {

                    GameObject fixedCamera = new GameObject("RCC_FixedCamera");
                    fixedCamera.transform.position = Vector3.zero;
                    fixedCamera.transform.rotation = Quaternion.identity;
                    fixedCamera.AddComponent<RCC_FixedCamera>();
                    EditorUtility.SetDirty(RCCCam);

                }

            } else {

                GUI.color = orgColor;

                if (GUILayout.Button("Select Fixed Camera System"))
                    Selection.activeGameObject = FindObjectOfType<RCC_FixedCamera>().gameObject;

            }

            GUI.color = orgColor;

        }

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Cinematic", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useCinematicCameraMode"), new GUIContent("Use Cinematic Camera Mode", "Shall we use cinematic camera mode?"), false);

        if (RCCCam.useCinematicCameraMode) {

            EditorGUILayout.HelpBox("Cinematic Camera is overrided by ''Cinematic Camera System'' on your scene.", MessageType.Info);

            EditorGUILayout.Space();

            if (!FindObjectOfType<RCC_CinematicCamera>()) {

                GUI.color = Color.green;

                if (GUILayout.Button("Create Cinematic Camera System")) {

                    Instantiate(RCC_Settings.Instance.cinematicCamera, Vector3.zero, Quaternion.identity);
                    EditorUtility.SetDirty(RCCCam);

                }

            } else {

                GUI.color = orgColor;

                if (GUILayout.Button("Select Cinematic Camera System"))
                    Selection.activeGameObject = FindObjectOfType<RCC_CinematicCamera>().gameObject;

            }

            GUI.color = orgColor;

        }

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Orbit", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitXSpeed"), new GUIContent("Orbit X Speed", "Orbit X speed."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitYSpeed"), new GUIContent("Orbit Y Speed", "Orbit Y speed."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitSmooth"), new GUIContent("Orbit Smooth", "Orbit smooth."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minOrbitY"), new GUIContent("Min Orbit Y", "Min orbit Y."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxOrbitY"), new GUIContent("Max Orbit Y", "Max orbit Y."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitReset"), new GUIContent("Resets orbit rotation after 2 seconds."), false);

        GUI.color = orgColor;

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Top-Down", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useTopCameraMode"), new GUIContent("Use Top Camera Mode", "Shall we use top camera mode?"), false);

        if (RCCCam.useTopCameraMode) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("useOrthoForTopCamera"), new GUIContent("Use Ortho Mode", "Use ortho mode."), false);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("topCameraDistance"), new GUIContent("Top Camera Distance", "Top camera distance"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("topCameraAngle"), new GUIContent("Top Camera Angle", "Top camera angle"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumZDistanceOffset"), new GUIContent("Top Camera Maximum Z Distance", "Top camera maximum Z distance"), false);

            if (RCCCam.useOrthoForTopCamera) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumOrtSize"), new GUIContent("Minimum Ortho Size", "Minimum ortho size related with vehicle speed."), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumOrtSize"), new GUIContent("Maximum Ortho Size", "Maximum ortho size related with vehicle speed."), false);
            } else {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumOrtSize"), new GUIContent("Minimum FOV", "Minimum FOV related with vehicle speed."), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumOrtSize"), new GUIContent("Maximum FOV", "Maximum FOV related with vehicle speed."), false);
            }

        }

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();

        if (GUI.changed)
            EditorUtility.SetDirty(RCCCam);

        serializedObject.ApplyModifiedProperties();

    }

}
