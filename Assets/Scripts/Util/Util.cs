using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Linq;
using CsvHelper.TypeConversion;
using System.Reflection;
using System.Text.RegularExpressions;

public static class Util
{
    public static string GetScoreString(SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings)
    {
        string result;

        if (supervisorSettings.randomSupervisor)
        {
            result = String.Format("CD{0}DRI{1}R{2}GD{3}ND{4}DII{5}DP{6}BD{7}BDF{8}S{9}RS{10}", supervisorSettings.setConstantDecisionRequestInterval.ToString()[0],
                                                         supervisorSettings.decisionRequestIntervalInSeconds,
                                                         supervisorSettings.decisionRequestIntervalRangeInSeconds,
                                                         balancingTaskSettings.globalDrag,
                                                         balancingTaskSettings.useNegativeDragDifficulty.ToString()[0],
                                                         supervisorSettings.difficultyIncrementInterval,
                                                         supervisorSettings.decisionPeriod,
                                                         balancingTaskSettings.ballAgentDifficulty,
                                                         balancingTaskSettings.ballAgentDifficultyDivisionFactor,
                                                         balancingTaskSettings.ballStartingRadius,
                                                         balancingTaskSettings.resetSpeed);
        }
        else
        {
            result = String.Format("CD{0}DRI{1}GD{2}ND{3}DII{4}DP{5}BD{6}BDF{7}S{8}RS{9}", supervisorSettings.setConstantDecisionRequestInterval.ToString()[0],
                                                         supervisorSettings.decisionRequestIntervalInSeconds,
                                                         balancingTaskSettings.globalDrag,
                                                         balancingTaskSettings.useNegativeDragDifficulty.ToString()[0],
                                                         supervisorSettings.difficultyIncrementInterval,
                                                         supervisorSettings.decisionPeriod,
                                                         balancingTaskSettings.ballAgentDifficulty,
                                                         balancingTaskSettings.ballAgentDifficultyDivisionFactor,
                                                         balancingTaskSettings.ballStartingRadius,
                                                         balancingTaskSettings.resetSpeed);
        }


        result = result.Replace(',', '.');

        return result;
    }

    public static (string, string, string) BuildPathsForBehavioralDataFileName(string filename, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings)
    {
        return (GetBehavioralDataPath(filename, behavioralDataCollectionSettings, supervisorSettings, balancingTaskSettings),
                GetReactionTimeDataPath(filename, behavioralDataCollectionSettings, supervisorSettings, balancingTaskSettings),
                GetSupervisorSettingsDataPath(filename, behavioralDataCollectionSettings, supervisorSettings, balancingTaskSettings));
    }

    public static (string, string, string) BuildPathsForBehavioralDataFileNameWithoutConfigString(string filename, SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings)
    {
        return (GetBehavioralDataWithoutConfigPath(filename, null, supervisorSettings, balancingTaskSettings),
                GetReactionTimeDataWithoutConfigPath(filename, null, supervisorSettings, balancingTaskSettings),
                GetSupervisorSettingsDataPath(filename, null, supervisorSettings, balancingTaskSettings));
    }

    public static string ConvertRawPathToBehavioralDataPath(string path, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings)
    {
        return ConvertRawPathToDataPath(path, behavioralDataCollectionSettings, supervisorSettings, GetBehavioralDataString);
    }

    public static string ConvertRawPathToReactionTimeDataPath(string path, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings)
    {
        return ConvertRawPathToDataPath(path, behavioralDataCollectionSettings, supervisorSettings, GetReactionTimeDataString);
    }

    public static string ConvertRawPathToSimDataPath(string path)
    {
        return path.Replace("raw", "sim");
    }

    public static string GetBehavioralDataPath(string filename, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings)
    {
        return GetDataPath(filename, behavioralDataCollectionSettings, supervisorSettings, balancingTaskSettings, GetBehavioralDataString);
    }

    public static string GetReactionTimeDataPath(string filename, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings)
    {
        return GetDataPath(filename, behavioralDataCollectionSettings, supervisorSettings, balancingTaskSettings, GetReactionTimeDataString);
    }

    public static string GetBehavioralDataWithoutConfigPath(string filename, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings)
    {
        return GetDataPath(filename, behavioralDataCollectionSettings, supervisorSettings, balancingTaskSettings, GetBehavioralDataWithoutConfigString);
    }

    public static string GetReactionTimeDataWithoutConfigPath(string filename, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings)
    {
        return GetDataPath(filename, behavioralDataCollectionSettings, supervisorSettings, balancingTaskSettings, GetReactionTimeDataWithoutConfigString);
    }

    public static string GetRawBehavioralDataPath(string filename, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings)
    {
        return GetDataPath(filename, behavioralDataCollectionSettings, supervisorSettings, balancingTaskSettings, GetRawBehavioralDataString);
    }

    public static string GetSupervisorSettingsDataPath(string filename, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings)
    {
        return GetDataPath(filename, behavioralDataCollectionSettings, supervisorSettings, balancingTaskSettings, GetScoreDataString);
    }

    public static string GetScoreDataPath()
    {
        string workingDirectory = GetWorkingDirectory();

        return Path.Combine(workingDirectory, "Scores");
    }

    public static string GetSupervisorSettingsDataPath(SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings)
    {
        return Path.Combine(GetScoreDataPath(), GetScoreString(supervisorSettings, balancingTaskSettings));
    }

    public static string GetWorkingDirectory()
    {
        string workingDirectory = Application.dataPath;
        
        return workingDirectory.Replace("Assets", ".");
    }

    public static string GenerateScoreFilename()
    {
        return string.Format("scores_{0}.csv", DateTime.Now.ToString("yyyyMMddHHmm"));
    }

    public static string GenerateBehavioralFilename()
    {
        return string.Format("bD{0}.json", DateTime.Now.ToString("yyyyMMddHHmm"));
    }

    public static string GetJSONFilenameForBehavioralDataConfigString(string filename)
    {
        string[] parts = filename.Split("NA");
        parts[0] = Path.ChangeExtension(parts[0], null);

        return parts[0] + ".json";
    }

    public static string GetCSVFilenameForBehavioralDataConfigString(string filename)
    {
        string[] parts = filename.Split("NA");
        parts[0] = Path.ChangeExtension(parts[0], null);

        return parts[0] + ".csv";
    }

    public static void SaveDataToCSV<T>(string path, List<T> data, bool overwritte = false)
    {
        var options = new TypeConverterOptions { Formats = new[] { "MM/dd/yyyy hh:mm:ss.fffffff tt" } };

        if (File.Exists(path) && !overwritte)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // Don't write the header again.
                HasHeaderRecord = false,
            };
            
            using (var stream = File.Open(path, FileMode.Append))
            using (var writer = new StreamWriter(stream))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(data);
            }
            Debug.Log(String.Format("Write data to existing file {0}", path));
        }
        else
        {
            try
            {
                using (var stream = File.Open(path, FileMode.Create))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                    csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                    csv.WriteRecords(data);
                }
                Debug.Log(String.Format("Write data to new file {0}", path));
            }
            catch (DirectoryNotFoundException)
            {
                Debug.Log(string.Format("Directory {0} not found, create new directory.", Path.GetDirectoryName(path)));
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (var writer = new StreamWriter(path))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(data);
                }
                Debug.Log(String.Format("Write data to new file {0}", path));
            }
        }
    }

    public static List<T> ReadDataFromCSV<T>(string path)
    {
        List<T> records = new List<T>();

        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            records = csv.GetRecords<T>().ToList();
        }

        return records;
    }

    public static T ImportJson<T>(string path)
    {
        StreamReader reader = new StreamReader(path);
        string json = reader.ReadToEnd();

        return JsonUtility.FromJson<T>(json);
    }

    public static void ExportJson(string path, object obj)
    {
        File.WriteAllText(path, JsonUtility.ToJson(obj));
    }

    public static Dictionary<string, string> GetArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        Dictionary<string, string> argsDict = new Dictionary<string, string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i][0] == '-')
            {
                if (i + 1 < args.Length && (args[i + 1][0] != '-' || Char.IsNumber(args[i + 1][1])))
                {
                    argsDict.Add(args[i], args[i + 1]);
                }
                else
                {
                    argsDict.Add(args[i], null);
                }
            }
        }

        return argsDict;
    }

    public static bool HasDefaultValuesForFields<T>(T t)
    {
        BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        foreach (var thisVar in t.GetType().GetFields(bindFlags))
        {
            if (thisVar.GetValue(t) is not null && (!object.Equals(thisVar.GetValue(t), GetDefault(thisVar.GetValue(t).GetType()))))
            {
                return false;
            }
        }

        return true;
    }

    public static void AssignNonDefault<T>(T assignee, T assigner)
    {
        if (assignee == null || assigner == null)
        {
            throw new ArgumentNullException();
        }

        Type type = typeof(T);

        MemberInfo[] members = type.GetMemberInfos(BindingFlags.Public | BindingFlags.Instance);

        foreach (MemberInfo member in members)
        {
            object assignerValue = member.GetValue(assigner);
            object defaultValue = GetDefault(member.GetUnderlyingType());

            if (!object.Equals(assignerValue, defaultValue) || member.GetUnderlyingType() == typeof(bool))
            {
                member.SetValue(assignee, assignerValue);
            } 
        }
    }

    public static object GetDefault(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    public static Type GetType(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type != null) return type;
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = a.GetType(typeName);
            if (type != null)
                return type;
        }
        return null;
    }

    public static string FormatFieldName(string input)
    {
        Match match = Regex.Match(input, @"<([a-zA-Z_]+)>([a-zA-Z_]+)");

        if (match.Success)
        {
            string prefix = match.Groups[1].Value;
            return $"{FormatName(prefix)}";
        }
        else
        {
            return input;
        }
    }


    private static string FormatName(string prefix)
    {
        // Split camel case and join with space for the prefix
        return Regex.Replace(prefix, @"(\p{Lu})", " $1").Trim();
    }

    private static string GetDataPath(string filename, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings, Func<string, string, BehavioralDataCollectionSettings, SupervisorSettings, string, string> getString)
    {
        if (filename is null || filename == "")
        {
            filename = GenerateBehavioralFilename();
        }

        string extension = Path.GetExtension(filename);
        string[] parts = filename.Split("NA");

        string path = Path.Combine(GetScoreDataPath(), GetScoreString(supervisorSettings, balancingTaskSettings));

        string id = Path.ChangeExtension(parts[0], null);

        return getString(path, id, behavioralDataCollectionSettings, supervisorSettings, extension);
    }

    private static string GetDataPath(string filename, BehavioralDataCollectionSettings behavioralDataCollectionSettings, Func<string, string, BehavioralDataCollectionSettings, string, string> getString)
    {
        if (filename is null || filename == "")
        {
            filename = GenerateBehavioralFilename();
        }

        string extension = Path.GetExtension(filename);
        string[] parts = filename.Split("NA");

        string path = GetScoreDataPath();

        string id = Path.ChangeExtension(parts[0], null);

        return getString(path, id, behavioralDataCollectionSettings, extension);
    }

    private static string ConvertRawPathToDataPath(string path, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, Func<string, string, BehavioralDataCollectionSettings, SupervisorSettings, string, string> getString)
    {
        string scorePath = Path.GetDirectoryName(path);
        string filename = Path.GetFileName(path);

        filename = filename.Replace("raw", "");
        string id = Path.ChangeExtension(filename, null);

        string dataPath = getString(scorePath, id, behavioralDataCollectionSettings, supervisorSettings, ".json");

        if (File.Exists(dataPath))
        {
            string extension = Path.GetExtension(dataPath);
            string dataPathold = dataPath;

            dataPath = Path.ChangeExtension(dataPath, null);
            dataPath = string.Format("{0}conv{1}", dataPath, extension);

            Debug.Log(string.Format("Path {0} already exists, therefore instead converting path to {1}", dataPathold, dataPath));
        }

        return dataPath;
    }

    private static string GetBehavioralDataString(string path, string id, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, string extension)
    {
        return Path.Combine(path, String.Format("{0}NA{1}NAN{2}NV{3}{4}", id, behavioralDataCollectionSettings.numberOfAreaBins_BehavioralData, behavioralDataCollectionSettings.numberOfAngleBinsPerAxis, behavioralDataCollectionSettings.numberOfBallVelocityBinsPerAxis_BehavioralData, extension));
    }

    private static string GetReactionTimeDataString(string path, string id, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, string extension)
    {
        int numberOfTimeBins = behavioralDataCollectionSettings.numberOfTimeBins;

        if (!supervisorSettings.randomSupervisor)
        {
            numberOfTimeBins = 1;
        }

        return Path.Combine(path, String.Format("{0}_rt_NT{1}ND{2}NVD{3}NA{4}{5}", id, numberOfTimeBins, behavioralDataCollectionSettings.numberOfDistanceBins, behavioralDataCollectionSettings.numberOfDistanceBins_velocity, behavioralDataCollectionSettings.numberOfActionBinsPerAxis, extension));
    }

    private static string GetBehavioralDataWithoutConfigString(string path, string id, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, string extension)
    {
        return Path.Combine(path, String.Format("{0}{1}", id, extension));
    }

    private static string GetReactionTimeDataWithoutConfigString(string path, string id, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, string extension)
    {
        return Path.Combine(path, String.Format("{0}_rt{1}", id, extension));
    }

    private static string GetRawBehavioralDataString(string path, string id, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, string extension)
    {
        return Path.Combine(path, String.Format("{0}raw{1}", id, ".csv"));
    }

    private static string GetScoreDataString(string path, string id, BehavioralDataCollectionSettings behavioralDataCollectionSettings, SupervisorSettings supervisorSettings, string extension)
    {
        return Path.Combine(path, String.Format("scores_{0}{1}", id, ".csv"));
    }
}
