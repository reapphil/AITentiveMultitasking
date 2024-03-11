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

/// <summary>
/// UI telemetry for info.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/RCC Telemetry")]
public class RCC_Telemetry : MonoBehaviour {

    private RCC_CarControllerV3 carController;
    public GameObject mainPanel;

    public Text RPM_WheelFL;
    public Text RPM_WheelFR;
    public Text RPM_WheelRL;
    public Text RPM_WheelRR;

    public Text Torque_WheelFL;
    public Text Torque_WheelFR;
    public Text Torque_WheelRL;
    public Text Torque_WheelRR;

    public Text Brake_WheelFL;
    public Text Brake_WheelFR;
    public Text Brake_WheelRL;
    public Text Brake_WheelRR;

    public Text Force_WheelFL;
    public Text Force_WheelFR;
    public Text Force_WheelRL;
    public Text Force_WheelRR;

    public Text Angle_WheelFL;
    public Text Angle_WheelFR;
    public Text Angle_WheelRL;
    public Text Angle_WheelRR;

    public Text Sideways_WheelFL;
    public Text Sideways_WheelFR;
    public Text Sideways_WheelRL;
    public Text Sideways_WheelRR;

    public Text Forward_WheelFL;
    public Text Forward_WheelFR;
    public Text Forward_WheelRL;
    public Text Forward_WheelRR;

    public Text ABS;
    public Text ESP;
    public Text TCS;

    public Text GroundHit_WheelFL;
    public Text GroundHit_WheelFR;
    public Text GroundHit_WheelRL;
    public Text GroundHit_WheelRR;

    public Text speed;
    public Text engineRPM;
    public Text gear;
    public Text finalTorque;
    public Text drivetrain;
    public Text angularVelocity;
    public Text controllable;

    public Text throttle;
    public Text steer;
    public Text brake;
    public Text handbrake;
    public Text clutch;

    private void Update() {

        if (mainPanel.activeSelf != RCC_Settings.Instance.useTelemetry)
            mainPanel.SetActive(RCC_Settings.Instance.useTelemetry);

        carController = RCC_SceneManager.Instance.activePlayerVehicle;

        if (!carController)
            return;

        RPM_WheelFL.text = carController.FrontLeftWheelCollider.WheelCollider.rpm.ToString("F0");
        RPM_WheelFR.text = carController.FrontRightWheelCollider.WheelCollider.rpm.ToString("F0");
        RPM_WheelRL.text = carController.RearLeftWheelCollider.WheelCollider.rpm.ToString("F0");
        RPM_WheelRR.text = carController.RearRightWheelCollider.WheelCollider.rpm.ToString("F0");

        Torque_WheelFL.text = carController.FrontLeftWheelCollider.WheelCollider.motorTorque.ToString("F0");
        Torque_WheelFR.text = carController.FrontRightWheelCollider.WheelCollider.motorTorque.ToString("F0");
        Torque_WheelRL.text = carController.RearLeftWheelCollider.WheelCollider.motorTorque.ToString("F0");
        Torque_WheelRR.text = carController.RearRightWheelCollider.WheelCollider.motorTorque.ToString("F0");

        Brake_WheelFL.text = carController.FrontLeftWheelCollider.WheelCollider.brakeTorque.ToString("F0");
        Brake_WheelFR.text = carController.FrontRightWheelCollider.WheelCollider.brakeTorque.ToString("F0");
        Brake_WheelRL.text = carController.RearLeftWheelCollider.WheelCollider.brakeTorque.ToString("F0");
        Brake_WheelRR.text = carController.RearRightWheelCollider.WheelCollider.brakeTorque.ToString("F0");

        Force_WheelFL.text = carController.FrontLeftWheelCollider.bumpForce.ToString("F0");
        Force_WheelFR.text = carController.FrontRightWheelCollider.bumpForce.ToString("F0");
        Force_WheelRL.text = carController.RearLeftWheelCollider.bumpForce.ToString("F0");
        Force_WheelRR.text = carController.RearRightWheelCollider.bumpForce.ToString("F0");

        Angle_WheelFL.text = carController.FrontLeftWheelCollider.WheelCollider.steerAngle.ToString("F0");
        Angle_WheelFR.text = carController.FrontRightWheelCollider.WheelCollider.steerAngle.ToString("F0");
        Angle_WheelRL.text = carController.RearLeftWheelCollider.WheelCollider.steerAngle.ToString("F0");
        Angle_WheelRR.text = carController.RearRightWheelCollider.WheelCollider.steerAngle.ToString("F0");

        Sideways_WheelFL.text = carController.FrontLeftWheelCollider.wheelSlipAmountSideways.ToString("F");
        Sideways_WheelFR.text = carController.FrontRightWheelCollider.wheelSlipAmountSideways.ToString("F");
        Sideways_WheelRL.text = carController.RearLeftWheelCollider.wheelSlipAmountSideways.ToString("F");
        Sideways_WheelRR.text = carController.RearRightWheelCollider.wheelSlipAmountSideways.ToString("F");

        Forward_WheelFL.text = carController.FrontLeftWheelCollider.wheelSlipAmountForward.ToString("F");
        Forward_WheelFR.text = carController.FrontRightWheelCollider.wheelSlipAmountForward.ToString("F");
        Forward_WheelRL.text = carController.RearLeftWheelCollider.wheelSlipAmountForward.ToString("F");
        Forward_WheelRR.text = carController.RearRightWheelCollider.wheelSlipAmountForward.ToString("F");

        ABS.text = carController.ABSAct ? "Engaged" : "Not Engaged";
        ESP.text = carController.ESPAct ? "Engaged" : "Not Engaged";
        TCS.text = carController.TCSAct ? "Engaged" : "Not Engaged";

        GroundHit_WheelFL.text = carController.FrontLeftWheelCollider.isGrounded ? carController.FrontLeftWheelCollider.wheelHit.collider.name : "";
        GroundHit_WheelFR.text = carController.FrontRightWheelCollider.isGrounded ? carController.FrontRightWheelCollider.wheelHit.collider.name : "";
        GroundHit_WheelRL.text = carController.RearLeftWheelCollider.isGrounded ? carController.RearLeftWheelCollider.wheelHit.collider.name : "";
        GroundHit_WheelRR.text = carController.RearRightWheelCollider.isGrounded ? carController.RearRightWheelCollider.wheelHit.collider.name : "";

        speed.text = carController.speed.ToString("F0");
        engineRPM.text = carController.engineRPM.ToString("F0");
        gear.text = carController.currentGear.ToString("F0");

        switch (carController.wheelTypeChoise) {

            case RCC_CarControllerV3.WheelType.FWD:

                drivetrain.text = "FWD";
                break;

            case RCC_CarControllerV3.WheelType.RWD:

                drivetrain.text = "RWD";
                break;

            case RCC_CarControllerV3.WheelType.AWD:

                drivetrain.text = "AWD";
                break;

        }

        angularVelocity.text = carController.Rigid.angularVelocity.ToString();
        controllable.text = carController.canControl ? "True" : "False";

        throttle.text = carController.throttleInput.ToString("F");
        steer.text = carController.steerInput.ToString("F");
        brake.text = carController.brakeInput.ToString("F");
        handbrake.text = carController.handbrakeInput.ToString("F");
        clutch.text = carController.clutchInput.ToString("F");

    }

}
