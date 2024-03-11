from pathlib import Path
import os


def printElapsedTime(start, end, functionName):
    hours, rem = divmod(end-start, 3600)
    minutes, seconds = divmod(rem, 60)
    
    print("Elapsed time during computation of \"{}\": {:0>2}:{:0>2}:{:05.2f}".format(functionName, int(hours), int(minutes), seconds))


def getFileNamesForConfigString(rawDataFileName, configString):
    fileName = rawDataFileName.replace('raw.csv', '')
    parts = configString.split('NT')

    behaviouralDataFileName = "{0}{1}.json".format(fileName, parts[0])
    reactionTimeFileName = "{0}_rt_{1}{2}.json".format(fileName, 'NT', parts[1])

    return (behaviouralDataFileName, reactionTimeFileName)

def get_model_name(model_config_file, environment_config_file):
    return str(Path(model_config_file).with_suffix('').name) + str(Path(environment_config_file).with_suffix('').name)

def get_model_path(session_dir, model_name):
    return Path("..", "Assets", "Models", session_dir, model_name)

def remove_left_directory(path):
    directories = str(path).split(os.path.sep)

    if len(directories) >= 2:
        remaining_directories = directories[1:]
        modified_path = os.path.sep.join(remaining_directories)

        return modified_path
    else:
        return path