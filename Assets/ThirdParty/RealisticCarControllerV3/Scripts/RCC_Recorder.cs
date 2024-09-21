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
/// Record / Replay system. Saves player's input, vehicle rigid velocity, position, and rotation on record, and replays it when on playback.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Recorder")]
public class RCC_Recorder : MonoBehaviour {

    /// <summary>
    /// Recorded clip.
    /// </summary>
    [System.Serializable]
    public class RecordedClip {

        public string recordName = "New Record";        //  Record name.

        [HideInInspector] public PlayerInput[] inputs;      //  All inputs recorded frame by frame.
        [HideInInspector] public PlayerTransform[] transforms;      //  All position and rotation recorded frame by frame.
        [HideInInspector] public PlayerRigidBody[] rigids;      //  All velocities recorded frame by frame.

        public RecordedClip(PlayerInput[] _inputs, PlayerTransform[] _transforms, PlayerRigidBody[] _rigids, string _recordName) {

            inputs = _inputs;
            transforms = _transforms;
            rigids = _rigids;
            recordName = _recordName;

        }

    }

    public RecordedClip recorded;       //  Last recorded clip.

    public RCC_CarControllerV3 carController;       //  Car controller.

    public List<PlayerInput> Inputs;        //  Inputs.
    public List<PlayerTransform> Transforms;        //  Positions and rotations.
    public List<PlayerRigidBody> Rigidbodies;       //  Velocities.

    /// <summary>
    /// Inputs of the vehicle.
    /// </summary>
    [System.Serializable]
    public class PlayerInput {

        public float throttleInput = 0f;
        public float brakeInput = 0f;
        public float steerInput = 0f;
        public float handbrakeInput = 0f;
        public float clutchInput = 0f;
        public float boostInput = 0f;
        public float fuelInput = 0f;
        public int direction = 1;
        public bool canGoReverse = false;
        public int currentGear = 0;
        public bool changingGear = false;

        public RCC_CarControllerV3.IndicatorsOn indicatorsOn = RCC_CarControllerV3.IndicatorsOn.Off;
        public bool lowBeamHeadLightsOn = false;
        public bool highBeamHeadLightsOn = false;

        public PlayerInput(float _gasInput, float _brakeInput, float _steerInput, float _handbrakeInput, float _clutchInput, float _boostInput, float _fuelInput, int _direction, bool _canGoReverse, int _currentGear, bool _changingGear, RCC_CarControllerV3.IndicatorsOn _indicatorsOn, bool _lowBeamHeadLightsOn, bool _highBeamHeadLightsOn) {

            throttleInput = _gasInput;
            brakeInput = _brakeInput;
            steerInput = _steerInput;
            handbrakeInput = _handbrakeInput;
            clutchInput = _clutchInput;
            boostInput = _boostInput;
            fuelInput = _fuelInput;
            direction = _direction;
            canGoReverse = _canGoReverse;
            currentGear = _currentGear;
            changingGear = _changingGear;

            indicatorsOn = _indicatorsOn;
            lowBeamHeadLightsOn = _lowBeamHeadLightsOn;
            highBeamHeadLightsOn = _highBeamHeadLightsOn;

        }

    }

    /// <summary>
    /// Position and rotation of the vehicle.
    /// </summary>
    [System.Serializable]
    public class PlayerTransform {

        public Vector3 position;
        public Quaternion rotation;

        public PlayerTransform(Vector3 _pos, Quaternion _rot) {

            position = _pos;
            rotation = _rot;

        }

    }

    /// <summary>
    /// Linear and angular velocity of the vehicle.
    /// </summary>
    [System.Serializable]
    public class PlayerRigidBody {

        public Vector3 velocity;
        public Vector3 angularVelocity;

        public PlayerRigidBody(Vector3 _vel, Vector3 _angVel) {

            velocity = _vel;
            angularVelocity = _angVel;

        }

    }

    //  Current state.
    public enum Mode { Neutral, Play, Record }
    public Mode mode = Mode.Neutral;

    private void Awake() {

        //  Creating new lists for inputs, transforms, and rigids.
        Inputs = new List<PlayerInput>();
        Transforms = new List<PlayerTransform>();
        Rigidbodies = new List<PlayerRigidBody>();

    }

    private void OnEnable() {

        // Listening input events.
        RCC_InputManager.OnRecord += RCC_InputManager_OnRecord;
        RCC_InputManager.OnReplay += RCC_InputManager_OnReplay;

    }

    /// <summary>
    /// Replay.
    /// </summary>
    private void RCC_InputManager_OnReplay() {

        RCC_SceneManager.Instance.Play();

    }

    /// <summary>
    /// Record
    /// </summary>
    private void RCC_InputManager_OnRecord() {

        RCC_SceneManager.Instance.Record();

    }

    /// <summary>
    /// Record.
    /// </summary>
    public void Record() {

        //  If current state is not record, set it to record. Otherwise set it to neutral and save the clip.
        if (mode != Mode.Record) {

            mode = Mode.Record;

        } else {

            mode = Mode.Neutral;
            SaveRecord();

        }

        //  If mode is set to record before, clear all lists. That means we've saved the clip.
        if (mode == Mode.Record) {

            Inputs.Clear();
            Transforms.Clear();
            Rigidbodies.Clear();

        }

    }

    /// <summary>
    /// Save record clip.
    /// </summary>
    public void SaveRecord() {

        print("Record saved!");
        recorded = new RecordedClip(Inputs.ToArray(), Transforms.ToArray(), Rigidbodies.ToArray(), RCC_Records.Instance.records.Count.ToString() + "_" + carController.transform.name);
        RCC_Records.Instance.records.Add(recorded);

    }

    /// <summary>
    /// Play.
    /// </summary>
    public void Play() {

        //  If clip not found, return.
        if (recorded == null)
            return;

        //  If current state is not play, set it to play. Otherwise set it to neutral.
        if (mode != Mode.Play)
            mode = Mode.Play;
        else
            mode = Mode.Neutral;

        //  If current state is play, enable external controller of the car controller.
        if (mode == Mode.Play)
            carController.externalController = true;
        else
            carController.externalController = false;

        if (mode == Mode.Play) {

            StartCoroutine(Replay());

            if (recorded != null && recorded.transforms.Length > 0) {

                carController.transform.SetPositionAndRotation(recorded.transforms[0].position, recorded.transforms[0].rotation);

            }

            StartCoroutine(Revel());

        }

    }

    /// <summary>
    /// Play.
    /// </summary>
    /// <param name="_recorded"></param>
    public void Play(RecordedClip _recorded) {

        recorded = _recorded;

        print("Replaying record " + recorded.recordName);

        if (recorded == null)
            return;

        if (mode != Mode.Play)
            mode = Mode.Play;
        else
            mode = Mode.Neutral;

        if (mode == Mode.Play)
            carController.externalController = true;
        else
            carController.externalController = false;

        if (mode == Mode.Play) {

            StartCoroutine(Replay());

            if (recorded != null && recorded.transforms.Length > 0) {

                carController.transform.SetPositionAndRotation(recorded.transforms[0].position, recorded.transforms[0].rotation);

            }

            StartCoroutine(Revel());

        }

    }

    /// <summary>
    /// Stop.
    /// </summary>
    public void Stop() {

        mode = Mode.Neutral;
        carController.externalController = false;

    }

    /// <summary>
    /// Replay.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Replay() {

        for (int i = 0; i < recorded.inputs.Length && mode == Mode.Play; i++) {

            carController.externalController = true;
            carController.throttleInput = recorded.inputs[i].throttleInput;
            carController.brakeInput = recorded.inputs[i].brakeInput;
            carController.steerInput = recorded.inputs[i].steerInput;
            carController.handbrakeInput = recorded.inputs[i].handbrakeInput;
            carController.clutchInput = recorded.inputs[i].clutchInput;
            carController.boostInput = recorded.inputs[i].boostInput;
            carController.fuelInput = recorded.inputs[i].fuelInput;
            carController.direction = recorded.inputs[i].direction;
            carController.canGoReverseNow = recorded.inputs[i].canGoReverse;
            carController.currentGear = recorded.inputs[i].currentGear;
            carController.changingGear = recorded.inputs[i].changingGear;

            carController.indicatorsOn = recorded.inputs[i].indicatorsOn;
            carController.lowBeamHeadLightsOn = recorded.inputs[i].lowBeamHeadLightsOn;
            carController.highBeamHeadLightsOn = recorded.inputs[i].highBeamHeadLightsOn;

            yield return new WaitForFixedUpdate();

        }

        mode = Mode.Neutral;

        carController.externalController = false;

    }

    private IEnumerator Revel() {

        for (int i = 0; i < recorded.rigids.Length && mode == Mode.Play; i++) {

            carController.Rigid.velocity = recorded.rigids[i].velocity;
            carController.Rigid.angularVelocity = recorded.rigids[i].angularVelocity;

            yield return new WaitForFixedUpdate();

        }

        mode = Mode.Neutral;

        carController.externalController = false;

    }

    private void FixedUpdate() {

        if (!carController)
            return;

        switch (mode) {

            case Mode.Neutral:

                break;

            case Mode.Play:

                carController.externalController = true;

                break;

            case Mode.Record:

                Inputs.Add(new PlayerInput(carController.throttleInput, carController.brakeInput, carController.steerInput, carController.handbrakeInput, carController.clutchInput, carController.boostInput, carController.fuelInput, carController.direction, carController.canGoReverseNow, carController.currentGear, carController.changingGear, carController.indicatorsOn, carController.lowBeamHeadLightsOn, carController.highBeamHeadLightsOn));
                Transforms.Add(new PlayerTransform(carController.transform.position, carController.transform.rotation));
                Rigidbodies.Add(new PlayerRigidBody(carController.Rigid.velocity, carController.Rigid.angularVelocity));

                break;

        }

    }

    private void OnDisable() {

        // Listening input events.
        RCC_InputManager.OnRecord -= RCC_InputManager_OnRecord;
        RCC_InputManager.OnReplay -= RCC_InputManager_OnReplay;

    }

}
