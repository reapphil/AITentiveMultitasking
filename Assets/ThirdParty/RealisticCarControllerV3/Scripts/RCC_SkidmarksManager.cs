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

/// <summary>
/// Skidmarks manager all all kind of skidmarks.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Skidmarks Manager")]
public class RCC_SkidmarksManager : RCC_Singleton<RCC_SkidmarksManager> {

    private RCC_Skidmarks[] skidmarks;      //  All skidmarks.
    private int[] skidmarksIndexes;     //  Index of the skidmarks.
    private int _lastGroundIndex = 0;       //  Last index of the ground.

    private void Awake() {

        //  Creating new skidmarks and initializing them with given ground materials in RCC Ground Materials.
        skidmarks = new RCC_Skidmarks[RCC_GroundMaterials.Instance.frictions.Length];
        skidmarksIndexes = new int[skidmarks.Length];

        for (int i = 0; i < skidmarks.Length; i++) {

            skidmarks[i] = Instantiate(RCC_GroundMaterials.Instance.frictions[i].skidmark, Vector3.zero, Quaternion.identity);
            skidmarks[i].transform.name = skidmarks[i].transform.name + "_" + RCC_GroundMaterials.Instance.frictions[i].groundMaterial.name;
            skidmarks[i].transform.SetParent(transform, true);

        }

    }

    // Function called by the wheels that is skidding. Gathers all the information needed to
    // create the mesh later. Sets the intensity of the skidmark section b setting the alpha
    // of the vertex color.
    public int AddSkidMark(Vector3 pos, Vector3 normal, float intensity, float width, int lastIndex, int groundIndex) {

        if (_lastGroundIndex != groundIndex) {

            _lastGroundIndex = groundIndex;
            return -1;

        }

        skidmarksIndexes[groundIndex] = skidmarks[groundIndex].AddSkidMark(pos, normal, intensity, width, lastIndex);

        return skidmarksIndexes[groundIndex];

    }

    /// <summary>
    /// Cleans all skidmarks.
    /// </summary>
    public void CleanSkidmarks() {

        for (int i = 0; i < skidmarks.Length; i++)
            skidmarks[i].Clean();

    }

    /// <summary>
    /// Cleans target skidmarks.
    /// </summary>
    /// <param name="index"></param>
    public void CleanSkidmarks(int index) {

        skidmarks[index].Clean();

    }

}
