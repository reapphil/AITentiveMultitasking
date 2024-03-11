//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ground materials for variable ground physics.
/// </summary>
[System.Serializable]
public class RCC_GroundMaterials : ScriptableObject {

    #region singleton
    private static RCC_GroundMaterials instance;
    public static RCC_GroundMaterials Instance { get { if (instance == null) instance = Resources.Load("RCC Assets/RCC_GroundMaterials") as RCC_GroundMaterials; return instance; } }
    #endregion

    [System.Serializable]
    public class GroundMaterialFrictions {

        public PhysicMaterial groundMaterial;       //  Physic material.
        public float forwardStiffness = 1f;     //  Forward stiffness.
        public float sidewaysStiffness = 1f;        //  Sideways stiffness.
        public float slip = .25f;       //  Target slip limit.
        public float damp = 1f;     //  Damp force.
        [Range(0f, 1f)] public float volume = 1f;       //  Volume of the ground sound.
        public GameObject groundParticles;      //  Ground particles.
        public AudioClip groundSound;       //  Ground audio clip.
        public RCC_Skidmarks skidmark;      //  Skidmarks.
        public bool deflate = false;        //  Deflate the wheel?

    }

    public GroundMaterialFrictions[] frictions;     //  Ground materials.

    /// <summary>
    /// Terrain ground materials.
    /// </summary>
    [System.Serializable]
    public class TerrainFrictions {

        public PhysicMaterial groundMaterial;

        [System.Serializable]
        public class SplatmapIndexes {

            public int index = 0;

        }

        public SplatmapIndexes[] splatmapIndexes;

    }

    public TerrainFrictions[] terrainFrictions;     //  Terrain ground materials.

}


