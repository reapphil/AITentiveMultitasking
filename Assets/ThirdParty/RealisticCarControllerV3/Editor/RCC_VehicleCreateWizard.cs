//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RCC_VehicleCreateWizard : EditorWindow {

    private GameObject currentSelectedObject;
    private RCC_CarControllerV3 currentSelectedVehicle;
    private int wheelSetup = 0;
    private Color guiColor;

    private const int windowWidth = 450;
    private const int windowHeight = 850;

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Open Vehicle Create Wizard", false, -80)]
    public static void OpenWindow() {

        GetWindow<RCC_VehicleCreateWizard>(true);

    }

    private void OnEnable() {

        titleContent = new GUIContent("Vehicle Create Wizard");
        maxSize = new Vector2(windowWidth, windowHeight);
        minSize = maxSize;
        guiColor = GUI.color;

    }

    private void OnGUI() {

        EditorGUILayout.LabelField("This wizard will guide to create new vehicles just in a few seconds.", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Please follow below steps carefully and build your new vehicle.", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Be sure your model is eligible for vehicle physics. Info in the documentations.", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Once you completed all steps, you won't need to use the wizard anymore.", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("You can configure your vehicle from the inspector panel afterwards.", EditorStyles.boldLabel);

        if (EditorApplication.isPlaying)
            return;

        if (Selection.gameObjects.Length > 1) {

            if (!EditorApplication.isPlaying)
                Repaint();

            return;

        }

        if (Selection.activeGameObject != null && !Selection.activeGameObject.activeSelf) {

            if (!EditorApplication.isPlaying)
                Repaint();

            return;

        }

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        currentSelectedObject = Selection.activeGameObject;

        if (currentSelectedObject != null && currentSelectedObject.GetComponentInParent<RCC_CarControllerV3>())
            currentSelectedVehicle = currentSelectedObject.GetComponentInParent<RCC_CarControllerV3>();
        else
            currentSelectedVehicle = null;

        bool isPersistent = EditorUtility.IsPersistent(currentSelectedObject);

        if (isPersistent) {

            EditorGUILayout.LabelField("Please select the model on your scene, not in the project.");
            EditorGUILayout.LabelField("Drag and drop your model to the scene to get started.");

            if (!EditorApplication.isPlaying)
                Repaint();

            return;

        }

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.LabelField("1. RCC_CarControllerV3", EditorStyles.boldLabel);

        if (currentSelectedVehicle == null)
            EditorGUILayout.LabelField("Please select the parent vehicle gameobject to add main controller.");

        if (currentSelectedVehicle != null)
            EditorGUILayout.ObjectField("Vehicle", currentSelectedVehicle, typeof(GameObject), true);

        if (currentSelectedObject != null && currentSelectedVehicle == null && GUILayout.Button("Create & Add Main Controller To " + "[" + currentSelectedObject.name + "]")) {

            bool verify = EditorUtility.DisplayDialog("Verify the model", "Are you sure selected gameobject is parent of your vehicle? Please don't select child gameobject of the model.", "Yes, it's parent object", "No, it's child object");

            if (verify)
                AddMainController(currentSelectedObject);

            EditorUtility.SetDirty(currentSelectedVehicle);

        }

        EditorGUILayout.EndVertical();

        if (!currentSelectedVehicle) {

            if (!EditorApplication.isPlaying)
                Repaint();

            return;

        }

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.LabelField("2. Wheels", EditorStyles.boldLabel);

        wheelSetup = 0;

        if (currentSelectedVehicle.FrontLeftWheelTransform != null)
            wheelSetup++;

        if (currentSelectedVehicle.FrontRightWheelTransform != null)
            wheelSetup++;

        if (currentSelectedVehicle.RearLeftWheelTransform != null)
            wheelSetup++;

        if (currentSelectedVehicle.RearRightWheelTransform != null)
            wheelSetup++;

        switch (wheelSetup) {

            case 0:

                EditorGUILayout.LabelField("Please select the proper wheel model of the vehicle and click the button.");
                EditorGUILayout.LabelField("Once you selected all wheel models, you can create their wheelcolliders.");

                if (GUILayout.Button("I've Selected [Front Left Wheel Model] Now")) {

                    currentSelectedVehicle.FrontLeftWheelTransform = Selection.activeGameObject.transform;
                    EditorUtility.SetDirty(currentSelectedVehicle);

                }

                break;

            case 1:

                if (GUILayout.Button("I've Selected [Front Right Wheel Model] Now")) {

                    currentSelectedVehicle.FrontRightWheelTransform = Selection.activeGameObject.transform;
                    EditorUtility.SetDirty(currentSelectedVehicle);

                }

                break;

            case 2:

                if (GUILayout.Button("I've Selected [Rear Left Wheel Model] Now")) {

                    currentSelectedVehicle.RearLeftWheelTransform = Selection.activeGameObject.transform;
                    EditorUtility.SetDirty(currentSelectedVehicle);

                }

                break;

            case 3:

                if (GUILayout.Button("I've Selected [Rear Right Wheel Model] Now")) {

                    currentSelectedVehicle.RearRightWheelTransform = Selection.activeGameObject.transform;
                    EditorUtility.SetDirty(currentSelectedVehicle);

                }

                break;

        }

        if (GUILayout.Button("Select All Wheels Again")) {

            currentSelectedVehicle.FrontLeftWheelTransform = null;
            currentSelectedVehicle.FrontRightWheelTransform = null;
            currentSelectedVehicle.RearLeftWheelTransform = null;
            currentSelectedVehicle.RearRightWheelTransform = null;
            wheelSetup = 0;

            bool hasWheelColliders = false;

            if (currentSelectedVehicle.transform.Find("Wheel Colliders"))
                hasWheelColliders = true;

            if (hasWheelColliders)
                DestroyImmediate(currentSelectedVehicle.transform.Find("Wheel Colliders").gameObject);

            EditorUtility.SetDirty(currentSelectedVehicle);

        }

        if (currentSelectedVehicle.FrontLeftWheelTransform)
            EditorGUILayout.ObjectField("Front Left Wheel", currentSelectedVehicle.FrontLeftWheelTransform.gameObject, typeof(GameObject), true);

        if (currentSelectedVehicle.FrontRightWheelTransform)
            EditorGUILayout.ObjectField("Front Right Wheel", currentSelectedVehicle.FrontRightWheelTransform.gameObject, typeof(GameObject), true);

        if (currentSelectedVehicle.RearLeftWheelTransform)
            EditorGUILayout.ObjectField("Rear Left Wheel", currentSelectedVehicle.RearLeftWheelTransform.gameObject, typeof(GameObject), true);

        if (currentSelectedVehicle.RearRightWheelTransform)
            EditorGUILayout.ObjectField("Rear Right Wheel", currentSelectedVehicle.RearRightWheelTransform.gameObject, typeof(GameObject), true);

        if (currentSelectedVehicle.FrontLeftWheelTransform && currentSelectedVehicle.FrontRightWheelTransform && currentSelectedVehicle.RearLeftWheelTransform && currentSelectedVehicle.RearRightWheelTransform) {

            bool hasWheelColliders = false;

            if (currentSelectedVehicle.transform.Find("Wheel Colliders"))
                hasWheelColliders = true;

            if (!hasWheelColliders) {

                if (GUILayout.Button("Create WheelColliders")) {

                    currentSelectedVehicle.CreateWheelColliders();

                    bool createCenter = EditorUtility.DisplayDialog("Create WheelColliders", "Do you want to create wheelcollider at the center of the wheel, or with suspension distance?", "Center", "With Suspension Distance");

                    if (createCenter) {

                        RCC_WheelCollider[] wheels = currentSelectedVehicle.GetComponentsInChildren<RCC_WheelCollider>();

                        foreach (RCC_WheelCollider wc in wheels)
                            wc.transform.position += currentSelectedVehicle.transform.up * (wc.WheelCollider.suspensionDistance / 2f);

                    }

                    EditorUtility.SetDirty(currentSelectedVehicle);

                }

            } else {

                if (GUILayout.Button("Delete WheelColliders")) {

                    DestroyImmediate(currentSelectedVehicle.transform.Find("Wheel Colliders").gameObject);
                    EditorUtility.SetDirty(currentSelectedVehicle);

                }

            }

        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.LabelField("3. COM", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Please place the COM (Center of mass) correctly.");
        EditorGUILayout.LabelField("If you are not sure how to place it, you can use the auto place button.");

        if (GUILayout.Button("Select And Place COM"))
            Selection.activeGameObject = currentSelectedVehicle.COM.gameObject;

        if (GUILayout.Button("Auto Place COM")) {

            Selection.activeGameObject = currentSelectedVehicle.COM.gameObject;
            currentSelectedVehicle.COM.transform.localPosition = new Vector3(0f, currentSelectedVehicle.FrontLeftWheelCollider.transform.localPosition.y - (currentSelectedVehicle.FrontLeftWheelCollider.GetComponent<WheelCollider>().suspensionDistance / 2f), currentSelectedVehicle.FrontLeftWheelCollider.transform.localPosition.z + currentSelectedVehicle.RearLeftWheelCollider.transform.localPosition.z);
            EditorUtility.SetDirty(currentSelectedVehicle);

        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.LabelField("4. Collider", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Be sure your vehicle has a mesh collider or box collider.");
        EditorGUILayout.LabelField("You can select main part of the vehicle and a mesh / box collider.");

        Collider[] colliders = currentSelectedVehicle.GetComponentsInChildren<Collider>();
        bool colliderFound = false;

        for (int i = 0; i < colliders.Length; i++) {

            if (!(colliders[i] as WheelCollider)) {

                colliderFound = true;
                EditorGUILayout.ObjectField("Found Collider ", colliders[i].gameObject, typeof(GameObject), true);

            }

        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.LabelField("5. Addons", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("You can add cameras, lights, exhausts, and all other essentials with in-screen UI.");

        if (GUILayout.Button("Enable In-Scene UI Buttons")) {

            RCC_SceneGUI.Enable();

        }

        if (GUILayout.Button("Disable In-Scene UI Buttons")) {

            RCC_SceneGUI.Disable();

        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.LabelField("6. Status", EditorStyles.boldLabel);

        if (currentSelectedVehicle)
            GUI.color = Color.green;
        else
            GUI.color = Color.red;

        EditorGUILayout.LabelField("Car Controller", EditorStyles.boldLabel);

        if (currentSelectedVehicle.FrontLeftWheelTransform && currentSelectedVehicle.FrontRightWheelTransform && currentSelectedVehicle.RearLeftWheelTransform && currentSelectedVehicle.RearRightWheelTransform && currentSelectedVehicle.FrontLeftWheelCollider && currentSelectedVehicle.FrontRightWheelCollider && currentSelectedVehicle.RearLeftWheelCollider && currentSelectedVehicle.RearRightWheelCollider)
            GUI.color = Color.green;
        else
            GUI.color = Color.red;

        EditorGUILayout.LabelField("Wheel Models & Wheel Colliders", EditorStyles.boldLabel);

        GUI.color = guiColor;

        if (colliderFound)
            GUI.color = Color.green;
        else
            GUI.color = Color.red;

        EditorGUILayout.LabelField("Colliders", EditorStyles.boldLabel);

        EditorGUILayout.EndVertical();

        if (!EditorApplication.isPlaying)
            Repaint();

    }

    private void AddMainController(GameObject currentSelectedObject) {

        wheelSetup = 0;

        if (!currentSelectedObject.GetComponentInParent<RCC_CarControllerV3>()) {

            bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(currentSelectedObject);

            if (isPrefab) {

                bool isModelPrefab = PrefabUtility.IsPartOfModelPrefab(currentSelectedObject);
                bool unpackPrefab = EditorUtility.DisplayDialog("Unpack Prefab", "This gameobject is connected to a " + (isModelPrefab ? "model" : "") + " prefab. Would you like to unpack the prefab completely? If you don't unpack it, you won't be able to move, reorder, or delete any children instance of the prefab.", "Unpack", "Don't Unpack");

                if (unpackPrefab)
                    PrefabUtility.UnpackPrefabInstance(currentSelectedObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            }

            bool fixPivot = EditorUtility.DisplayDialog("Fix Pivot Position Of The Vehicle", "Would you like to fix pivot position of the vehicle? If your vehicle has correct pivot position, select no.", "Fix", "No");

            if (fixPivot) {

                GameObject pivot = new GameObject(currentSelectedObject.name);
                pivot.transform.position = RCC_GetBounds.GetBoundsCenter(currentSelectedObject.transform);
                pivot.transform.rotation = currentSelectedObject.transform.rotation;

                pivot.AddComponent<RCC_CarControllerV3>();

                Rigidbody rigid = pivot.GetComponent<Rigidbody>();
                rigid.mass = RCC_InitialSettings.Instance.mass;
                rigid.drag = RCC_InitialSettings.Instance.drag;
                rigid.angularDrag = RCC_InitialSettings.Instance.angularDrag;
                rigid.interpolation = RCC_InitialSettings.Instance.interpolation;

                currentSelectedObject.transform.SetParent(pivot.transform);
                Selection.activeGameObject = pivot;

            } else {

                currentSelectedObject.AddComponent<RCC_CarControllerV3>();

                Rigidbody rigid = currentSelectedObject.GetComponent<Rigidbody>();
                rigid.mass = RCC_InitialSettings.Instance.mass;
                rigid.drag = RCC_InitialSettings.Instance.drag;
                rigid.angularDrag = RCC_InitialSettings.Instance.angularDrag;
                rigid.interpolation = RCC_InitialSettings.Instance.interpolation;

                Selection.activeGameObject = currentSelectedObject;

            }

        } else {

            EditorUtility.DisplayDialog("Your Gameobject Already Has Realistic Car Controller", "Your Gameobject Already Has Realistic Car Controller", "Close");
            Selection.activeGameObject = currentSelectedObject;

        }

        if (currentSelectedObject.GetComponentInParent<RCC_CarControllerV3>())
            currentSelectedVehicle = currentSelectedObject.GetComponentInParent<RCC_CarControllerV3>();

        if (RCC_Settings.Instance.setLayers && !EditorApplication.isPlaying)
            SetLayers();

        if (currentSelectedVehicle.autoGenerateEngineRPMCurve)
            currentSelectedVehicle.ReCreateEngineTorqueCurve();

    }

    private void SetLayers() {

        if (string.IsNullOrEmpty(RCC_Settings.Instance.RCCLayer)) {

            Debug.LogError("RCC Layer is missing in RCC Settings. Go to Tools --> BoneCracker Games --> RCC --> Edit Settings, and set the layer of RCC.");
            return;

        }

        Transform[] allTransforms = currentSelectedVehicle.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in allTransforms) {

            int layerInt = LayerMask.NameToLayer(RCC_Settings.Instance.RCCLayer);

            if (layerInt >= 0 && layerInt <= 31) {

                if (!t.GetComponent<RCC_Light>()) {

                    t.gameObject.layer = LayerMask.NameToLayer(RCC_Settings.Instance.RCCLayer);

                    if (t.GetComponent<RCC_WheelCollider>())
                        t.gameObject.layer = LayerMask.NameToLayer(RCC_Settings.Instance.WheelColliderLayer);

                    if (t.GetComponent<RCC_DetachablePart>())
                        t.gameObject.layer = LayerMask.NameToLayer(RCC_Settings.Instance.DetachablePartLayer);

                }

            } else {

                Debug.LogError("RCC layers selected in RCC Settings doesn't exist on your Tags & Layers. Go to Edit --> Project Settings --> Tags & Layers, and create a new layer named ''" + RCC_Settings.Instance.RCCLayer + " " + RCC_Settings.Instance.WheelColliderLayer + " " + RCC_Settings.Instance.DetachablePartLayer + "''.");
                Debug.LogError("From now on, ''Setting Layers'' disabled in RCC Settings! You can enable this when you created this layer.");

                foreach (Transform tr in allTransforms)
                    tr.gameObject.layer = LayerMask.NameToLayer("Default");

                RCC_Settings.Instance.setLayers = false;
                return;

            }

        }

    }

}
