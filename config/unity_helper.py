import subprocess
from pathlib import Path
import sys
import os
import io
import textUtils
import workload


def call_function(function_name, *args, logFile=""):

    strArgs = []

    for arg in args:
        strArgs.append(str(arg))

    print("Executing Unity function: {}".format(function_name) + "\nArgs:\t" + '\n\t'.join([elem for elem in strArgs]))
    if logFile != "":
        print("Writing log files to {}.".format(logFile))

    command = ["Unity", "-quit", "-batchmode", "-nographics", "-executeMethod", function_name]
    command.extend(strArgs)
    command.extend(["-logFile", str(logFile)])

    try:
        result = subprocess.run(command, 
                                text=True, 
                                shell=True,
                                stdout=sys.stdout)
    except io.UnsupportedOperation:
        result = subprocess.run(command, 
                                text=True, 
                                shell=True,
                                stdout=subprocess.PIPE)
        
        print(result.stdout)

    return result.returncode


def start_training(model_config_file, session_dir, model_name, resume=False):

    print("Start training:\n" +
          "\t model_config_file: {} \n".format(str(model_config_file)) +
          "\t session_dir: {} \n".format(str(session_dir)) +
          "\t model_name: {} \n".format(str(model_name)))

    command = ["mlagents-learn",
                str(model_config_file),
                "--run-id=" + str(Path("..", "..", "Assets", "Models", session_dir, model_name)), 
                "--env=" + str(Path("..", "Build", "TrainingEnvironment")), 
                "--num-envs={} ".format(workload.get_number_of_environments_for_cpu_workload())]
    
    if resume:
        command.append("--resume")

    result = subprocess.run(command, 
                            text=True, 
                            shell=True,
                            stdout=sys.stdout)

    return result.returncode


def convertBehaviourMeasurementOfSession(sessionPath, scoreString, configString, supervisorSettingsPath, behavioralDataCollectionSettingsPath):
    sub_dirs = [d for d in sessionPath.glob('*') if d.is_dir()]

    totalEnv = len(sub_dirs)
    count = 0

    print("Total number of environments: {0}".format(totalEnv))

    for dir in sub_dirs:
        scorePath = Path(dir, scoreString)
        rawDataPath = [d for d in scorePath.glob('*raw*')][0]
        rawDataFileName = os.path.basename(rawDataPath)

        (behaviouralDataFileName, reactionTimeFileName) = textUtils.getFileNamesForConfigString(rawDataFileName, configString)

        if (not os.path.isfile(Path(scorePath, behaviouralDataFileName)) or not os.path.isfile(Path(scorePath, reactionTimeFileName))):
            call_function("API.ConvertRawToBinData", 
                          supervisorSettingsPath, 
                          behavioralDataCollectionSettingsPath, 
                          rawDataPath, 
                          logFile=Path("..", "Logs", "LogFileConvertRawToBinData.txt"))
        else:
            print("Data already converted for environment {0}. Skip conversion...".format(dir))

        count += 1
        print("{0}/{1} environments converted".format(count, totalEnv))