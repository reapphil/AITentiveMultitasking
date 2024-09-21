using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

public class BehaviourMeasurementConverterAPI
{
    public static void ConvertRawToBinData(string settingsPath, string behavioralDataCollectionSettingsPath, string rawDataPath)
    {
        Dictionary<Type, ISettings> settings = SettingsLoader.LoadSettings(settingsPath);
        SupervisorSettings supervisorSettings = (SupervisorSettings)settings[typeof(SupervisorSettings)];
        Hyperparameters hyperparameters = (Hyperparameters)settings[typeof(Hyperparameters)];

        BehavioralDataCollectionSettings behavioralDataCollectionSettings = Util.ImportJson<BehavioralDataCollectionSettings>(behavioralDataCollectionSettingsPath);

        BehaviorMeasurementConverter.ConvertRawToBinData(supervisorSettings, hyperparameters, behavioralDataCollectionSettings, rawDataPath);
    }
}
