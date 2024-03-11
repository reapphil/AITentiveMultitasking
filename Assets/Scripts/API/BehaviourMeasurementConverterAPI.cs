using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

public class BehaviourMeasurementConverterAPI
{
    public static void ConvertRawToBinData(string supervisorSettingsPath, string behavioralDataCollectionSettingsPath, string rawDataPath)
    {
        List<ISettings> settings = ConfigVersioning.UnifySettings(supervisorSettingsPath, typeof(SupervisorSettings));
        SupervisorSettings supervisorSettings = (SupervisorSettings)settings[0];
        BalancingTaskSettings balancingTaskSettings = (BalancingTaskSettings)settings[1];

        BehavioralDataCollectionSettings behavioralDataCollectionSettings = Util.ImportJson<BehavioralDataCollectionSettings>(behavioralDataCollectionSettingsPath);

        BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData(supervisorSettings, balancingTaskSettings, behavioralDataCollectionSettings, rawDataPath);
    }
}
