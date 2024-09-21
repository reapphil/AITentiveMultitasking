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
using UnityEngine.SceneManagement;

/// <summary>
/// Loads target scene.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Level Loader")]
public class RCC_LevelLoader : MonoBehaviour {

    /// <summary>
    /// Loads target scene with string.
    /// </summary>
    /// <param name="levelName"></param>
    public void LoadLevel(string levelName) {

        SceneManager.LoadScene(levelName);

    }

}
