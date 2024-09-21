using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerCar : MonoBehaviour {
    public TextMeshPro SpeedText;


    private RCC_CarControllerV3 carControllerV3;

    void Start() {
        carControllerV3 = GetComponent<RCC_CarControllerV3>();
    }

    void Update() {
        #region Player Related

        SpeedText.text = (int)carControllerV3.speed + "";

        #endregion
    }
}