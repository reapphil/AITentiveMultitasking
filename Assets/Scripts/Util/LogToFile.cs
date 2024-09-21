using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Reflection;
using System.Linq;

public class LogToFile : MonoBehaviour
{
    public static void Log(string logString, string stackTrace, LogType type)
    {
        try
        {
            string workingDirectory = Application.dataPath;
            workingDirectory = workingDirectory.Replace("Assets", ".");
            string filename = Path.Combine(workingDirectory, "Logs", "LogFile.txt");

            TextWriter tw = new StreamWriter(filename, true);

            tw.WriteLine("[" + System.DateTime.Now + "]" + logString);
            tw.WriteLine(stackTrace);

            tw.Close();

            if (type == LogType.Exception || type == LogType.Error)
            {
                Core.Exit();
            }
        }
        catch (IOException){}
    }

    public static void LogPropertiesFieldsOfObject(object obj)
    {
        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
;
        MemberInfo[] members = obj.GetType().GetMemberInfos(bindingFlags);

        string str = String.Format("Component {0} has the following parameters:\n", obj.GetType());
        foreach (var thisVar in members)
        {
            try
            {
                if (thisVar.GetUnderlyingType() != typeof(char)) //chars are not correctly logged
                {
                    str += String.Format("{0} = {1}\n", thisVar.Name, thisVar.GetValue(obj));
                }
            }
            catch (Exception e)
            {
                if(e.InnerException != null && e.InnerException.GetType() == typeof(NotImplementedException))
                {
                    Debug.Log(string.Format("{0}: {1}", thisVar.Name, "Not implemented."));
                }
                else
                {
                    Debug.LogWarning(string.Format("{0}: {1}", thisVar.Name, e));
                }
                
            }
        }

        Debug.Log(str);
    }

    //Prefix must contain placeholder for index {0} and value {1}
    public static void LogArray<T>(T[] array, string prefix = "")
    {
        string arrayString = "";

        if (prefix == "")
        {
            for (int i = 0; i < array.Length; i++)
            {
                arrayString = arrayString + string.Format("{0} ", array[i]);
            }
        }
        else
        {
            for (int i = 0; i < array.Length; i++)
            {
                arrayString = arrayString + string.Format(prefix, i, array[i]);
            }
        }

        Debug.Log(arrayString);
    }


    private void OnEnable()
    {
        Application.logMessageReceived += Log;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= Log;
    }
}
