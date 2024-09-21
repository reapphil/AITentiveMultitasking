//----------------------------------------------
//            Realistic Tank Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RCC_Installation {

    public static void Check() {

        bool layer_RCC = false;
        bool layer_RCC_WheelCollider = false;
        bool layer_RCC_DetachablePart = false;
        bool layer_RCC_Trailer = false;
        bool layer_RCC_Crasher = false;

        string[] missingLayers = new string[5];

        layer_RCC = LayerExists("RCC");
        layer_RCC_WheelCollider = LayerExists("RCC_WheelCollider");
        layer_RCC_DetachablePart = LayerExists("RCC_DetachablePart");
        layer_RCC_Trailer = LayerExists("RCC_Trailer");
        layer_RCC_Crasher = LayerExists("RCC_Crasher");

        if (!layer_RCC)
            missingLayers[0] = "RCC";

        if (!layer_RCC_WheelCollider)
            missingLayers[1] = "RCC_WheelCollider";

        if (!layer_RCC_DetachablePart)
            missingLayers[2] = "RCC_DetachablePart";

        if (!layer_RCC_Trailer)
            missingLayers[3] = "RCC_Trailer";

        if (!layer_RCC_Crasher)
            missingLayers[4] = "RCC_Crasher";

        if (!layer_RCC || !layer_RCC_Crasher || !layer_RCC_DetachablePart || !layer_RCC_Trailer || !layer_RCC_WheelCollider) {

            if (EditorUtility.DisplayDialog("Found Missing Layers For Realistic Car Controller", "These layers will be added to the Tags and Layers\n\n" + missingLayers[0] + "\n" + missingLayers[1] + "\n" + missingLayers[2] + "\n" + missingLayers[3] + "\n" + missingLayers[4], "Add")) {

                CheckLayer("RCC");
                CheckLayer("RCC_WheelCollider");
                CheckLayer("RCC_DetachablePart");
                CheckLayer("RCC_Trailer");
                CheckLayer("RCC_Crasher");

            }

        }

    }

    public static bool CheckTag(string tagName) {

        if (TagExists(tagName))
            return true;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        if (!PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName)) {

            int index = tagsProp.arraySize;

            tagsProp.InsertArrayElementAtIndex(index);
            SerializedProperty sp = tagsProp.GetArrayElementAtIndex(index);

            sp.stringValue = tagName;
            Debug.Log("Tag: " + tagName + " has been added.");

            tagManager.ApplyModifiedProperties();

            return true;

        }

        return false;

    }

    public static string NewTag(string name) {

        CheckTag(name);

        if (name == null || name == "")
            name = "Untagged";

        return name;

    }

    public static bool RemoveTag(string tagName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        if (PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName)) {

            SerializedProperty sp;

            for (int i = 0, j = tagsProp.arraySize; i < j; i++) {

                sp = tagsProp.GetArrayElementAtIndex(i);

                if (sp.stringValue == tagName) {

                    tagsProp.DeleteArrayElementAtIndex(i);
                    Debug.Log("Tag: " + tagName + " has been removed.");
                    tagManager.ApplyModifiedProperties();
                    return true;

                }

            }

        }

        return false;

    }

    public static bool TagExists(string tagName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        return PropertyExists(tagsProp, 0, 10000, tagName);

    }

    public static bool CheckLayer(string layerName) {

        if (LayerExists(layerName))
            return true;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        if (!PropertyExists(layersProp, 0, 31, layerName)) {

            SerializedProperty sp;

            for (int i = 8, j = 31; i < j; i++) {

                sp = layersProp.GetArrayElementAtIndex(i);

                if (sp.stringValue == "") {

                    sp.stringValue = layerName;
                    Debug.Log("Layer: " + layerName + " has been added.");
                    tagManager.ApplyModifiedProperties();
                    return true;

                }

                if (i == j)
                    Debug.Log("All allowed layers have been filled.");

            }

        }

        return false;

    }

    public static string NewLayer(string name) {

        if (name != null || name != "")
            CheckLayer(name);

        return name;

    }

    public static bool RemoveLayer(string layerName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        if (PropertyExists(layersProp, 0, layersProp.arraySize, layerName)) {

            SerializedProperty sp;

            for (int i = 0, j = layersProp.arraySize; i < j; i++) {

                sp = layersProp.GetArrayElementAtIndex(i);

                if (sp.stringValue == layerName) {

                    sp.stringValue = "";
                    Debug.Log("Layer: " + layerName + " has been removed.");
                    // Save settings
                    tagManager.ApplyModifiedProperties();
                    return true;

                }

            }

        }

        return false;

    }

    public static bool LayerExists(string layerName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        return PropertyExists(layersProp, 0, 31, layerName);

    }

    private static bool PropertyExists(SerializedProperty property, int start, int end, string value) {

        for (int i = start; i < end; i++) {

            SerializedProperty t = property.GetArrayElementAtIndex(i);

            if (t.stringValue.Equals(value))
                return true;

        }

        return false;

    }

}
#endif