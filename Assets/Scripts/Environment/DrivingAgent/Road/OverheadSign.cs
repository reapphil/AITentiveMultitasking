using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

public class OverheadSign : MonoBehaviour {
    public OverheadSignSingle[] signList = { };

    private Lane lane = Lane.Center;
    
    private GameManager gameManager;
    private Road road;
    
    private void Awake() {
        gameManager = GameManager.Instance;
        road = transform.parent.GetComponent<Road>();
    }
    
    
    public List<SignState> GetSignStates() {
        return signList.Select(overheadSignSingle => overheadSignSingle.signState).ToList();
    }

    /*
     * From left to right 0 - X
     */
    private Lane GetActiveLine() {
        int index = signList.ToList().FindIndex(e => e.signState == SignState.Free);
        if (Enum.IsDefined(typeof(Lane), index)) {
            return (Lane)index;
        }

        Debug.LogError("FAILED TO GET ACTIVE LANE CORRECTLY");
        return Lane.Center;
    }

    public Lane RandomSigns(Lane previousLane) {
        Lane freeLane = EnumHelper.GetRandomEnumValue<Lane>();

        while (freeLane == previousLane) {
            freeLane = EnumHelper.GetRandomEnumValue<Lane>();
        }
        
        switch (freeLane) {
            case Lane.Left:
                signList[0].SetSignState(SignState.Free);
                signList[1].SetSignState(SignState.Blocked);
                signList[2].SetSignState(SignState.Blocked);
                lane = Lane.Left;
                break;
            case Lane.Center:
                signList[0].SetSignState(SignState.Blocked);
                signList[1].SetSignState(SignState.Free);
                signList[2].SetSignState(SignState.Blocked);
                lane = Lane.Center;
                break;
            case Lane.Right:
                signList[0].SetSignState(SignState.Blocked);
                signList[1].SetSignState(SignState.Blocked);
                signList[2].SetSignState(SignState.Free);
                lane = Lane.Right;
                break;
        }
        
        return lane;
        
    }

    public Lane SetSigns(SignState[] newSignStates) {
        if (signList.Length < newSignStates.Length) {
            return 0;
        }

        for (int i = 0; i < signList.Length; i++) {
            signList[i].SetSignState(newSignStates[i]);
        }

        lane = GetActiveLine();
        
        return lane;
        
    }


    private void OnTriggerEnter(Collider other) {
        // Debug.Log("LANE CHANGE");
        if (other.CompareTag("PlayerCar")) {
            if (gameManager.LaneChangeInProgress) {
                gameManager.PerfLogWriter.WriteInfoLine(InfoLineType.LaneChangeFailed,(float) gameManager.TargetLaneOfLaneChange);
            }
            gameManager.PerfLogWriter.WriteInfoLine(InfoLineType.LaneChangeStart,(float) lane);
            gameManager.PerfLogWriter.WriteInfoLine(InfoLineType.OptimalLaneChangeStart,(float) lane);
            gameManager.LaneChangeInProgress = true;
            gameManager.TargetLaneOfLaneChange = lane;
            road.LoggedOptimalLaneChangeEnd = false;
        }
    }
}