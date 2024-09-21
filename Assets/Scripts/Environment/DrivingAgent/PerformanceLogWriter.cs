using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Utils;

public class PerformanceLogWriter {
    private string filePath;

    //info lines:
    //target speed change (speed) ,
    //lane change start under sign (target lane) ,
    //lane change end when car is in new lane (half car size to center),
    //Optimal lane change start/end   

    //Add total distance to standard output - z value,
    //Current Steering Wheel Angle

    private GameManager gameManager;
    
    public void Init(String fileName, GameManager gameManagerRef) {
        gameManager = gameManagerRef;
        filePath = Application.persistentDataPath + "/" + fileName;
        if (!gameManager.WritePerformanceLog)
            return;
        using (StreamWriter writer = new StreamWriter(filePath)) {
            writer.WriteLine("Time;Distance;DistanceToLaneCenter;Speed;SteeringAngle;Info");
        }
    }

    public void WriteValueLine(float distance, float distanceToCurrentLane, float carSpeed, float steeringAngle) {
        
        if (!gameManager.WritePerformanceLog)
            return;
        
        using (StreamWriter writer = new StreamWriter(filePath, true)) {
            string line = Time.time + ";" +
                          distance + ";" +
                          distanceToCurrentLane + ";" +
                          carSpeed + ";" +
                          steeringAngle + ";";
            writer.WriteLine(line);
        }
    }

    public void WriteInfoLine(InfoLineType type, float value) {
        
        if (!gameManager.WritePerformanceLog)
            return;
        
        using (StreamWriter writer = new StreamWriter(filePath, true)) {
            string line = Time.time + ";0;0;0;0;" + GetInfoLinePrefix(type) + "(" + value + ")";
            writer.WriteLine(line);
        }
    }

    private string GetInfoLinePrefix(InfoLineType type) {
        switch (type) {
            case InfoLineType.LaneChangeStart:
                return "Lane Change Start";
            case InfoLineType.LaneChangeEnd:
                return "Lane Change End";
            case InfoLineType.LaneChangeFailed:
                return "Lane Change Failed";
            case InfoLineType.OptimalLaneChangeStart:
                return "Optimal Lane Change Start";
            case InfoLineType.OptimalLaneChangeEnd:
                return "Optimal Lane Change End";
            case InfoLineType.TargetSpeedChange:
                return "Speed Change";
            default:
                return "Wrong Info Type";
        }
    }
}