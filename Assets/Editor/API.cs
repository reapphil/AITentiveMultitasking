using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
public class API
{
    public static void ConvertRawToBinData()
    {
        List<string> args = APIHelper.GetArgs();

        string oldScenePath = SceneManagement.BackUpScene();
        SceneManagement.ConfigIsRawDataCollected(false);

        BehaviourMeasurementConverterAPI.ConvertRawToBinData(args[0], args[1], args[2]);

        SceneManagement.RestoreScene(oldScenePath);
    }
}
**/
