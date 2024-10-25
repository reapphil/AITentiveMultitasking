using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;


public static class SettingsLoader
{
    public static Dictionary<Type, ISettings> LoadSettings(string path)
    {
        StreamReader reader = new StreamReader(path);
        string json = reader.ReadToEnd();
        reader.Close();

        var settingsDict = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(json);
        var resultDict = new Dictionary<Type, ISettings>(new TypeEqualityComparer());

        if(settingsDict == null)
        {
            return resultDict;
        }

        foreach (var kvp in settingsDict)
        {
            Type type = Util.GetType(kvp.Key.FirstCharToUpper());

            foreach (ISettings setting in ConfigVersioning.UnifySettings(kvp.Value.ToString(), type))
            {
                if(setting.GetType().IsSubclassOf((typeof(JsonSettings))) || setting.GetType() == typeof(JsonSettings))
                {
                    resultDict[((JsonSettings) setting).type] = setting;
                }
                else if(resultDict.ContainsKey(setting.GetType()))
                {
                    resultDict[setting.GetType()] = MergeSettings(resultDict[setting.GetType()], setting);
                }
                else
                {
                    resultDict.Add(setting.GetType(), setting);
                }
            }
        }

        return resultDict;
    }

    public static void SaveSettings(Dictionary<Type, ISettings> settings, string path)
    {
        string json = JsonConvert.SerializeObject(settings, Formatting.Indented, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        StreamWriter writer = new StreamWriter(path);
        writer.Write(json);
        writer.Close();
    }


    private static ISettings MergeSettings(ISettings settings1, ISettings settings2)
    {
        Type type = settings1.GetType();

        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        MemberInfo[] members = type.GetMemberInfos(bindingFlags);

        foreach (MemberInfo memberInfo in members)
        {
            object value2 = memberInfo.GetValue(settings2);
            object value1 = memberInfo.GetValue(settings1);

            if (value1 == null || (value2 != null && !value2.Equals(Util.GetDefault(value2.GetType()))))
            {
                //Overwrite the value only if the value of settings1 is null or the default value if the type would not be nullable. So for instance,
                //if one value is true and the other is false, the value will be true since the default value of bool is false.
                if (Nullable.GetUnderlyingType(memberInfo.GetUnderlyingType()) != null && value1 != Util.GetDefault(Nullable.GetUnderlyingType(memberInfo.GetUnderlyingType())))
                {
                    memberInfo.SetValue(settings1, value2);
                }
            }
        }

        return settings1;
    }
}

