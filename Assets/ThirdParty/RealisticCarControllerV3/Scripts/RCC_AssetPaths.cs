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
/// Paths of the projects, links, and assets.
/// </summary>
public class RCC_AssetPaths {

    public const string BCGSharedAssetsPath = "Assets/RealisticCarControllerV3/Addon Packages/For BCG Shared Assets (Enter-Exit)/BCG Shared Assets.unitypackage";
    public const string PUN2AssetsPath = "Assets/RealisticCarControllerV3/Addon Packages/For Photon PUN 2/Photon PUN 2 Scripts and Photon Vehicles.unitypackage";
    public const string ProFlareAssetsPath = "Assets/RealisticCarControllerV3/Addon Packages/For ProFlares/Pro Flares Integration.unitypackage";
    public const string LogiAssetsPath = "Assets/RealisticCarControllerV3/Addon Packages/For Logitech Steering Wheel/Logitech Integration.unitypackage";
    public const string projectSettingsPath = "Assets/RealisticCarControllerV3/Addon Packages/Project Settings/RCC_ProjectSettings.unitypackage";
    public const string editorPreferences = "Assets/RealisticCarControllerV3/Editor/RCC_Preferences.asset";
    public const string inputsPath = "RCC_InputActions";

    public const string assetStorePath = "https://assetstore.unity.com/packages/tools/physics/realistic-car-controller-16296#content";
    public const string photonPUN2 = "https://assetstore.unity.com/packages/tools/network/pun-2-free-119922";
    public const string logitech = "https://assetstore.unity.com/packages/tools/integration/logitech-gaming-sdk-6630";
    public const string proFlares = "https://assetstore.unity.com/packages/tools/particles-effects/proflares-ultimate-lens-flares-for-unity3d-12845";

    public const string documentations = "https://www.bonecrackergames.com/bonecrackergames.com/admin/AssetStoreDocs/RCC_Documentations.rar";
    public const string YTVideos = "https://www.youtube.com/playlist?list=PLRXTqAVrLDpoW58lKf8XA1AWD6kDkoKb1";
    public const string otherAssets = "https://assetstore.unity.com/publishers/5425";

    public const string demo_AIO = "Assets/RealisticCarControllerV3/Demo Scenes/RCC City AIO.unity";
    public const string demo_City = "Assets/RealisticCarControllerV3/Demo Scenes/RCC City.unity";
    public const string demo_CarSelection = "Assets/RealisticCarControllerV3/Demo Scenes/RCC City (Car Selection).unity";
    public const string demo_CarSelectionLoadNextScene = "Assets/RealisticCarControllerV3/Demo Scenes/RCC City (Car Selection with Load Next Scene).unity";
    public const string demo_CarSelectionLoadedScene = "Assets/RealisticCarControllerV3/Demo Scenes/RCC City (Car Selection with Loaded Scene).unity";
    public const string demo_OverrideInputs = "Assets/RealisticCarControllerV3/Demo Scenes/RCC Blank Override Inputs Scene.unity";
    public const string demo_Customization = "Assets/RealisticCarControllerV3/Demo Scenes/RCC Blank Customization Scene.unity";

    public const string demo_APIBlank = "Assets/RealisticCarControllerV3/Demo Scenes/RCC Blank API Test Scene.unity";
    public const string demo_BlankMobile = "Assets/RealisticCarControllerV3/Demo Scenes/RCC Blank Test Scene.unity";
    public const string demo_Damage = "Assets/RealisticCarControllerV3/Demo Scenes/RCC Damage Test Scene.unity";
    public const string demo_MultipleTerrain = "Assets/RealisticCarControllerV3/Demo Scenes/RCC Multiple Terrain Test Scene.unity";

    public const string demo_CityFPS = "Assets/RealisticCarControllerV3/Demo Scenes/RCC Enter Exit Scenes/RCC City (Enter-Exit FPS).unity";
    public const string demo_CityTPS = "Assets/RealisticCarControllerV3/Demo Scenes/RCC Enter Exit Scenes/RCC City (Enter-Exit TPS).unity";

    public const string demo_PUN2Lobby = "Assets/RealisticCarControllerV3/Demo Scenes/RCC Photon Scenes/RCC Lobby (Photon PUN2).unity";
    public const string demo_PUN2City = "Assets/RealisticCarControllerV3/Demo Scenes/RCC Photon Scenes/RCC City (Photon PUN2).unity";
    public const string demo_PUN2CityEnterExit = "Assets/RealisticCarControllerV3/Demo Scenes/RCC Photon Scenes/RCC City Enter Exit FPS (Photon PUN2).unity";

    public readonly static string[] demoAssetPaths = new string[]{

        "Assets/RealisticCarControllerV3/Models",
        "Assets/RealisticCarControllerV3/Demo Scenes",
        "Assets/RealisticCarControllerV3/Textures/City Textures",
        "Assets/RealisticCarControllerV3/Textures/Vehicle Textures",
        "Assets/RealisticCarControllerV3/Prefabs/Demo Vehicles",
        "Assets/RealisticCarControllerV3/Resources/Changable Wheels",
        "Assets/RealisticCarControllerV3/Resources/Photon Vehicles"

    };

}
