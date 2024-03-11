//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AI Controller of RCC. It's not professional, but it does the job. Follows all waypoints, or follows/chases the target gameobject.
/// </summary>
[RequireComponent(typeof(RCC_CarControllerV3))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/AI/RCC AI Car Controller")]
public class RCC_AICarController : MonoBehaviour {

    // Car controller.
    public RCC_CarControllerV3 CarController {
        get {
            if (_carController == null)
                _carController = GetComponentInParent<RCC_CarControllerV3>();
            return _carController;
        }
    }
    private RCC_CarControllerV3 _carController;

    public RCC_AIWaypointsContainer waypointsContainer;                 // Waypoints Container.
    public int currentWaypointIndex = 0;                                            // Current index in Waypoint Container.
    public string targetTag = "Player";                                 // Search and chase Gameobjects with tags.

    // AI Type
    public NavigationMode navigationMode = NavigationMode.FollowWaypoints;
    public enum NavigationMode { FollowWaypoints, ChaseTarget, FollowTarget }

    // Raycast distances used for detecting obstacles at front of the AI vehicle.
    [Range(5f, 30f)] public float raycastLength = 3f;
    [Range(10f, 90f)] public float raycastAngle = 30f;
    public LayerMask obstacleLayers = -1;
    public GameObject obstacle;

    public bool useRaycasts = true;     //	Using forward and sideways raycasts to avoid obstacles.
    private float rayInput = 0f;                // Total ray input affected by raycast distances.
    private bool raycasting = false;        // Raycasts hits an obstacle now?
    private float resetTime = 0f;           // This timer was used for deciding go back or not, after crashing.
    private bool reversingNow = false;      //  Reversing now?

    // Steer, Motor, And Brake inputs. Will feed RCC_CarController with these inputs.
    public float steerInput = 0f;
    public float throttleInput = 0f;
    public float brakeInput = 0f;
    public float handbrakeInput = 0f;

    // Limit speed.
    public bool limitSpeed = false;
    public float maximumSpeed = 100f;

    // Smoothed steering.
    public bool smoothedSteer = true;

    // Counts laps and how many waypoints were passed.
    public int lap = 0;
    public bool stopAfterLap = false;
    public int stopLap = 10;
    public int totalWaypointPassed = 0;
    public bool ignoreWaypointNow = false;

    // Detector radius.
    public int detectorRadius = 200;
    public int startFollowDistance = 300;
    public int stopFollowDistance = 30;
    private bool updateTargets = false;
    private float lastUpdatedTargets = 0f;

    // Unity's Navigator.
    private NavMeshAgent navigator;

    // Detector with Sphere Collider. Used for finding target Gameobjects in chasing mode.
    public List<Transform> targetsInZone = new List<Transform>();
    public List<RCC_AIBrakeZone> brakeZones = new List<RCC_AIBrakeZone>();

    public Transform targetChase;       // Target Gameobject for chasing.
    public RCC_AIBrakeZone targetBrake;     //  Target brakezone.

    // Firing an event when each RCC AI vehicle spawned / enabled.
    public delegate void onRCCAISpawned(RCC_AICarController RCCAI);
    public static event onRCCAISpawned OnRCCAISpawned;

    // Firing an event when each RCC AI vehicle disabled / destroyed.
    public delegate void onRCCAIDestroyed(RCC_AICarController RCCAI);
    public static event onRCCAIDestroyed OnRCCAIDestroyed;

    private void Awake() {

        // If Waypoints Container is not selected in Inspector Panel, find it on scene.
        if (!waypointsContainer)
            waypointsContainer = FindObjectOfType(typeof(RCC_AIWaypointsContainer)) as RCC_AIWaypointsContainer;

        // Creating our Navigator and setting properties.
        GameObject navigatorObject = new GameObject("Navigator");
        navigatorObject.transform.SetParent(transform, false);
        navigator = navigatorObject.AddComponent<NavMeshAgent>();
        navigator.radius = 1;
        navigator.speed = 1;
        navigator.angularSpeed = 100000f;
        navigator.acceleration = 100000f;
        navigator.height = 1;
        navigator.avoidancePriority = 0;

    }

    private void OnEnable() {

        //  Setting external controller on enable.
        CarController.externalController = true;

        // Calling this event when AI vehicle spawned.
        if (OnRCCAISpawned != null)
            OnRCCAISpawned(this);

    }

    private void Update() {

        // If not controllable, no need to go further.
        if (!CarController.canControl)
            return;

        //  If limit speed is not enabled, maximum speed is same with vehicle's maximum speed.
        if (!limitSpeed)
            maximumSpeed = CarController.maxspeed;

        // Assigning navigator's position to front wheels of the vehicle
        navigator.transform.localPosition = Vector3.zero;
        navigator.transform.localPosition += Vector3.forward * CarController.FrontLeftWheelCollider.transform.localPosition.z;

        CheckTargets();     //  Checking targets if navigation mode is set to chase or follow target mode.
        CheckBrakeZones();      //  Checking existing brake zones in the scene.

        if (!updateTargets)
            lastUpdatedTargets += Time.deltaTime;

        if (lastUpdatedTargets >= 1f)
            updateTargets = true;

    }

    private void FixedUpdate() {

        // If not controllable, no need to go further.
        if (!CarController.canControl)
            return;

        //  If enabled, raycasts will be used to avoid obstacles at runtime.
        if (useRaycasts)
            FixedRaycasts();        // Recalculates steerInput if one of raycasts detects an object front of AI vehicle.

        Navigation();       // Calculates steerInput based on navigator.
        CheckReset();       // Used for deciding go back or not after crashing.
        FeedRCC();      // Feeds inputs of the RCC.

    }

    private void Navigation() {

        // Navigator Input is multiplied by 1.5f for fast reactions.
        float navigatorInput = Mathf.Clamp(transform.InverseTransformDirection(navigator.desiredVelocity).x * 1f, -1f, 1f);

        if (navigatorInput > .4f)
            navigatorInput = 1f;

        if (navigatorInput < -.4f)
            navigatorInput = -1f;

        //  Navigation has three modes.
        switch (navigationMode) {

            case NavigationMode.FollowWaypoints:

                // If our scene doesn't have a Waypoint Container, stop and return with error.
                if (!waypointsContainer) {

                    Debug.LogError("Waypoints Container Couldn't Found!");
                    Stop();
                    return;

                }

                // If our scene has Waypoints Container and it doesn't have any waypoints, stop and return with error.
                if (waypointsContainer && waypointsContainer.waypoints.Count < 1) {

                    Debug.LogError("Waypoints Container Doesn't Have Any Waypoints!");
                    Stop();
                    return;

                }

                //	If stop after lap is enabled, stop at target lap.
                if (stopAfterLap && lap >= stopLap) {

                    Stop();
                    return;

                }

                // Next waypoint and its position.
                RCC_Waypoint currentWaypoint = waypointsContainer.waypoints[currentWaypointIndex];

                // Checks for the distance to next waypoint. If it is less than written value, then pass to next waypoint.
                float distanceToNextWaypoint = Vector3.Distance(transform.position, currentWaypoint.transform.position);

                // Setting destination of the Navigator.
                if (navigator.isOnNavMesh)
                    navigator.SetDestination(waypointsContainer.waypoints[currentWaypointIndex].transform.position);

                //  If distance to the next waypoint is not 0, and close enough to the vehicle, increase index of the current waypoint and total waypoint.
                if (distanceToNextWaypoint != 0 && distanceToNextWaypoint < waypointsContainer.waypoints[currentWaypointIndex].radius) {

                    currentWaypointIndex++;
                    totalWaypointPassed++;

                    // If all waypoints were passed, sets the current waypoint to first waypoint and increase lap.
                    if (currentWaypointIndex >= waypointsContainer.waypoints.Count) {

                        currentWaypointIndex = 0;
                        lap++;

                    }

                    // Setting destination of the Navigator. 
                    if (navigator.isOnNavMesh)
                        navigator.SetDestination(waypointsContainer.waypoints[currentWaypointIndex].transform.position);

                }

                //  If vehicle goes forward, calculate throttle and brake inputs.
                if (!reversingNow) {

                    throttleInput = (distanceToNextWaypoint < (waypointsContainer.waypoints[currentWaypointIndex].radius * (CarController.speed / 30f))) ? (Mathf.Clamp01(currentWaypoint.targetSpeed - CarController.speed)) : 1f;
                    throttleInput *= Mathf.Clamp01(Mathf.Lerp(10f, 0f, (CarController.speed) / maximumSpeed));
                    brakeInput = (distanceToNextWaypoint < (waypointsContainer.waypoints[currentWaypointIndex].radius * (CarController.speed / 30f))) ? (Mathf.Clamp01(CarController.speed - currentWaypoint.targetSpeed)) : 0f;
                    handbrakeInput = 0f;

                    //  If vehicle speed is high enough, calculate them related to navigator input. This will reduce throttle input, and increase brake input on sharp turns.
                    if (CarController.speed > 30f) {

                        throttleInput -= Mathf.Abs(navigatorInput) / 3f;
                        brakeInput += Mathf.Abs(navigatorInput) / 3f;

                    }

                }

                break;

            case NavigationMode.ChaseTarget:

                // If our scene doesn't have a target to chase, stop and return.
                if (!targetChase) {

                    Stop();
                    return;

                }

                // Setting destination of the Navigator. 
                if (navigator.isOnNavMesh)
                    navigator.SetDestination(targetChase.position);

                //  If vehicle goes forward, calculate throttle and brake inputs.
                if (!reversingNow) {

                    throttleInput = 1f;
                    throttleInput *= Mathf.Clamp01(Mathf.Lerp(10f, 0f, (CarController.speed) / maximumSpeed));
                    brakeInput = 0f;
                    handbrakeInput = 0f;

                    //  If vehicle speed is high enough, calculate them related to navigator input. This will reduce throttle input, and increase brake input on sharp turns.
                    if (CarController.speed > 30f) {

                        throttleInput -= Mathf.Abs(navigatorInput) / 3f;
                        brakeInput += Mathf.Abs(navigatorInput) / 3f;

                    }

                }

                break;

            case NavigationMode.FollowTarget:

                // If our scene doesn't have a Waypoints Container, return with error.
                if (!targetChase) {

                    Stop();
                    return;

                }

                // Setting destination of the Navigator. 
                if (navigator.isOnNavMesh)
                    navigator.SetDestination(targetChase.position);

                // Checks for the distance to target. 
                float distanceToTarget = Vector3.Distance(transform.position, targetChase.position);

                //  If vehicle goes forward, calculate throttle and brake inputs.
                if (!reversingNow) {

                    throttleInput = distanceToTarget < (stopFollowDistance * Mathf.Lerp(1f, 5f, CarController.speed / 50f)) ? Mathf.Lerp(-5f, 1f, distanceToTarget / (stopFollowDistance / 1f)) : 1f;
                    throttleInput *= Mathf.Clamp01(Mathf.Lerp(10f, 0f, (CarController.speed) / maximumSpeed));
                    brakeInput = distanceToTarget < (stopFollowDistance * Mathf.Lerp(1f, 5f, CarController.speed / 50f)) ? Mathf.Lerp(5f, 0f, distanceToTarget / (stopFollowDistance / 1f)) : 0f;
                    handbrakeInput = 0f;

                    //  If vehicle speed is high enough, calculate them related to navigator input. This will reduce throttle input, and increase brake input on sharp turns.
                    if (CarController.speed > 30f) {

                        throttleInput -= Mathf.Abs(navigatorInput) / 3f;
                        brakeInput += Mathf.Abs(navigatorInput) / 3f;

                    }

                    if (throttleInput < .05f)
                        throttleInput = 0f;
                    if (brakeInput < .05f)
                        brakeInput = 0f;

                }

                break;

        }

        //  If vehicle is in brake zone, apply brake input.
        if (targetBrake) {

            //  If vehicle is in brake zone and speed of the vehicle is higher than the target speed, apply brake input.
            if (Vector3.Distance(transform.position, targetBrake.transform.position) < targetBrake.distance && CarController.speed > targetBrake.targetSpeed) {

                throttleInput = 0f;
                brakeInput = 1f;

            }

        }

        if (brakeInput > .25f)
            throttleInput = 0f;

        // Steer input.
        steerInput = (ignoreWaypointNow ? rayInput : navigatorInput + rayInput);
        steerInput = Mathf.Clamp(steerInput, -1f, 1f) * CarController.direction;

        //  Clamping inputs.
        throttleInput = Mathf.Clamp01(throttleInput);
        brakeInput = Mathf.Clamp01(brakeInput);
        handbrakeInput = Mathf.Clamp01(handbrakeInput);

        //  If vehicle goes backwards, set brake input to 1 for reversing.
        if (reversingNow) {

            throttleInput = 0f;
            brakeInput = 1f;
            handbrakeInput = 0f;

        } else {

            if (CarController.speed < 5f && brakeInput >= .5f) {

                brakeInput = 0f;
                handbrakeInput = 1f;

            }

        }

    }

    /// <summary>
    /// Vehicle will try to go backwards if crashed or stucked.
    /// </summary>
    private void CheckReset() {

        //  If navigation mode is set to follow, this means vehicle may stop. If vehicle is stopped near the target, no need to go backwards.
        if (targetChase && navigationMode == NavigationMode.FollowTarget && Vector3.Distance(transform.position, targetChase.position) < stopFollowDistance) {

            reversingNow = false;
            resetTime = 0;
            return;

        }

        // If unable to move forward, puts the gear to R.
        if (CarController.speed <= 5 && transform.InverseTransformDirection(CarController.Rigid.velocity).z <= 1f)
            resetTime += Time.deltaTime;

        //  If car is stucked for 2 seconds, reverse now.
        if (resetTime >= 2)
            reversingNow = true;

        //  If car is stucked for 4 seconds, or speed exceeds 25, go forward.
        if (resetTime >= 4 || CarController.speed >= 25) {

            reversingNow = false;
            resetTime = 0;

        }

    }

    /// <summary>
    /// Using raycasts to avoid obstacles.
    /// </summary>
    private void FixedRaycasts() {

        //  Creating five raycasts with angles.
        int[] anglesOfRaycasts = new int[5];
        anglesOfRaycasts[0] = 0;
        anglesOfRaycasts[1] = Mathf.FloorToInt(raycastAngle / 3f);
        anglesOfRaycasts[2] = Mathf.FloorToInt(raycastAngle / 1f);
        anglesOfRaycasts[3] = -Mathf.FloorToInt(raycastAngle / 1f);
        anglesOfRaycasts[4] = -Mathf.FloorToInt(raycastAngle / 3f);

        // Ray pivot position.
        Vector3 pivotPos = transform.position;
        pivotPos += transform.forward * CarController.FrontLeftWheelCollider.transform.localPosition.z;

        //  Ray hit.
        RaycastHit hit;
        rayInput = 0f;
        bool casted = false;

        //  Casting rays.
        for (int i = 0; i < anglesOfRaycasts.Length; i++) {

            //  Drawing normal gizmos.
            Debug.DrawRay(pivotPos, Quaternion.AngleAxis(anglesOfRaycasts[i], transform.up) * transform.forward * raycastLength, Color.white);

            //  Casting the ray. If ray hits another obstacle...
            if (Physics.Raycast(pivotPos, Quaternion.AngleAxis(anglesOfRaycasts[i], transform.up) * transform.forward, out hit, raycastLength, obstacleLayers) && !hit.collider.isTrigger && hit.transform.root != transform) {

                switch (navigationMode) {

                    case NavigationMode.FollowWaypoints:

                        //  Drawing hit gizmos.
                        Debug.DrawRay(pivotPos, Quaternion.AngleAxis(anglesOfRaycasts[i], transform.up) * transform.forward * raycastLength, Color.red);
                        casted = true;

                        //  Setting ray input related to distance to the obstacle.
                        if (i != 0)
                            rayInput -= Mathf.Lerp(Mathf.Sign(anglesOfRaycasts[i]), 0f, (hit.distance / raycastLength));

                        break;

                    case NavigationMode.ChaseTarget:

                        if (targetChase && hit.transform != targetChase && !hit.transform.IsChildOf(targetChase)) {

                            //  Drawing hit gizmos.
                            Debug.DrawRay(pivotPos, Quaternion.AngleAxis(anglesOfRaycasts[i], transform.up) * transform.forward * raycastLength, Color.red);
                            casted = true;

                            //  Setting ray input related to distance to the obstacle.
                            if (i != 0)
                                rayInput -= Mathf.Lerp(Mathf.Sign(anglesOfRaycasts[i]), 0f, (hit.distance / raycastLength));

                        }

                        break;

                    case NavigationMode.FollowTarget:

                        //  Drawing hit gizmos.
                        Debug.DrawRay(pivotPos, Quaternion.AngleAxis(anglesOfRaycasts[i], transform.up) * transform.forward * raycastLength, Color.red);
                        casted = true;

                        //  Setting ray input related to distance to the obstacle.
                        if (i != 0)
                            rayInput -= Mathf.Lerp(Mathf.Sign(anglesOfRaycasts[i]), 0f, (hit.distance / raycastLength));

                        break;

                }

                //  If ray hits an obstacle, set obstacle. Otherwise set it to null.
                if (casted)
                    obstacle = hit.transform.gameObject;
                else
                    obstacle = null;

            }

        }

        //  Ray hits an obstacle or not?
        raycasting = casted;

        //  If so, clamp the ray input.
        rayInput = Mathf.Clamp(rayInput, -1f, 1f);

        //  If ray input is high enough, ignore the navigator input and directly use the ray input for steering.
        if (raycasting && Mathf.Abs(rayInput) > .5f)
            ignoreWaypointNow = true;
        else
            ignoreWaypointNow = false;

    }

    /// <summary>
    /// Feeding the RCC with throttle, brake, steer, and handbrake inputs.
    /// </summary>
    private void FeedRCC() {

        // Feeding throttleInput of the RCC.
        if (!CarController.changingGear && !CarController.cutGas)
            CarController.throttleInput = (CarController.direction == 1 ? Mathf.Clamp01(throttleInput) : Mathf.Clamp01(brakeInput));
        else
            CarController.throttleInput = 0f;

        if (!CarController.changingGear && !CarController.cutGas)
            CarController.brakeInput = (CarController.direction == 1 ? Mathf.Clamp01(brakeInput) : Mathf.Clamp01(throttleInput));
        else
            CarController.brakeInput = 0f;

        // Feeding steerInput of the RCC.
        if (smoothedSteer)
            CarController.steerInput = Mathf.Lerp(CarController.steerInput, steerInput, Time.deltaTime * 20f);
        else
            CarController.steerInput = steerInput;

        CarController.handbrakeInput = handbrakeInput;

    }

    /// <summary>
    /// Stops the vehicle immediately.
    /// </summary>
    private void Stop() {

        throttleInput = 0f;
        brakeInput = 0f;
        steerInput = 0f;
        handbrakeInput = 1f;

    }

    /// <summary>
    /// Checks the near targets if navigation mode is set to follow or chase mode.
    /// </summary>
    private void CheckTargets() {

        if (!updateTargets)
            return;

        updateTargets = false;
        lastUpdatedTargets = 0f;

        Collider[] colliders = Physics.OverlapSphere(transform.position, detectorRadius);

        for (int i = 0; i < colliders.Length; i++) {

            //  If a target in the zone, add it to the list.
            if (colliders[i].transform.root.CompareTag(targetTag)) {

                if (!targetsInZone.Contains(colliders[i].transform.root))
                    targetsInZone.Add(colliders[i].transform.root);

            }

            //  If a brake zone in the zone, add it to the list.
            if (colliders[i].GetComponent<RCC_AIBrakeZone>()) {

                if (!brakeZones.Contains(colliders[i].GetComponent<RCC_AIBrakeZone>()))
                    brakeZones.Add(colliders[i].GetComponent<RCC_AIBrakeZone>());

            }

        }

        // Removing unnecessary targets in list first. If target is null or not active, remove it from the list.
        for (int i = 0; i < targetsInZone.Count; i++) {

            if (targetsInZone[i] == null)
                targetsInZone.RemoveAt(i);

            if (!targetsInZone[i].gameObject.activeInHierarchy)
                targetsInZone.RemoveAt(i);

            else {

                //  If distance to the target is far away, remove it from the list.
                if (Vector3.Distance(transform.position, targetsInZone[i].transform.position) > (detectorRadius * 1.1f))
                    targetsInZone.RemoveAt(i);

            }

        }

        // If there is a target in the zone, get closest enemy.
        if (targetsInZone.Count > 0)
            targetChase = GetClosestEnemy(targetsInZone.ToArray());
        else
            targetChase = null;

    }

    /// <summary>
    /// Checks the brake zones.
    /// </summary>
    private void CheckBrakeZones() {

        // Removing unnecessary brake zones in list. If brake zone is null or not active, remove it from the list.
        for (int i = 0; i < brakeZones.Count; i++) {

            if (brakeZones[i] == null)
                brakeZones.RemoveAt(i);

            if (!brakeZones[i].gameObject.activeInHierarchy)
                brakeZones.RemoveAt(i);

            else {

                //  If distance to the brake zone is far away, remove it from the list.
                if (Vector3.Distance(transform.position, brakeZones[i].transform.position) > (detectorRadius * 1.1f))
                    brakeZones.RemoveAt(i);

            }

        }

        // If there is a brake zone, get closest one.
        if (brakeZones.Count > 0)
            targetBrake = GetClosestBrakeZone(brakeZones.ToArray());
        else
            targetBrake = null;

    }

    /// <summary>
    /// Gets the closest enemy.
    /// </summary>
    /// <param name="enemies"></param>
    /// <returns></returns>
    private Transform GetClosestEnemy(Transform[] enemies) {

        Transform bestTarget = null;

        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (Transform potentialTarget in enemies) {

            Vector3 directionToTarget = potentialTarget.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;

            if (dSqrToTarget < closestDistanceSqr) {

                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget;

            }

        }

        return bestTarget;

    }

    /// <summary>
    /// Gets the closest brake zone.
    /// </summary>
    /// <param name="enemies"></param>
    /// <returns></returns>
    private RCC_AIBrakeZone GetClosestBrakeZone(RCC_AIBrakeZone[] enemies) {

        RCC_AIBrakeZone bestTarget = null;

        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (RCC_AIBrakeZone potentialTarget in enemies) {

            Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;

            if (dSqrToTarget < closestDistanceSqr) {

                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget;

            }

        }

        return bestTarget;

    }

    private void OnDisable() {

        //  Disabling external controller of the vehicle on disable.
        CarController.externalController = false;

        // Calling this event when AI vehicle is destroyed.
        if (OnRCCAIDestroyed != null)
            OnRCCAIDestroyed(this);

    }

}