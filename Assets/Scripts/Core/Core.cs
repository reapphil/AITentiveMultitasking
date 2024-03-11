using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Core
{
    public static int ExitOnNumberOfCalls { get; set; } = 1;


    private static int s_callCount = 0;


    public static void Exit()
    {
        s_callCount += 1;

        if (s_callCount >= ExitOnNumberOfCalls)
        {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
    }
}
