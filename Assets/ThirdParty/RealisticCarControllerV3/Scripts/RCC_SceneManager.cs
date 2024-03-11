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
/// Scene manager that contains current player vehicle, current player camera, current player UI, current player character, recording/playing mechanim, and other vehicles as well.
/// 
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Main/RCC Scene Manager")]
public class RCC_SceneManager : RCC_Singleton<RCC_SceneManager> {

    public RCC_CarControllerV3 activePlayerVehicle;     //  Current active player vehicle.
    public RCC_Camera activePlayerCamera;       //  Current active player camera as RCC Camera.
    public RCC_UIDashboardDisplay activePlayerCanvas;       //  Current active UI canvas.
    public Camera activeMainCamera;     //  Current active main camera.
    private RCC_CarControllerV3 lastActivePlayerVehicle;        //  Last selected player vehicle.

    public bool registerLastSpawnedVehicleAsPlayerVehicle = true;        //  Registers the lastly spawned vehicle as player vehicle.
    public bool disableUIWhenNoPlayerVehicle = false;       //  Disables the UI when there is no any player vehicle.
    public bool loadCustomizationAtFirst = false;        //  Loads the latest customization for the spawned vehicle.

    public bool useRecord = false;      //  Use record / replay?

    public List<RCC_Recorder> allRecorders = new List<RCC_Recorder>();      //  All recorders attached to the vehicles.
    public enum RecordMode { Neutral, Play, Record }        //  Current record / replay state.
    public RecordMode recordMode = RecordMode.Neutral;

    // Default time scale of the game.
    private float orgTimeScale = 1f;

    public List<RCC_CarControllerV3> allVehicles = new List<RCC_CarControllerV3>();     //  All vehicles.

#if BCG_ENTEREXIT
    public BCG_EnterExitPlayer activePlayerCharacter;       //  Current active player character controller.
#endif

    public Terrain[] allTerrains;       //  All terrains.

    public class Terrains {

        //	Terrain data.
        public Terrain terrain;
        public TerrainData mTerrainData;
        public PhysicMaterial terrainCollider;
        public int alphamapWidth;
        public int alphamapHeight;

        public float[,,] mSplatmapData;
        public float mNumTextures;

    }

    public Terrains[] terrains;     //  All terrains with custom class.
    public bool terrainsInitialized = false;        //  All terrains are initialized yet?

    // Firing an event when main behavior changed.
    public delegate void onBehaviorChanged();
    public static event onBehaviorChanged OnBehaviorChanged;

    // Firing an event when player vehicle changed.
    public delegate void onVehicleChanged();
    public static event onVehicleChanged OnVehicleChanged;

    private void Awake() {

        // Overriding Fixed TimeStep.
        if (RCC_Settings.Instance.overrideFixedTimeStep)
            Time.fixedDeltaTime = RCC_Settings.Instance.fixedTimeStep;

        // Overriding FPS.
        if (RCC_Settings.Instance.overrideFPS)
            Application.targetFrameRate = RCC_Settings.Instance.maxFPS;

        //  Instantiate telemetry UI if it's enabled in RCC Settings.
        if (RCC_Settings.Instance.useTelemetry)
            Instantiate(RCC_Settings.Instance.RCCTelemetry, Vector3.zero, Quaternion.identity);

        //  Listening events.
        RCC_Camera.OnBCGCameraSpawned += RCC_Camera_OnBCGCameraSpawned;
        RCC_CarControllerV3.OnRCCPlayerSpawned += RCC_CarControllerV3_OnRCCSpawned;
        RCC_AICarController.OnRCCAISpawned += RCC_AICarController_OnRCCAISpawned;
        RCC_CarControllerV3.OnRCCPlayerDestroyed += RCC_CarControllerV3_OnRCCPlayerDestroyed;
        RCC_AICarController.OnRCCAIDestroyed += RCC_AICarController_OnRCCAIDestroyed;
        RCC_InputManager.OnSlowMotion += RCC_InputManager_OnSlowMotion;

#if BCG_ENTEREXIT
        BCG_EnterExitPlayer.OnBCGPlayerSpawned += BCG_EnterExitPlayer_OnBCGPlayerSpawned;
        BCG_EnterExitPlayer.OnBCGPlayerDestroyed += BCG_EnterExitPlayer_OnBCGPlayerDestroyed;
#endif

        //  Getting active UI canvas.
        activePlayerCanvas = FindObjectOfType<RCC_UIDashboardDisplay>();

        // Getting default time scale of the game.
        orgTimeScale = Time.timeScale;

        //  If lock cursor is enabled in RCC Settings, lock the cursor.
        if (RCC_Settings.Instance.lockAndUnlockCursor)
            Cursor.lockState = CursorLockMode.Locked;

    }

    #region ONSPAWNED

    /// <summary>
    /// When RCC vehicle is spawned.
    /// </summary>
    /// <param name="RCC"></param>
    private void RCC_CarControllerV3_OnRCCSpawned(RCC_CarControllerV3 RCC) {

        //  If all vehicles list doesn't contain spawned vehicle, add it to the list.
        if (!allVehicles.Contains(RCC)) {

            allVehicles.Add(RCC);

            if (useRecord) {

                //  Finding recorder if attached to the vehicle before. If not found, add it.
                allRecorders = new List<RCC_Recorder>();
                allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());

                RCC_Recorder recorder = null;

                if (allRecorders != null && allRecorders.Count > 0) {

                    for (int i = 0; i < allRecorders.Count; i++) {

                        if (allRecorders[i] != null && allRecorders[i].carController == RCC) {
                            recorder = allRecorders[i];
                            break;
                        }

                    }

                }

                if (recorder == null) {

                    recorder = gameObject.AddComponent<RCC_Recorder>();
                    recorder.carController = RCC;

                }

            }

        }

        //  Checking all recorders.
        StartCoroutine(CheckMissingRecorders());

        //  Registers the last spawned vehicle as player vehicle.
        if (registerLastSpawnedVehicleAsPlayerVehicle)
            RegisterPlayer(RCC);

#if BCG_ENTEREXIT

        //  If spawned vehicle has enter exit script, set corresponding camera of the script to RCC Camera.
        if (RCC.gameObject.GetComponent<BCG_EnterExitVehicle>())
            RCC.gameObject.GetComponent<BCG_EnterExitVehicle>().correspondingCamera = activePlayerCamera.gameObject;

#endif

    }

    /// <summary>
    /// When AI vehicle spawned.
    /// </summary>
    /// <param name="RCCAI"></param>
    private void RCC_AICarController_OnRCCAISpawned(RCC_AICarController RCCAI) {

        //  If all vehicles list doesn't contain spawned vehicle, add it to the list.
        if (!allVehicles.Contains(RCCAI.CarController)) {

            allVehicles.Add(RCCAI.CarController);

            if (useRecord) {

                //  Finding recorder if attached to the vehicle before. If not found, add it.
                allRecorders = new List<RCC_Recorder>();
                allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());

                RCC_Recorder recorder = null;

                if (allRecorders != null && allRecorders.Count > 0) {

                    for (int i = 0; i < allRecorders.Count; i++) {

                        if (allRecorders[i] != null && allRecorders[i].carController == RCCAI.CarController) {
                            recorder = allRecorders[i];
                            break;
                        }

                    }

                }

                if (recorder == null) {

                    recorder = gameObject.AddComponent<RCC_Recorder>();
                    recorder.carController = RCCAI.CarController;

                }

            }

        }

        if (useRecord) {

            //  Checking all recorders.
            StartCoroutine(CheckMissingRecorders());

        }

    }

    /// <summary>
    /// When RCC Camera spawned.
    /// </summary>
    /// <param name="BCGCamera"></param>
    private void RCC_Camera_OnBCGCameraSpawned(GameObject BCGCamera) {

        if (BCGCamera.GetComponent<RCC_Camera>())
            activePlayerCamera = BCGCamera.GetComponent<RCC_Camera>();

    }

#if BCG_ENTEREXIT
    /// <summary>
    /// When a character with enter exit script spawned.
    /// </summary>
    /// <param name="player"></param>
    private void BCG_EnterExitPlayer_OnBCGPlayerSpawned(BCG_EnterExitPlayer player) {

        activePlayerCharacter = player;

    }
#endif

    #endregion

    #region ONDESTROYED

    /// <summary>
    /// When a vehicle destroyed.
    /// </summary>
    /// <param name="RCC"></param>
    private void RCC_CarControllerV3_OnRCCPlayerDestroyed(RCC_CarControllerV3 RCC) {

        if (allVehicles.Contains(RCC))
            allVehicles.Remove(RCC);

        StartCoroutine(CheckMissingRecorders());

    }

    /// <summary>
    /// When a AI vehicle destroyed.
    /// </summary>
    /// <param name="RCCAI"></param>
    private void RCC_AICarController_OnRCCAIDestroyed(RCC_AICarController RCCAI) {

        if (allVehicles.Contains(RCCAI.CarController))
            allVehicles.Remove(RCCAI.CarController);

        StartCoroutine(CheckMissingRecorders());

    }

#if BCG_ENTEREXIT
    /// <summary>
    /// When a character with enter exit script destroyed.
    /// </summary>
    /// <param name="player"></param>
    private void BCG_EnterExitPlayer_OnBCGPlayerDestroyed(BCG_EnterExitPlayer player) {

    }
#endif

    #endregion

    private void Start() {

        //  Getting all terrains.
        StartCoroutine(GetAllTerrains());

    }

    /// <summary>
    /// Getting all terrains.
    /// </summary>
    /// <returns></returns>
    public IEnumerator GetAllTerrains() {

        yield return new WaitForFixedUpdate();
        allTerrains = Terrain.activeTerrains;
        yield return new WaitForFixedUpdate();

        //  If terrains found...
        if (allTerrains != null && allTerrains.Length >= 1) {

            terrains = new Terrains[allTerrains.Length];

            for (int i = 0; i < allTerrains.Length; i++) {

                if (allTerrains[i].terrainData == null) {

                    Debug.LogError("Terrain data of the " + allTerrains[i].transform.name + " is missing! Check the terrain data...");
                    yield return null;

                }

            }

            //  Initializing terrains.
            for (int i = 0; i < terrains.Length; i++) {

                terrains[i] = new Terrains();
                terrains[i].terrain = allTerrains[i];
                terrains[i].mTerrainData = allTerrains[i].terrainData;
                terrains[i].terrainCollider = allTerrains[i].GetComponent<TerrainCollider>().sharedMaterial;
                terrains[i].alphamapWidth = allTerrains[i].terrainData.alphamapWidth;
                terrains[i].alphamapHeight = allTerrains[i].terrainData.alphamapHeight;

                terrains[i].mSplatmapData = allTerrains[i].terrainData.GetAlphamaps(0, 0, terrains[i].alphamapWidth, terrains[i].alphamapHeight);
                terrains[i].mNumTextures = terrains[i].mSplatmapData.Length / (terrains[i].alphamapWidth * terrains[i].alphamapHeight);

            }

            terrainsInitialized = true;

        }

    }

    private void Update() {

        //  When player vehicle changed...
        if (activePlayerVehicle) {

            if (activePlayerVehicle != lastActivePlayerVehicle) {

                if (OnVehicleChanged != null)
                    OnVehicleChanged();

            }

            lastActivePlayerVehicle = activePlayerVehicle;

        }

        //  Checking UI canvas.
        if (disableUIWhenNoPlayerVehicle && activePlayerCanvas)
            CheckCanvas();

        //  Getting main camera.
        if (Camera.main != null)
            activeMainCamera = Camera.main;

        if (useRecord) {

            //  Getting all recorders and setting their states.
            if (allRecorders != null && allRecorders.Count > 0) {

                switch (allRecorders[0].mode) {

                    case RCC_Recorder.Mode.Neutral:

                        recordMode = RecordMode.Neutral;
                        break;

                    case RCC_Recorder.Mode.Play:

                        recordMode = RecordMode.Play;
                        break;

                    case RCC_Recorder.Mode.Record:

                        recordMode = RecordMode.Record;
                        break;

                }

            }

        }

    }

    /// <summary>
    /// Recording.
    /// </summary>
    public void Record() {

        if (!useRecord)
            return;

        if (allRecorders != null && allRecorders.Count > 0) {

            for (int i = 0; i < allRecorders.Count; i++)
                allRecorders[i].Record();

        }

    }

    /// <summary>
    /// Playing.
    /// </summary>
    public void Play() {

        if (!useRecord)
            return;

        if (allRecorders != null && allRecorders.Count > 0) {

            for (int i = 0; i < allRecorders.Count; i++)
                allRecorders[i].Play();

        }

    }

    /// <summary>
    /// Stop all records.
    /// </summary>
    public void Stop() {

        if (!useRecord)
            return;

        if (allRecorders != null && allRecorders.Count > 0) {

            for (int i = 0; i < allRecorders.Count; i++)
                allRecorders[i].Stop();

        }

    }

    /// <summary>
    /// Checking all recorders. If missing found, destroy it.
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckMissingRecorders() {

        if (!useRecord)
            yield break;

        yield return new WaitForFixedUpdate();

        allRecorders = new List<RCC_Recorder>();
        allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());

        if (allRecorders != null && allRecorders.Count > 0) {

            for (int i = 0; i < allRecorders.Count; i++) {

                if (allRecorders[i].carController == null)
                    Destroy(allRecorders[i]);

            }

        }

        yield return new WaitForFixedUpdate();

        allRecorders = new List<RCC_Recorder>();
        allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle.
    /// </summary>
    /// <param name="playerVehicle"></param>
    public void RegisterPlayer(RCC_CarControllerV3 playerVehicle) {

        activePlayerVehicle = playerVehicle;

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

        if (loadCustomizationAtFirst)
            RCC_Customization.LoadStats(RCC_SceneManager.Instance.activePlayerVehicle);

        if (FindObjectOfType<RCC_CustomizerExample>())
            FindObjectOfType<RCC_CustomizerExample>().CheckUIs();

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle. Also sets controllable state of the vehicle.
    /// </summary>
    /// <param name="playerVehicle"></param>
    /// <param name="isControllable"></param>
    public void RegisterPlayer(RCC_CarControllerV3 playerVehicle, bool isControllable) {

        activePlayerVehicle = playerVehicle;
        activePlayerVehicle.SetCanControl(isControllable);

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

        if (loadCustomizationAtFirst)
            RCC_Customization.LoadStats(RCC_SceneManager.Instance.activePlayerVehicle);

        if (FindObjectOfType<RCC_CustomizerExample>())
            FindObjectOfType<RCC_CustomizerExample>().CheckUIs();

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle. Also sets controllable state and engine state of the vehicle.
    /// </summary>
    /// <param name="playerVehicle"></param>
    /// <param name="isControllable"></param>
    /// <param name="engineState"></param>
    public void RegisterPlayer(RCC_CarControllerV3 playerVehicle, bool isControllable, bool engineState) {

        activePlayerVehicle = playerVehicle;
        activePlayerVehicle.SetCanControl(isControllable);
        activePlayerVehicle.SetEngine(engineState);

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

        if (loadCustomizationAtFirst)
            RCC_Customization.LoadStats(RCC_SceneManager.Instance.activePlayerVehicle);

        if (FindObjectOfType<RCC_CustomizerExample>())
            FindObjectOfType<RCC_CustomizerExample>().CheckUIs();

    }

    /// <summary>
    /// Deregisters the player vehicle.
    /// </summary>
    public void DeRegisterPlayer() {

        if (activePlayerVehicle)
            activePlayerVehicle.SetCanControl(false);

        activePlayerVehicle = null;

        if (activePlayerCamera)
            activePlayerCamera.RemoveTarget();

    }

    /// <summary>
    /// Checks UI canvas.
    /// </summary>
    public void CheckCanvas() {

        if (!activePlayerVehicle || !activePlayerVehicle.canControl || !activePlayerVehicle.gameObject.activeInHierarchy || !activePlayerVehicle.enabled) {

            activePlayerCanvas.SetDisplayType(RCC_UIDashboardDisplay.DisplayType.Off);

            return;

        }

        if (activePlayerCanvas.displayType != RCC_UIDashboardDisplay.DisplayType.Customization)
            activePlayerCanvas.displayType = RCC_UIDashboardDisplay.DisplayType.Full;

    }

    ///<summary>
    /// Sets new behavior.
    ///</summary>
    public void SetBehavior(int behaviorIndex) {

        RCC_Settings.Instance.overrideBehavior = true;
        RCC_Settings.Instance.behaviorSelectedIndex = behaviorIndex;

        if (OnBehaviorChanged != null)
            OnBehaviorChanged();

    }

    /// <summary>
    /// Changes current camera mode.
    /// </summary>
    public void ChangeCamera() {

        if (activePlayerCamera)
            activePlayerCamera.ChangeCamera();

    }

    /// <summary>
    /// Transport player vehicle the specified position and rotation.
    /// </summary>
    /// <param name="position">Position.</param>
    /// <param name="rotation">Rotation.</param>
    public void Transport(Vector3 position, Quaternion rotation) {

        if (activePlayerVehicle) {

            activePlayerVehicle.Rigid.velocity = Vector3.zero;
            activePlayerVehicle.Rigid.angularVelocity = Vector3.zero;

            activePlayerVehicle.transform.position = position;
            activePlayerVehicle.transform.rotation = rotation;

            activePlayerVehicle.throttleInput = 0f;
            activePlayerVehicle.brakeInput = 1f;
            activePlayerVehicle.engineRPM = activePlayerVehicle.minEngineRPM;
            activePlayerVehicle.currentGear = 0;

            for (int i = 0; i < activePlayerVehicle.AllWheelColliders.Length; i++)
                activePlayerVehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

            StartCoroutine(Freeze(activePlayerVehicle));

        }

    }

    /// <summary>
    /// Transport target vehicle the specified position and rotation.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    public void Transport(RCC_CarControllerV3 vehicle, Vector3 position, Quaternion rotation) {

        if (vehicle) {

            vehicle.Rigid.velocity = Vector3.zero;
            vehicle.Rigid.angularVelocity = Vector3.zero;

            vehicle.transform.position = position;
            vehicle.transform.rotation = rotation;

            vehicle.throttleInput = 0f;
            vehicle.brakeInput = 1f;
            vehicle.engineRPM = vehicle.minEngineRPM;
            vehicle.currentGear = 0;

            for (int i = 0; i < vehicle.AllWheelColliders.Length; i++)
                vehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

            //StartCoroutine(Freeze(vehicle));

        }

    }

    /// <summary>
    /// Freezing the target vehicle for 1 second.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    private IEnumerator Freeze(RCC_CarControllerV3 vehicle) {

        float timer = 1f;

        while (timer > 0) {

            timer -= Time.deltaTime;
            vehicle.canControl = false;
            vehicle.Rigid.velocity = new Vector3(0f, vehicle.Rigid.velocity.y, 0f);
            vehicle.Rigid.angularVelocity = Vector3.zero;
            yield return null;

        }

        vehicle.canControl = true;

    }

    /// <summary>
    /// Enters slow motion.
    /// </summary>
    /// <param name="state"></param>
    private void RCC_InputManager_OnSlowMotion(bool state) {

        if (state)
            Time.timeScale = .2f;
        else
            Time.timeScale = orgTimeScale;

    }

    private void OnDisable() {

        RCC_Camera.OnBCGCameraSpawned -= RCC_Camera_OnBCGCameraSpawned;
        RCC_CarControllerV3.OnRCCPlayerSpawned -= RCC_CarControllerV3_OnRCCSpawned;
        RCC_AICarController.OnRCCAISpawned -= RCC_AICarController_OnRCCAISpawned;
        RCC_CarControllerV3.OnRCCPlayerDestroyed -= RCC_CarControllerV3_OnRCCPlayerDestroyed;
        RCC_AICarController.OnRCCAIDestroyed -= RCC_AICarController_OnRCCAIDestroyed;
        RCC_InputManager.OnSlowMotion -= RCC_InputManager_OnSlowMotion;

#if BCG_ENTEREXIT
        BCG_EnterExitPlayer.OnBCGPlayerSpawned -= BCG_EnterExitPlayer_OnBCGPlayerSpawned;
        BCG_EnterExitPlayer.OnBCGPlayerDestroyed -= BCG_EnterExitPlayer_OnBCGPlayerDestroyed;
#endif

    }

}
