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
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// RCC All In One playable demo scene manager.
/// </summary>
public class RCC_AIO : MonoBehaviour {

    // Instance of the script.
    private static RCC_AIO instance;

    public GameObject levels;       // Levels menu.
    public GameObject photonLevels;     //  Photon levels menu.
    public GameObject BCGLevels;        //  Enter exit levels menu.
    public GameObject back;     //Back button.

    private AsyncOperation async;       //Async.
    public Slider slider;       //	Loading slider.

    private void Start() {

        // Getting instance. If same exists, destroy it.
        if (instance) {

            Destroy(gameObject);
            return;

        } else {

            instance = this;
            DontDestroyOnLoad(gameObject);

        }

#if !RCC_PHOTON
        Toggle[] pbuttons = photonLevels.GetComponentsInChildren<Toggle>();

        foreach (var button in pbuttons)
            button.interactable = false;
#endif

#if !BCG_ENTEREXIT
        Toggle[] bbuttons = BCGLevels.GetComponentsInChildren<Toggle>();

        foreach (var button in bbuttons)
            button.interactable = false;
#endif

    }

    private void Update() {

        // If level load is in progress, enable and adjust loading slider. Otherwise, disable it.
        if (async != null && !async.isDone) {

            if (!slider.gameObject.activeSelf)
                slider.gameObject.SetActive(true);

            slider.value = async.progress;

        } else {

            if (slider.gameObject.activeSelf)
                slider.gameObject.SetActive(false);

        }

    }

    /// <summary>
    /// Loads the target level.
    /// </summary>
    /// <param name="levelName">Level name.</param>
    public void LoadLevel(string levelName) {

        async = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Single);

    }

    /// <summary>
    /// Toggles the UI menu.
    /// </summary>
    /// <param name="menu">Menu.</param>
    public void ToggleMenu(GameObject menu) {

        levels.SetActive(false);
        back.SetActive(false);

        menu.SetActive(true);

    }

    /// <summary>
    /// Closes application.
    /// </summary>
    public void Quit() {

        Application.Quit();

    }

}
