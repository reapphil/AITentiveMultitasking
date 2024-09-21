using CsvHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public static class BallTaskUtil
{
    public static void AddSecondsUntilGameOverToSwitchingData(string path)
    {
        var dataDict = Util.ReadDatafromCSV(path);
        List<Dictionary<string, string>> result = ProcessDataset(dataDict);

        string ext = RemoveFileExtension(ref path);
        string newPath = path + "_pro" + ext;

        Util.SaveDataToCSV(newPath, result);

        Debug.Log(string.Format("Processed data saved to {0}.", newPath));
    }


    private static List<Dictionary<string, string>> ProcessDataset(List<Dictionary<string, string>> dataset)
    {
        List<Dictionary<string, string>> result = new();

        float radius = 5.0f;

        foreach (var dataRow in dataset)
        {
            float dragValue = float.Parse(dataRow["DragValue"]);

            Vector3 ballPositionSource = GetVector(dataRow, "BallPosition", "Source");
            Vector3 ballVelocitySource = GetVector(dataRow, "BallVelocity", "Source");
            float slopeSource = CalculateSlope(GetVector(dataRow, "PlatformAngle", "Source"));
            float secondsUntilGameOverSource = Distances.SecondsUntilGameOver(radius, slopeSource, ballPositionSource, ballVelocitySource, dragValue);
            dataRow["SecondsUntilGameOverSource"] = secondsUntilGameOverSource.ToString();

            Vector3 ballPositionTarget = GetVector(dataRow, "BallPosition", "Target");
            Vector3 ballVelocityTarget = GetVector(dataRow, "BallVelocity", "Target");
            float slopeTarget = CalculateSlope(GetVector(dataRow, "PlatformAngle", "Target"));
            float secondsUntilGameOverTarget = Distances.SecondsUntilGameOver(radius, slopeTarget, ballPositionTarget, ballVelocityTarget, dragValue);
            dataRow["SecondsUntilGameOverTarget"] = secondsUntilGameOverTarget.ToString();

            result.Add(dataRow);
        }

        return result;
    }

    private static Vector3 GetVector(Dictionary<string, string> row, string valueName, string platformName)
    {
        return new Vector3(
                float.Parse(row[string.Format("{0}{1}X", valueName, platformName)]),
                float.Parse(row[string.Format("{0}{1}Y", valueName, platformName)]),
                float.Parse(row[string.Format("{0}{1}Z", valueName, platformName)])
        );
    }

    private static float CalculateSlope(Vector3 eulerAngles)
    {
        // Convert angles to the range [-180, 180]
        float pitch = NormalizeAngle(eulerAngles.x); // Rotation around the X axis
        float roll = NormalizeAngle(eulerAngles.z);  // Rotation around the Z axis

        // You can calculate the magnitude of both pitch and roll to get the total slope
        Vector2 slopeVector = new Vector2(pitch, roll);
        float totalSlope = slopeVector.magnitude;

        return totalSlope;
    }

    // Helper function to normalize an angle to the range [-180, 180]
    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }
        return angle;
    }

    private static string RemoveFileExtension(ref string filePath)
    {
        string extension = Path.GetExtension(filePath);
        if (!string.IsNullOrEmpty(extension))
        {
            filePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath));
        }
        return extension;
    }
}