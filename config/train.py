#!/usr/bin/env python3

import argparse
from pathlib import Path
import shutil
import unity_helper
import time
import textUtils
import os

import numpy as np
np.float = float


def Main():
    parser = argparse.ArgumentParser()
    parser.add_argument("model_config_file", help="Config file in YAML format of the hyperparameters for model training.", type=str)
    parser.add_argument("environment_config_file", help="Config file in JSON format consisting of the different parameters", type=str)
    parser.add_argument("session_dir", help="Directory where the models are saved.", type=str)

    args = parser.parse_args()

    startTime = time.time()

    train_model(model_config_file = args.model_config_file, 
                environment_config_file = args.environment_config_file, 
                session_dir = args.session_dir)
    enrich_model(model_config_file = args.model_config_file, 
                environment_config_file = args.environment_config_file, 
                session_dir = args.session_dir)

    endTime = time.time()

    textUtils.printElapsedTime(startTime, endTime, "train")


def train_model(model_config_file, environment_config_file, session_dir):
    session_dir = __get_top_level(session_dir)

    model_name = textUtils.get_model_name(model_config_file = model_config_file, 
                                          environment_config_file = environment_config_file)
    model_path = textUtils.get_model_path(session_dir = session_dir, 
                                          model_name = model_name)

    resume = __check_for_existing_model(model_path)
    
    code = unity_helper.call_function("BuildScript.BuildTrainingEnvironment",
                                      Path("config", environment_config_file),
                                      logFile=Path("..", "Logs", "LogFileBuild.txt"))

    if code != 0:
        return

    unity_helper.start_training(model_config_file, session_dir, model_name, resume)


def enrich_model(model_config_file, environment_config_file, session_dir):
    session_dir = __get_top_level(session_dir)

    model_name = textUtils.get_model_name(model_config_file = model_config_file, 
                                          environment_config_file = environment_config_file)
    model_path = textUtils.get_model_path(session_dir = session_dir, 
                                          model_name = model_name)
    
    target_dir = Path(model_path, Path(environment_config_file).name)
    shutil.copyfile(environment_config_file, target_dir)

    code = unity_helper.call_function("PostProcessing.EnrichModels",
                                      textUtils.remove_left_directory(target_dir),
                                      logFile=Path("..", "Logs", "LogFilePostProcessing.txt"))

    if code != 0:
        return
    

def __check_for_existing_model(model_path):
    resume = False
    
    if Path.exists(model_path):
        print("There is already a model with the same id. Do you want to overwrite the model (O) or continue training [C])?")

        i = input()

        if  i == "o" or i == "O":
            print("Deleting of old session id...")
            shutil.rmtree(model_path)
            try:
                Path.unlink(Path(str(model_path) + '.meta'))
            except FileNotFoundError: 
                print("Could not find and delete {}.".format(Path(str(model_path) + '.meta')))
        else:
            resume = True

    return resume


def __rename_model(model_prefix, model_name, model_path):
    try:
        path = Path(model_path, "{}.onnx".format(model_prefix))
        new_path = path.replace(Path(path.parent, "{}{}.onnx".format(model_prefix, model_name)))
        print("File {} renamed to {}".format(path, new_path))
    except FileNotFoundError: 
        print("Could not find file {}.".format(path))


def __get_top_level(file_path):
    normalized_path = os.path.normpath(file_path)
    path_components = normalized_path.split(os.path.sep)
    top_level_directory = path_components[-1]
    
    return top_level_directory


if __name__ == '__main__':
    Main()