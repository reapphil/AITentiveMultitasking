using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class SelfDrivingCar : MonoBehaviour {
    [Header("Current Inputs")]
    public float steerInput = 0f;
    public float throttleInput = 0f;
    public float brakeInput = 0f;
    public float handbrakeInput = 0f;

    [Header("Speed Limit")]
    public bool limitSpeed = true;
    public float maximumSpeed = 100f;

    [Header("Steering")]
    public bool smoothedSteer = false;

    [Header("Settings")]
    public Lane targetLane = Lane.Center;
    public LayerMask layerMask;

    [Header("Realistic Car Controller")]
    public RCC_CarControllerV3 carController;


    private RoadManager roadManager;
    private GameManager gameManager;

    private void Awake() {
        roadManager = RoadManager.Instance;
        gameManager = GameManager.Instance;
    }

    private void OnEnable() {
        carController.externalController = true;
    }
    
    void Update() {
        if (transform.position.y < -5) {
            GameManager.Instance.RemoveSelfDrivingCar(this);
            Destroy(gameObject);
        }

        if (!carController.canControl)
            return;

        if (!limitSpeed)
            maximumSpeed = carController.maxspeed;
    }

    private void FixedUpdate() {
        throttleInput = 1f;
        throttleInput *= Mathf.Clamp01(Mathf.Lerp(10f, 0f, (carController.speed) / maximumSpeed));
        brakeInput = 0f;
        handbrakeInput = 0f;
        
        // Steer input.
        steerInput = Steering(); 
        steerInput = Mathf.Clamp(steerInput, -1f, 1f) * carController.direction;
        
        CheckForVehicleInFront();

        //  Clamping inputs.
        throttleInput = Mathf.Clamp01(throttleInput);
        brakeInput = Mathf.Clamp01(brakeInput);
        handbrakeInput = Mathf.Clamp01(handbrakeInput);
        
        FeedRCC();
    }

    private void CheckForVehicleInFront() {


        // Raycast forward from the car's position
        Vector3 raycastStart = transform.position;
        raycastStart.y = 1f;
        
        Vector3 raycastStartLeft = raycastStart;
        Vector3 raycastStartRight = raycastStart;
        raycastStartLeft.x = -1.5f;
        raycastStartRight.x = 1.5f;
        
        List<Vector3> vectorList = new List<Vector3>
        {
            raycastStart,
            raycastStartLeft,
            raycastStartRight
        };

        foreach (Vector3 raystart in vectorList) {
            //  If vehicle is in front
            RaycastHit hit;
        
            if (Physics.Raycast(raystart, transform.forward, out hit, 50f, layerMask)) {
                if (hit.collider.CompareTag("PlayerCar") || hit.collider.CompareTag("AgentCar") || hit.collider.CompareTag("SelfDriving")) {
                    RCC_CarControllerV3 carControllerV3Target = hit.collider.GetComponentInParent<RCC_CarControllerV3>();
                    if (carController.speed > carControllerV3Target.speed) {
                        throttleInput = 0f;
                        brakeInput = 1f;

                        if (hit.distance < 20) {
                            Stop();
                        }
                        
                    }
                }
            }
        }
        

        if (brakeInput > .25f)
            throttleInput = 0f;

        if (carController.speed < 5f && brakeInput >= .5f) {
            brakeInput = 0f;
            handbrakeInput = 1f;
        }
    }

    private float Steering() {
        float targetLaneX = gameManager.GetXLocationForLane(targetLane);

        Vector3 carPosition = transform.position;
        Vector3 targetPosition = new Vector3(targetLaneX, carPosition.y, carPosition.z + 40);
        Vector3 direction = targetPosition - carPosition;
        Vector3 forwardVector = transform.forward;

        //TODO - Remove Drawing
        Debug.DrawRay(carPosition, forwardVector * 40f, Color.yellow);
        Debug.DrawRay(carPosition, direction, Color.blue);

        direction.Normalize();


        float dot = Vector3.Dot(forwardVector.normalized, direction.normalized);
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
        Vector3 forwardPoint = carPosition + forwardVector * 40;
        if (transform.position.x > targetLaneX && forwardPoint.x > targetLaneX) {
            angle *= -1;
        }
        // Just for the thinking process what is needed
        // if (transform.position.x > targetLaneX && forwardPoint.x < targetLaneX) {
        //     angle *= 1;
        // } 
        // if (transform.position.x < targetLaneX && forwardPoint.x < targetLaneX) {
        //     angle *= 1;
        // } 

        if (transform.position.x < targetLaneX && forwardPoint.x > targetLaneX) {
            angle *= -1;
        }

        return angle / 15;
    }


    private void FeedRCC() {
        // Feeding throttleInput of the RCC.
        if (!carController.changingGear && !carController.cutGas)
            carController.throttleInput =
                (carController.direction == 1 ? Mathf.Clamp01(throttleInput) : Mathf.Clamp01(brakeInput));
        else
            carController.throttleInput = 0f;

        if (!carController.changingGear && !carController.cutGas)
            carController.brakeInput =
                (carController.direction == 1 ? Mathf.Clamp01(brakeInput) : Mathf.Clamp01(throttleInput));
        else
            carController.brakeInput = 0f;

        // Feeding steerInput of the RCC.
        if (smoothedSteer)
            carController.steerInput = Mathf.Lerp(carController.steerInput, steerInput, Time.deltaTime * 20f);
        else
            carController.steerInput = steerInput;

        carController.handbrakeInput = handbrakeInput;
    }


    private void Stop() {
        throttleInput = 0f;
        brakeInput = 0f;
        steerInput = 0f;
        handbrakeInput = 1f;
    }

    private void OnDisable() {
        //  Disabling external controller of the vehicle on disable.
        carController.externalController = false;
    }
}