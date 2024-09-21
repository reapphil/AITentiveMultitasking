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

[CustomEditor(typeof(RCC_Settings))]
public class RCC_SettingsEditor : Editor {

    private RCC_Settings RCCSettingsAsset;

    private Color originalGUIColor;
    private Vector2 scrollPos;
    private PhysicMaterial[] physicMaterials;

    private bool foldGeneralSettings = false;
    private bool foldBehaviorSettings = false;
    private bool foldControllerSettings = false;
    private bool foldUISettings = false;
    private bool foldWheelPhysics = false;
    private bool foldSFX = false;
    private bool foldOptimization = false;
    private bool foldTagsAndLayers = false;

    public bool RCC_Shortcuts {

        get {

            bool _bool = RCC_Settings.Instance.useShortcuts;
            return _bool;

        }

        set {

            bool _bool = RCC_Settings.Instance.useShortcuts;

            if (_bool == value)
                return;

            RCC_Settings.Instance.useShortcuts = value;
            RCC_SetScriptingSymbol.SetEnabled("RCC_SHORTCUTS", value);

        }

    }

    private void OnEnable() {

        foldGeneralSettings = RCC_Settings.Instance.foldGeneralSettings;
        foldBehaviorSettings = RCC_Settings.Instance.foldBehaviorSettings;
        foldControllerSettings = RCC_Settings.Instance.foldControllerSettings;
        foldUISettings = RCC_Settings.Instance.foldUISettings;
        foldWheelPhysics = RCC_Settings.Instance.foldWheelPhysics;
        foldSFX = RCC_Settings.Instance.foldSFX;
        foldOptimization = RCC_Settings.Instance.foldOptimization;
        foldTagsAndLayers = RCC_Settings.Instance.foldTagsAndLayers;

    }

    private void OnDestroy() {

        RCC_Settings.Instance.foldBehaviorSettings = foldBehaviorSettings;
        RCC_Settings.Instance.foldControllerSettings = foldControllerSettings;
        RCC_Settings.Instance.foldUISettings = foldUISettings;
        RCC_Settings.Instance.foldWheelPhysics = foldWheelPhysics;
        RCC_Settings.Instance.foldSFX = foldSFX;
        RCC_Settings.Instance.foldOptimization = foldOptimization;
        RCC_Settings.Instance.foldTagsAndLayers = foldTagsAndLayers;

    }

    public override void OnInspectorGUI() {

        RCCSettingsAsset = (RCC_Settings)target;
        serializedObject.Update();

        originalGUIColor = GUI.color;
        EditorGUIUtility.labelWidth = 250;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("RCC Settings Editor Window", EditorStyles.boldLabel);
        GUI.color = new Color(.75f, 1f, .75f);
        EditorGUILayout.LabelField("This editor will keep update necessary .asset files in your project for RCC. Don't change directory of the ''Resources/RCC Assets''.", EditorStyles.helpBox);
        GUI.color = originalGUIColor;
        EditorGUILayout.Space();

        EditorGUI.indentLevel++;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

        EditorGUILayout.Space();

        foldGeneralSettings = EditorGUILayout.Foldout(foldGeneralSettings, "General Settings");

        if (foldGeneralSettings) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("General Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideFixedTimeStep"), new GUIContent("Override FixedTimeStep"));

            if (RCCSettingsAsset.overrideFixedTimeStep)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fixedTimeStep"), new GUIContent("Fixed Timestep"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngularVelocity"), new GUIContent("Maximum Angular Velocity"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideFPS"), new GUIContent("Override FPS"));

            if (RCCSettingsAsset.overrideFPS)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxFPS"), new GUIContent("Maximum FPS"));

            EditorGUILayout.HelpBox("You can find all references to any mode. Open up ''RCC_Settings.cs'' and right click to any mode. Hit ''Find references'' to find all modifications.", MessageType.Info);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("useFixedWheelColliders"), new GUIContent("Use Fixed WheelColliders", "Improves stability by increasing mass of the WheelColliders."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lockAndUnlockCursor"), new GUIContent("Locks Cursor", "Locks Cursor."));

            RCC_Shortcuts = EditorGUILayout.Toggle(new GUIContent("Use Editor Shortcuts", "It will enable shortcuts. Shift + E = In-Scene GUI, Shift + R = Add Main Car Controller, Shift + S = RCC Settings."), RCC_Shortcuts);

            if (RCC_Shortcuts)
                EditorGUILayout.HelpBox("Shift + E = In-Scene GUI, Shift + R = Add Main Car Controller, Shift + S = RCC Settings.", MessageType.None, true);

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

        foldBehaviorSettings = EditorGUILayout.Foldout(foldBehaviorSettings, "Behavior Settings");

        if (foldBehaviorSettings) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Behavior Settings", EditorStyles.boldLabel);

            GUI.color = new Color(.75f, 1f, .75f);
            EditorGUILayout.HelpBox("Using behavior preset will override wheelcollider settings, chassis joint, antirolls, and other stuff.", MessageType.Info);
            GUI.color = originalGUIColor;

            RCCSettingsAsset.overrideBehavior = EditorGUILayout.BeginToggleGroup("Override Behavior", RCCSettingsAsset.overrideBehavior);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("behaviorTypes"), new GUIContent("Behavior Types"), true);

            List<string> behaviorTypeStrings = new List<string>();

            GUI.color = new Color(.5f, 1f, 1f, 1f);

            for (int i = 0; i < RCCSettingsAsset.behaviorTypes.Length; i++)
                behaviorTypeStrings.Add(RCCSettingsAsset.behaviorTypes[i].behaviorName);

            RCCSettingsAsset.behaviorSelectedIndex = GUILayout.Toolbar(RCCSettingsAsset.behaviorSelectedIndex, behaviorTypeStrings.ToArray());

            EditorGUI.indentLevel--;
            GUI.color = originalGUIColor;

            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

        foldControllerSettings = EditorGUILayout.Foldout(foldControllerSettings, "Controller Settings");

        if (foldControllerSettings) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.Space();

            if (GUILayout.Button("Edit Inputs"))
                Selection.activeObject = Resources.Load(RCC_AssetPaths.inputsPath);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Mobile Controller Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("mobileControllerEnabled"), new GUIContent("Mobile Controller Enabled"));

            if (RCCSettingsAsset.mobileControllerEnabled) {

                EditorGUILayout.Space();
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Mobile UI controller buttons will be used.", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mobileController"), new GUIContent("Mobile Controller Type"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("UIButtonSensitivity"), new GUIContent("Mobile UI Button Sensitivity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("UIButtonGravity"), new GUIContent("Mobile UI Button Gravity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gyroSensitivity"), new GUIContent("Mobile Gyro Sensitivity"));
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Main Controller Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("units"), new GUIContent("Units"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoReset"), new GUIContent("Auto Reset"));

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

        foldUISettings = EditorGUILayout.Foldout(foldUISettings, "UI Settings");

        if (foldUISettings) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("UI Dashboard Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useTelemetry"), new GUIContent("Use Telemetry"));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

        foldWheelPhysics = EditorGUILayout.Foldout(foldWheelPhysics, "Wheel Physics Settings");

        if (foldWheelPhysics) {

            if (RCC_GroundMaterials.Instance.frictions != null && RCC_GroundMaterials.Instance.frictions.Length > 0) {

                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Ground Physic Materials", EditorStyles.boldLabel);

                physicMaterials = new PhysicMaterial[RCC_GroundMaterials.Instance.frictions.Length];

                for (int i = 0; i < physicMaterials.Length; i++) {
                    physicMaterials[i] = RCC_GroundMaterials.Instance.frictions[i].groundMaterial;
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.ObjectField("Ground Physic Materials " + i, physicMaterials[i], typeof(PhysicMaterial), false);
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();

            }

            GUI.color = new Color(.5f, 1f, 1f, 1f);

            if (GUILayout.Button("Configure Ground Physic Materials")) {
                Selection.activeObject = RCC_GroundMaterials.Instance;
            }

            GUI.color = originalGUIColor;

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

        foldSFX = EditorGUILayout.Foldout(foldSFX, "SFX Settings");

        if (foldSFX) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Sound FX", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            GUI.color = new Color(.5f, 1f, 1f, 1f);

            if (GUILayout.Button("Configure Wheel Slip Sounds"))
                Selection.activeObject = RCC_GroundMaterials.Instance;

            GUI.color = originalGUIColor;
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioMixer"), new GUIContent("Main Audio Mixer"), false);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("crashClips"), new GUIContent("Crashing Sounds"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gearShiftingClips"), new GUIContent("Gear Shifting Sounds"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("indicatorClip"), new GUIContent("Indicator Clip"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bumpClip"), new GUIContent("Wheel Bump Clip"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("exhaustFlameClips"), new GUIContent("Exhaust Flame Clips"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("NOSClip"), new GUIContent("NOS Clip"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("turboClip"), new GUIContent("Turbo Clip"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blowoutClip"), new GUIContent("Turbo Blowout Clip"), true);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("reversingClip"), new GUIContent("Reverse Transmission Sound"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("windClip"), new GUIContent("Wind Sound"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("brakeClip"), new GUIContent("Brake Sound"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelDeflateClip"), new GUIContent("Wheel Deflate Sound"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelInflateClip"), new GUIContent("Wheel Inflate Sound"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelFlatClip"), new GUIContent("Wheel Flat Sound"), true);
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxGearShiftingSoundVolume"), new GUIContent("Max Gear Shifting Sound Volume"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxCrashSoundVolume"), new GUIContent("Max Crash Sound Volume"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxWindSoundVolume"), new GUIContent("Max Wind Sound Volume"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxBrakeSoundVolume"), new GUIContent("Max Brake Sound Volume"), true);

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

        foldOptimization = EditorGUILayout.Foldout(foldOptimization, "Optimization");

        if (foldOptimization) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Optimization", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useHeadLightsAsVertexLights"), new GUIContent("Head Lights As Vertex Lights"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useBrakeLightsAsVertexLights"), new GUIContent("Brake Lights As Vertex Lights"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useReverseLightsAsVertexLights"), new GUIContent("Reverse Lights As Vertex Lights"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useIndicatorLightsAsVertexLights"), new GUIContent("Indicator Lights As Vertex Lights"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useOtherLightsAsVertexLights"), new GUIContent("Other Lights As Vertex Lights"));
            GUI.color = new Color(.75f, 1f, .75f);
            EditorGUILayout.HelpBox("Always use vertex lights for mobile platform. Even only one pixel light will drop your performance dramaticaly.", MessageType.Info);
            GUI.color = originalGUIColor;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dontUseAnyParticleEffects"), new GUIContent("Do Not Use Any Particle Effects"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dontUseSkidmarks"), new GUIContent("Do Not Use Skidmarks"));

            GUI.color = originalGUIColor;

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

        foldTagsAndLayers = EditorGUILayout.Foldout(foldTagsAndLayers, "Tags & Layers");

        if (foldTagsAndLayers) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Tags & Layers", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("setLayers"), new GUIContent("Set Layers Auto"), false);

            if (RCCSettingsAsset.setLayers) {

                EditorGUILayout.PropertyField(serializedObject.FindProperty("RCCLayer"), new GUIContent("Vehicle Layer"), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("WheelColliderLayer"), new GUIContent("WheelCollider Layer"), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DetachablePartLayer"), new GUIContent("DetachablePart Layer"), false);
                GUI.color = new Color(.75f, 1f, .75f);
                EditorGUILayout.HelpBox("Be sure you have that tag and layer in your Tags & Layers", MessageType.Warning);
                EditorGUILayout.HelpBox("All vehicles powered by Realistic Car Controller are using this layer. What does this layer do? It was used for masking wheel rays, light masks, and projector masks. Just create a new layer for vehicles from Edit --> Project Settings --> Tags & Layers, and select the layer here.", MessageType.Info);
                GUI.color = originalGUIColor;

            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("Resources", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("headLights"), new GUIContent("Head Lights"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("brakeLights"), new GUIContent("Brake Lights"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("reverseLights"), new GUIContent("Reverse Lights"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("indicatorLights"), new GUIContent("Indicator Lights"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lightTrailers"), new GUIContent("Light Trailers"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mirrors"), new GUIContent("Mirrors"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("skidmarksManager"), new GUIContent("Skidmarks Manager"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("exhaustGas"), new GUIContent("Exhaust Gas"), false);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("RCCMainCamera"), new GUIContent("RCC Main Camera"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hoodCamera"), new GUIContent("Hood Camera"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cinematicCamera"), new GUIContent("Cinematic Camera"), false);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("RCCCanvas"), new GUIContent("RCC UI Canvas"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("RCCTelemetry"), new GUIContent("RCC Telemetry Canvas"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("RCCModificationCanvas"), new GUIContent("RCC Modification Canvas"), false);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("contactParticles"), new GUIContent("Contact Particles On Collisions"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scratchParticles"), new GUIContent("Scratch Particles On Collisions"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelDeflateParticles"), new GUIContent("Wheel Deflate Particles"), false);

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.button);

        GUI.color = new Color(.5f, 1f, 1f, 1f);

        if (GUILayout.Button("Open PDF Documentation")) {

            string url = "http://www.bonecrackergames.com/realistic-car-controller";
            Application.OpenURL(url);

        }

        GUI.color = originalGUIColor;

        EditorGUILayout.LabelField("Realistic Car Controller " + RCC_Version.version + " \nBoneCracker Games", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

        EditorGUILayout.LabelField("Created by Buğra Özdoğanlar", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(RCCSettingsAsset);

    }

}
