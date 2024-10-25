using Supervisor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class MeasurementSettings : MonoBehaviour
{
    [field: SerializeField]
    public ProjectSettings ProjectSettings { get; set; }

    [field: SerializeField]
    public const string SETTINGSFILE = "MeasurementSettings.json";


    public static Dictionary<Type, ISettings> Data {
        get
        {
            string settingsPath = Path.Combine(Application.dataPath, "Settings", SETTINGSFILE);

            if (_data == null)
            {
                _data = LoadSettingsFromDisk(settingsPath);                
            }

            return _data;
        }

        set
        {
            _data = value;
        }
    }


    private static Dictionary<Type, ISettings> _data;


    public void WriteSettingsToDisk()
    {
        string settingsPath = Path.Combine(Application.dataPath, "Settings", SETTINGSFILE);
        SettingsLoader.SaveSettings(_data, settingsPath);
    }


    private void OnEnable()
    {
        string settingsPath = Path.Combine(Application.dataPath, "Settings", SETTINGSFILE);

        _data = LoadSettingsFromDisk(settingsPath);
        AssignSettingsToStateinformations();
    }

    private void AssignSettingsToStateinformations()
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        foreach (var task in ProjectSettings.SupervisorAgent.Tasks)
        {
            task.StateInformation.GetType().GetFields(flags).ToList().ForEach(field =>
            {
                ProjectAssignAttribute projectAssignAttribute = (ProjectAssignAttribute)Attribute.GetCustomAttribute(field, typeof(ProjectAssignAttribute));

                if (projectAssignAttribute != null)
                {
                    field.SetValue(task.StateInformation, field.GetValue(_data[task.StateInformation.GetType()]));
                }
            });
        }
    }

    private static Dictionary<Type, ISettings> LoadSettingsFromDisk(string path)
    {
        Dictionary<Type, ISettings> data = null;

        try
        {
             data = SettingsLoader.LoadSettings(path);
        }
        catch (DirectoryNotFoundException e)
        {
            path = Path.Combine(GetSubPath(path, "Build"), "..", "Assets", "Settings", SETTINGSFILE);

            data = SettingsLoader.LoadSettings(path);
        }

        return data ?? new();
    }

    private static string GetSubPath(string fullPath, string directoryName)
    {
        string directoryPath = Path.GetDirectoryName(fullPath);

        int index = directoryPath.IndexOf(directoryName);
        if (index != -1)
        {
            return directoryPath.Substring(0, index + directoryName.Length) + Path.DirectorySeparatorChar;
        }
        else
        {
            return null;
        }
    }
}
