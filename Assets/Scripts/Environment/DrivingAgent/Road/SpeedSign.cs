using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpeedSign : MonoBehaviour {

    [field: SerializeField]
    public TextMeshPro SpeedSignText { get; set; }

    [field: SerializeField]
    public TextMeshPro SpeedSignText2 { get; set; }

    [field: SerializeField]
    public int SignSpeed { get; set; } = 100;


    private GameManager _gameManager;


    public void SetSpeedText(int speed) {
        SignSpeed = speed;
        SpeedSignText.text = "" + speed;
        SpeedSignText2.text = "" + speed;
    }


    private void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag("PlayerCar")) 
        {
            GameManager.Instance.UpdateCurrentTargetSpeed(SignSpeed);
        }
    }
}