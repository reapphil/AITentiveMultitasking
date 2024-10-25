using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using CsvHelper;
using System.Globalization;
using System.Linq;
using CsvHelper.TypeConversion;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using System.ComponentModel;
using UnityEngine.Profiling;
using CsvHelper.Configuration;
using System.Collections;

public static class Util
{
    public static string GetScoreString(SupervisorSettings supervisorSettings, Hyperparameters hyperparameters)
    {
        string result;

        if (supervisorSettings.randomSupervisor.GetValueOrDefault())
        {
            result = String.Format("CD{0}DRI{1}R{2}DII{3}DP{4}{5}", supervisorSettings.setConstantDecisionRequestInterval.ToString()[0],
                                                         supervisorSettings.decisionRequestIntervalInSeconds,
                                                         supervisorSettings.decisionRequestIntervalRangeInSeconds,
                                                         supervisorSettings.difficultyIncrementInterval,
                                                         supervisorSettings.decisionPeriod,
                                                         GetTaskString(hyperparameters));
        }
        else
        {
            result = String.Format("CD{0}DRI{1}DII{2}DP{3}{4}", supervisorSettings.setConstantDecisionRequestInterval.ToString()[0],
                                                         supervisorSettings.decisionRequestIntervalInSeconds,
                                                         supervisorSettings.difficultyIncrementInterval,
                                                         supervisorSettings.decisionPeriod,
                                                         GetTaskString(hyperparameters));
        }


        result = result.Replace(',', '.');

        return result;
    }

    public static string ConvertRawPathToBehavioralDataPath(string path, int[]  dimensions, SupervisorSettings supervisorSettings, string name = "")
    {
        return ConvertRawPathToDataPath(path, dimensions, supervisorSettings, GetBehavioralDataString, name);
    }

    public static string ConvertRawPathToReactionTimeDataPath(string path, int[]  dimensions, SupervisorSettings supervisorSettings, string name = "")
    {
        return ConvertRawPathToDataPath(path, dimensions, supervisorSettings, GetReactionTimeDataString, name);
    }

    public static string ConvertRawPathToSimDataPath(string path)
    {
        return path.Replace("raw", "sim");
    }

    public static string GetBehavioralDataPath(string filename, SupervisorSettings supervisorSettings, Hyperparameters hyperparameters, string name, int[] dimensions)
    {
        return GetDataPath(filename, supervisorSettings, hyperparameters, GetBehavioralDataString, name, dimensions);
    }

    public static string GetReactionTimeDataPath(string filename, SupervisorSettings supervisorSettings, Hyperparameters hyperparameters, string name, int[] dimensions)
    {
        return GetDataPath(filename, supervisorSettings, hyperparameters, GetReactionTimeDataString, name, dimensions);
    }

    public static string GetBehavioralDataWithoutConfigPath(string filename, SupervisorSettings supervisorSettings, Hyperparameters hyperparameters, string name = "", int[] dimensions = null)
    {
        return GetDataPath(filename, supervisorSettings, hyperparameters, GetBehavioralDataWithoutConfigString);
    }

    public static string GetReactionTimeDataWithoutConfigPath(string filename, SupervisorSettings supervisorSettings, Hyperparameters hyperparameters, string name = "", int[] dimensions = null)
    {
        return GetDataPath(filename, supervisorSettings, hyperparameters, GetReactionTimeDataWithoutConfigString);
    }

    public static string GetRawBehavioralDataPath(string filename, SupervisorSettings supervisorSettings, Hyperparameters hyperparameters)
    {
        return GetDataPath(filename, supervisorSettings, hyperparameters, GetRawBehavioralDataString);
    }

    public static string GetSupervisorSettingsDataPath(string filename, SupervisorSettings supervisorSettings, Hyperparameters hyperparameters)
    {
        return GetDataPath(filename, supervisorSettings, hyperparameters, GetScoreDataString);
    }

    public static string GetABCBehavioralDataPath(string scorePath, string id, int[] dimensions, string extension, string name, int simulationId)
    {
        return Path.Combine(scorePath, String.Format("{0}behavior_sim{1}{2}", simulationId, ShortenString(name), extension));
    }

    public static string GetABCReactionTimeDataPath(string scorePath, string id, int[] dimensions, string extension, string name, int simulationId)
    {
        return Path.Combine(scorePath, String.Format("{0}rt_sim{1}{2}", simulationId, ShortenString(name), extension));
    }

    public static string GetScoreDataPath()
    {
        string workingDirectory = GetWorkingDirectory();

        return Path.Combine(workingDirectory, "Scores");
    }

    public static string GetSupervisorSettingsDataPath(SupervisorSettings supervisorSettings, Hyperparameters hyperparameters)
    {
        return Path.Combine(GetScoreDataPath(), GetScoreString(supervisorSettings, hyperparameters));
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
            WriteToCSV(path, options, data, FileMode.Append);
        }
        else
        {
            try
            {
                WriteToCSV(path, options, data, FileMode.Create);
            }
            catch (DirectoryNotFoundException)
            {
                Debug.Log(string.Format("Directory {0} not found, create new directory.", Path.GetDirectoryName(path)));
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                WriteToCSV(path, options, data, FileMode.Create);
            }
        }
    }

    public static void SaveDataToCSV(string path, List<Dictionary<string, string>> dataset)
    {
        if (dataset == null || !dataset.Any())
        {
            throw new ArgumentException("The dataset is empty or null.");
        }

        // Step 1: Determine the headers (unique keys across all dictionaries)
        var headers = dataset.SelectMany(dict => dict.Keys).Distinct().ToList();

        using (var writer = new StreamWriter(path))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            // Step 2: Write the header row
            foreach (var header in headers)
            {
                csv.WriteField(header);
            }
            csv.NextRecord();

            // Step 3: Write each row of the dataset
            foreach (var dict in dataset)
            {
                foreach (var header in headers)
                {
                    // Write the value if the key exists, otherwise write an empty string
                    csv.WriteField(dict.ContainsKey(header) ? dict[header] : "");
                }
                csv.NextRecord();
            }
        }

        Console.WriteLine($"CSV file saved to {path}");
    }

    public static List<T> ReadDatafromCSV<T>(string path)
    {
        List<T> records = new List<T>();

        if (HasReferenceTypeFields<T>())
        {
            records = ReadComplexDataFromCSV<T>(path);
        }
        else
        {
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                records = csv.GetRecords<T>().ToList();
            }
        }

        return records;
    }

    public static List<Dictionary<string, string>> ReadDatafromCSV(string path)
    {
        var dataList = new List<Dictionary<string, string>>();

        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            // Read records as a list of dictionaries
            var records = csv.GetRecords<object>();
            foreach (var record in records)
            {
                var dict = new Dictionary<string, string>();
                foreach (var kvp in (IDictionary<string, object>)record)
                {
                    dict[kvp.Key] = kvp.Value.ToString();
                }
                dataList.Add(dict);
            }
        }

        return dataList;
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
        if (typeName == null)
        {
            return null;
        }

        var type = Type.GetType(typeName);
        if (type != null)
            return type;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
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

    public static string ShortenString(string input, int maxLength = 10)
    {
        if (string.IsNullOrEmpty(input) || maxLength <= 0)
        {
            return string.Empty;
        }

        if (input.Length <= maxLength)
        {
            return input;
        }

        var words = Regex.Matches(input, @"([A-Z][a-z]+|\d+|[a-z]+)");

        string shortened = "";
        foreach (Match word in words.ToList())
        {
            shortened += word.Value[0];
        }

        return shortened.Substring(0, Math.Min(maxLength, shortened.Length));
    }

    public static List<int[]> GetIndicesForDimentions(int[] dimensions, int currentIndex = 0, int[] currentCombination = null)
    {
        if (currentCombination == null)
        {
            currentCombination = new int[dimensions.Length];
        }

        List<int[]> combinations = new List<int[]>();

        if (currentIndex == dimensions.Length - 1)
        {
            for (int i = 0; i < dimensions[currentIndex]; i++)
            {
                currentCombination[currentIndex] = i;
                combinations.Add((int[])currentCombination.Clone());
            }
        }
        else
        {
            for (int i = 0; i < dimensions[currentIndex]; i++)
            {
                currentCombination[currentIndex] = i;
                combinations.AddRange(GetIndicesForDimentions(dimensions, currentIndex + 1, currentCombination));
            }
        }

        return combinations;
    }

    public static List<T> FlattenArray<T>(Array array)
    {
        var flatList = new List<T>();
        FlattenArrayRecursive(array, new int[array.Rank], 0, flatList);
        return flatList;
    }

    public static void CopyDirectory(string sourcePath, string targetPath)
    {
        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }


    private static void FlattenArrayRecursive<T>(Array array, int[] indices, int dimension, List<T> flatList)
    {
        if (dimension == array.Rank)
        {
            flatList.Add((T)array.GetValue(indices));
        }
        else
        {
            for (int i = array.GetLowerBound(dimension); i <= array.GetUpperBound(dimension); i++)
            {
                indices[dimension] = i;
                FlattenArrayRecursive(array, indices, dimension + 1, flatList);
            }
        }
    }

    private static bool HasReferenceTypeFields<T>()
    {
        var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var field in fields)
        {
            if (!field.FieldType.IsValueType && field.FieldType != typeof(string))
            {
                return true;
            }
        }

        return false;
    }

    private static List<T> ReadComplexDataFromCSV<T>(string path)
    {
        List<T> records = new List<T>();

        var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);
        config.MissingFieldFound = null;

        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, config))
        {
            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                T record = ReadLine<T>(csv);
                records.Add(record);
            }
        }

        return records;
    }

    private static T ReadLine<T>(CsvReader csv, string prefix = "")
    {
        return (T)ReadLine(typeof(T), csv, prefix);
    }

    private static object ReadLine(Type type, CsvReader csv, string prefix = "")
    {
        object value = Activator.CreateInstance(type);

        foreach (MemberInfo member in type.GetMembers())
        {
            Type memberType = member.GetUnderlyingType();

            if(member.MemberType == System.Reflection.MemberTypes.Field || member.MemberType == System.Reflection.MemberTypes.Property)
            {
                //Debug.Log("memberType: " + memberType + "; member.Name: " + member.Name + "; value: " + csv.GetField(prefix + member.Name));

                if (memberType.IsPrimitive)
                {
                    if(csv.GetField(prefix + member.Name) != null)
                    {
                        member.SetValue(value, csv.GetField(memberType, prefix + member.Name));
                    }
                }
                else
                {
                    //Debug.Log("memberType: " + memberType + "; csv.GetField(string.Format(\"{0}_{1}\", member.Name, \"Name\"))" + csv.GetField(string.Format("{0}_{1}", member.Name, "Name")));
                    //e.g. member.Name = SourceState --> string.Format = SourceState_Name --> csv.GetField = BallStateInformation
                    string dynamicTypeString = csv.GetField(string.Format("{0}_{1}", member.Name, "Name"));

                    if(dynamicTypeString != null)
                    {
                        Type dynamicType = GetType(dynamicTypeString);
                        member.SetValue(value, ReadLine(dynamicType, csv, member.Name + "_"));
                    }
                }
            }
        }

        return value;
    }

    private static void WriteToCSV<T>(string path, TypeConverterOptions options, List<T> data, FileMode mode)
    {
        if(data.IsNullOrEmpty())
        {
            Debug.LogWarning("No data to write to CSV");
            return;
        }

        using (var stream = File.Open(path, mode))
        using (var writer = new StreamWriter(stream))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
            csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);

            List<Type> structure = GetStructureOfCSV(csv, data);

            if (mode == FileMode.Create)
            {
                WriteHeaderToCSV(csv, data);
                csv.NextRecord();
            }

            foreach (var record in data)
            {
                WriteRecordToCSV(csv, record, structure);
                csv.NextRecord();
            }
        }

        if (mode == FileMode.Create)
        {
            Debug.Log(String.Format("Write data to new file {0}", path));
        }
        else if (mode == FileMode.Append)
        {
            Debug.Log(String.Format("Write data to existing file {0}", path));
        }
    }

    private static List<Type> GetStructureOfCSV<T>(CsvWriter csv, List<T> data)
    {
        MemberInfo[] members = GetMemberInfo(data[0].GetType());

        List<Type> structure = new List<Type>
        {
            data[0].GetType()
        };

        foreach (var thisVar in members)
        {
            if (!HasCSVHelperBuiltInConverter(thisVar.GetUnderlyingType()))
            {
                try
                {
                    List<T> records = data.Where(x => thisVar.GetValue(x) != null).ToList();
                    List<object> values = records.Select(x => thisVar.GetValue(x)).ToList();
                    if (values.Count > 0)
                    {
                        structure = structure.Concat(GetStructureOfCSV(csv, values)).ToList();
                    }
                    else
                    {
                        Debug.LogWarning(string.Format("Cannot detect dynamic type since no data of {0} is available. Format of CSV will be incorrect!", thisVar.GetUnderlyingType()));
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        return structure;
    }

    private static void WriteHeaderToCSV<T>(CsvWriter csv, List<T> data, string prefix = "")
    {
        MemberInfo[] members = GetMemberInfo(data[0].GetType());

        foreach (var thisVar in members)
        {
            if (thisVar.GetUnderlyingType() == typeof(Dictionary<string, object>) || thisVar.GetUnderlyingType() == typeof(Dictionary<string, double>))
            {
                switch(thisVar.GetUnderlyingType())
                {
                    case Type t when t == typeof(Dictionary<string, object>):
                        WriteDictionaryToHeader<T, object>(csv, thisVar, data);
                        break;
                    case Type t when t == typeof(Dictionary<string, double>):
                        WriteDictionaryToHeader<T, double>(csv, thisVar, data);
                        break;
                }
            }
            else if (IsList(thisVar.GetUnderlyingType()))
            {
                List<T> records = data.Where(x => thisVar.GetValue(x) != null).ToList();
                List<dynamic> values = records.Select(x => thisVar.GetValue(x)).ToList();

                for(int i = 0; i < values[0].Count; i++)
                {
                    WriteHeaderToCSV(csv, new List<dynamic>() {values[0][i]}, string.Format("{0}[{1}]", thisVar.Name, i));
                }
            }
            else if (!HasCSVHelperBuiltInConverter(thisVar.GetUnderlyingType()))
            {
                try
                {
                    List<T> records = data.Where(x => thisVar.GetValue(x) != null).ToList();
                    List<object> values = records.Select(x => thisVar.GetValue(x)).ToList();
                    if (values.Count > 0)
                    {
                        WriteHeaderToCSV(csv, values, thisVar.Name);
                    }
                }
                catch (InvalidOperationException)
                {
                    csv.WriteField(thisVar.Name);
                }
            }
            else
            {
                if (prefix != "" && prefix != null)
                {
                    csv.WriteField(string.Format("{0}_{1}", prefix, thisVar.Name));
                }
                else
                {
                    csv.WriteField(thisVar.Name);
                }
            }
        }
    }

    private static bool IsList(Type t)
    {
        if (t == null) return false;
        return t.IsGenericType &&
               t.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
    }

    private static void WriteDictionaryToHeader<T1, T2>(CsvWriter csv, MemberInfo thisVar, List<T1> data)
    {
        List<T1> records = data.Where(x => thisVar.GetValue(x) != null).ToList();
        Dictionary<string, T2> value = records.Select(x => thisVar.GetValue(x)).First() as Dictionary<string, T2>;

        foreach (var key in value.Keys)
        {
            csv.WriteField(key);
        }
    }

    private static void WriteRecordToCSV(CsvWriter csv, object record, List<Type> structure)
    {
        MemberInfo[] members = GetMemberInfo(record.GetType());
        List<Type> local_structure = new List<Type>(structure);

        local_structure.RemoveAt(0);

        foreach (var thisVar in members)
        {
            if (thisVar.GetUnderlyingType() == typeof(Dictionary<string, object>))
            {
                WriteDictionaryToCSV(csv, thisVar, record);
            }
            else if (!HasCSVHelperBuiltInConverter(thisVar.GetUnderlyingType()))
            {
                if(thisVar.GetValue(record) != null)
                {
                    WriteRecordToCSV(csv, thisVar.GetValue(record), local_structure);
                }
                else
                {
                    object instance = Activator.CreateInstance(local_structure.First());
                    WriteRecordToCSV(csv, instance, local_structure);
                }
            }
            else if (IsList(thisVar.GetUnderlyingType()))
            {
                List<dynamic> values = ((IEnumerable<dynamic>)thisVar.GetValue(record)).ToList();

                foreach (dynamic val in values)
                {
                    local_structure.Add(val.GetType());
                    WriteRecordToCSV(csv, val, local_structure);
                }
            }
            else
            {
                csv.WriteField(thisVar.GetValue(record));
            }
        }
    }

    private static void WriteDictionaryToCSV(CsvWriter csv, MemberInfo thisVar, object data)
    {
        Dictionary<string, object> value = thisVar.GetValue(data) as Dictionary<string, object>;

        foreach (var val in value.Values)
        {
            csv.WriteField(val);
        }
    }

    private static bool HasCSVHelperBuiltInConverter(Type type)
    {
        return TypeDescriptor.GetConverter(type).GetType() != typeof(ReferenceConverter);
    }

    private static MemberInfo[] GetMemberInfo(Type type)
    {
        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;

        bool hasMeasureAttribute = HasMeasureAttribute(type);

        MemberInfo[] members = type.GetInterfaces().SelectMany(x => x.GetMemberInfos(bindingFlags)).ToArray();
        members = members.Concat(type.GetMemberInfos(bindingFlags)).ToArray();

        if (hasMeasureAttribute)
        {
            MemberInfo[] membersWihtMeasureAttribute;
            membersWihtMeasureAttribute = members.Where(x => x.GetCustomAttribute(typeof(MeasureAttribute)) != null).ToArray();

            return membersWihtMeasureAttribute;
        }

        return members;
    }

    private static bool HasMeasureAttribute(Type type)
    {
        foreach (Type t in type.GetInterfaces())
        {
            if (t.GetCustomAttribute(typeof(MeasureAttribute)) != null)
            {
                return true;
            }
        }

        return type.GetCustomAttribute(typeof(MeasureAttribute)) != null;
    }

    private static string GetTaskString(Hyperparameters hyperparameters)
    {
        string taskString = "";

        var query = hyperparameters.tasks.ToList()
            .GroupBy(s => s)
            .Select(g => new { Name = g.Key, Count = g.Count() });

        foreach (var result in query)
        {
            taskString += ShortenString(result.Name, 20).ToLower() + result.Count;
        }

        return taskString;
    }

    private static string FormatName(string prefix)
    {
        // Split camel case and join with space for the prefix
        return Regex.Replace(prefix, @"(\p{Lu})", " $1").Trim();
    }

    private static string GetDataPath(string filename, SupervisorSettings supervisorSettings, Hyperparameters hyperparameters, Func<string, string, int[], SupervisorSettings, string, string, string> getString, string name = "", int[] dimensions = null)
    {
        if (filename is null || filename == "")
        {
            filename = GenerateBehavioralFilename();
        }

        string extension = Path.GetExtension(filename);
        string[] parts = filename.Split("_b");

        string path = Path.Combine(GetScoreDataPath(), GetScoreString(supervisorSettings, hyperparameters));

        string id = Path.ChangeExtension(parts[0], null);

        return getString(path, id, dimensions, supervisorSettings, extension, name);
    }

    private static string GetDataPath(string filename, int[]  dimensions, Func<string, string, int[], string, string> getString)
    {
        if (filename is null || filename == "")
        {
            filename = GenerateBehavioralFilename();
        }

        string extension = Path.GetExtension(filename);
        string[] parts = filename.Split("_b");

        string path = GetScoreDataPath();

        string id = Path.ChangeExtension(parts[0], null);

        return getString(path, id, dimensions, extension);
    }

    private static string ConvertRawPathToDataPath(string path, int[]  dimensions, SupervisorSettings supervisorSettings, Func<string, string, int[], SupervisorSettings, string, string, string> getString, string name = "")
    {
        string scorePath = Path.GetDirectoryName(path);
        string filename = Path.GetFileName(path);

        filename = filename.Replace("raw", "");
        string id = Path.ChangeExtension(filename, null);

        string dataPath = getString(scorePath, id, dimensions, supervisorSettings, ".json", name);

        if (File.Exists(dataPath))
        {
            string extension = Path.GetExtension(dataPath);
            string dataPathold = dataPath;

            dataPath = Path.ChangeExtension(dataPath, null);
            dataPath = string.Format("{0}conv{1}", dataPath, extension);

            Debug.Log(string.Format("Path {0} already exists, therefore instead converting path to {1}", dataPathold, dataPath));
        }

        Debug.Log(string.Format("transform {0} to {1}", path, dataPath));

        return dataPath;
    }

    private static string GetBehavioralDataString(string path, string id, int[]  dimensions, SupervisorSettings supervisorSettings, string extension, string name = "")
    {
        return Path.Combine(path, String.Format("{0}_b{1}{2}{3}", id, name, GetDimensionString(dimensions), extension));
    }

    private static string GetReactionTimeDataString(string path, string id, int[]  dimensions, SupervisorSettings supervisorSettings, string extension, string name)
    {
        return Path.Combine(path, String.Format("{0}_rt_{1}{2}{3}", id, name, GetDimensionString(dimensions), extension));
    }

    private static string GetBehavioralDataWithoutConfigString(string path, string id, int[] dimensions, SupervisorSettings supervisorSettings, string extension, string name = "")
    {
        return Path.Combine(path, String.Format("{0}_b{1}", id, extension));
    }

    private static string GetReactionTimeDataWithoutConfigString(string path, string id, int[]  dimensions, SupervisorSettings supervisorSettings, string extension, string name = "")
    {
        return Path.Combine(path, String.Format("{0}_rt{1}", id, extension));
    }

    private static string GetRawBehavioralDataString(string path, string id, int[]  dimensions, SupervisorSettings supervisorSettings, string extension, string name = "")
    {
        return Path.Combine(path, String.Format("{0}raw{1}", id, ".csv"));
    }

    private static string GetScoreDataString(string path, string id, int[]  dimensions, SupervisorSettings supervisorSettings, string extension, string name = "")
    {
        return Path.Combine(path, String.Format("scores_{0}{1}", id, ".csv"));
    }

    private static string GetDimensionString(int[] dimensions)
    {
        string dimensionString = "";

        for (int i = 0; i < dimensions.Length; i++)
        {
            dimensionString += string.Format("D{0}", dimensions[i]);
        }

        return dimensionString;
    }
}
