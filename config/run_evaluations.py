#!/usr/bin/env python3

import argparse
from pathlib import Path
import subprocess
from unity_helper import call_function
import time
from textUtils import printElapsedTime
from shutil import copytree, ignore_patterns
from generate_config_files import generate_Config_files_directory
import os
from workload import is_cpu_overloaded
import json


def Main():
    parser = argparse.ArgumentParser()
    parser.add_argument("ball_agent_models_dir_name", help="Name of models directory in Assets/Models path which should be evaluated.", type=str)
    parser.add_argument("evaluation_config_file", help="JSON config file with the evaluation parameters.", type=str)
    parser.add_argument("comparison_file_name", help="Data based on which the models should be compared/evaluated.", nargs='?', type=str)
    parser.add_argument("--environment_config_list_file", "-e",
        help="Config file in JSON format consisting of lists of the different parameters to generate the environment config files. Replaces config" 
            +"files of ball_agent_models_dir_name if given.", type=str)
    parser.add_argument("--number_of_environments", "-n", 
        help="If this value is passed, only a subset of the possible parameter combinations is generated. The subset consists of combinations with "
            +"a maximal distance. Will be ignored if environment_config_list_file is not given.", default=0, type=int)
    parser.add_argument('--nobuild', '-nb',  help="The given directory name is used for the evaluation. The environments are not build.", type=str)
    parser.add_argument("--start_index", "-i", 
    help="Is ignored if number_of_environments is not given. Returns the environments with the highest distance at starting index Start_index." 
    + "For instance if number_of_environments = 10 and start_index=5 then the first 5 environments with the largest distance are ignored and the" 
    + "next 10 environments are returned.", default=0, type=int)
    parser.add_argument("--copy_raw_data", "-c", help="If given also the raw data is copied to the Scores directory.", action='store_true')
    parser.add_argument("--target_dir", "-t", help="If given the results are saved to the target- instead of the session directory.", type=str)

    args = parser.parse_args()

    startTime = time.time()

    if (args.comparison_file_name is None):
        comparison_file_name = ""
    else:
        comparison_file_name = args.comparison_file_name

    if args.nobuild is None:
        if (args.environment_config_list_file is None):
            session_dir = __build_all_evaluation_environments(ball_agent_models_dir_name = args.ball_agent_models_dir_name,
                                                              evaluation_config_file = args.evaluation_config_file, 
                                                              comparison_file_name = comparison_file_name)
        else:
            session_dir = __build_all_evaluation_environments_with_config_list(ball_agent_models_dir_name = args.ball_agent_models_dir_name,
                                                                               evaluation_config_file = args.evaluation_config_file,
                                                                               comparison_file_name = comparison_file_name,
                                                                               environment_config_list_file = args.environment_config_list_file, 
                                                                               number_of_environments = args.number_of_environments,
                                                                               start_index = args.start_index)
    else:
        session_dir = args.nobuild
    
    __run_environments(session_dir)

    __copy_evaluation_results_to_score_folder(session_dir, args.copy_raw_data, args.target_dir)

    endTime = time.time()

    printElapsedTime(startTime, endTime, "run_evaluations")



def __build_all_evaluation_environments(ball_agent_models_dir_name, evaluation_config_file, comparison_file_name):
    model_dir = Path('..', 'Assets', 'Models', ball_agent_models_dir_name)

    sub_dirs = [d for d in model_dir.glob('*') if d.is_dir()]

    count = 1

    for dir in sub_dirs:
        print("building step {}/{}...".format(count, len(sub_dirs)))

        try:
            json_path = list(dir.glob("*.json"))[0]
            #removes ".." from path
            json_path = Path(*json_path.parts[1:])

            asset_filename = list(dir.glob("3DBall*.asset"))[0].name

            call_function("BuildScript.BuildEvaluationEnvironment",
                ball_agent_models_dir_name, #e.g. Session1
                json_path, #e.g. Assets\Models\Session1\model_name\envConf.json
                Path(ball_agent_models_dir_name, dir.name, asset_filename), #e.g. Session1\model_name\model.asset
                Path("config", evaluation_config_file), 
                comparison_file_name,
                logFile=Path("..", "Logs", "Builds", ball_agent_models_dir_name, dir.name,"LogFileBuild.txt"))

        except IndexError:
            print("Could not find all files for {}.".format(dir))

        count += 1

    return ball_agent_models_dir_name



def __build_all_evaluation_environments_with_config_list(ball_agent_models_dir_name, evaluation_config_file, comparison_file_name, environment_config_list_file, number_of_environments, start_index=0):
    total_number = generate_Config_files_directory(environment_config_list_file, number_of_environments, False, start_index)

    conf_files = [f for f in Path("envConf").glob('*.json') if f.is_file()]

    count = 1

    for conf_file in conf_files:
        print("building step {}/{}...".format(count, total_number))

        model_dir = Path('..', 'Assets', 'Models', ball_agent_models_dir_name)

        sub_dirs = [d for d in model_dir.glob('*') if d.is_dir()]

        dir = __get_model_for_decision_period(sub_dirs, conf_file)

        onnx_filename = list(dir.glob("3DBall*.onnx"))[0].name
        
        returncode = call_function("BuildScript.BuildEvaluationEnvironment",
                                   Path(environment_config_list_file).stem, #e.g. Session1
                                   Path("config", conf_file), #e.g. config\envConf\EnvAfD3RtAhcTfTBtN1000NS1000S0.05SM0.01U0.1O0.9RT0.5.json
                                   Path(ball_agent_models_dir_name, dir.name, onnx_filename), #e.g. Session1\model_name\model.onnx
                                   Path("config", evaluation_config_file), 
                                   comparison_file_name,
                                   logFile=Path("..", "Logs", "Builds", ball_agent_models_dir_name, dir.name,"LogFileBuild.txt"))
        
        if returncode != 0:
            break

        count += 1

    return Path(environment_config_list_file).stem


def __get_model_for_decision_period(sub_dirs, conf_file):
    decision_period = __get_decision_period(conf_file)

    for dir in sub_dirs:
        if "DP{}R".format(decision_period) in str(dir):
            return dir
        
    raise FileNotFoundError("Could not find file with decision period of {}.".format(decision_period))
    

def __get_decision_period(conf_file):
    f = open(conf_file)
    data = json.load(f)

    return data['decisionPeriod']


def __run_environments(ball_agent_models_dir_name):
    build_dir = Path('..', 'Build', ball_agent_models_dir_name)

    count = 0

    sub_build_dirs = [d for d in build_dir.glob('*') if d.is_dir()]

    procs = []
    active_instances = 0

    for dir in sub_build_dirs:
        try:
            if not __environment_is_evaluated(dir):
                procs.append(subprocess.Popen(str(Path(dir, "SupervisorML.exe"))))
                active_instances += 1
                print("Evaluating {}...".format(str(Path(dir))))
            else:
                print("Skip evaluation, model was already evaluated!")
        except FileNotFoundError as e:
            print("Could not find file: {}".format(str(e)))

        count += 1
        print("evaluation step {}/{}...".format(count, len(sub_build_dirs)))

        while is_cpu_overloaded(5):
            for p in procs:
                if p.poll() != None:
                    active_instances -= 1
                    procs.remove(p)

    while procs:
        for p in procs:
            if p.poll() != None:
                active_instances -= 1
                procs.remove(p)


def __environment_is_evaluated(dir):
    path = Path(dir, "SupervisorML_Data", "Scores")

    sub_dirs = [d for d in path.glob('*') if d.is_dir()]

    for dir in sub_dirs:
        file_nr = len([d for d in dir.glob('*.json')])
        
        if file_nr == 4:
            return True
            
    return False


def __copy_evaluation_results_to_score_folder(ball_agent_models_dir_name, copy_raw_data, target_dir):
    build_dir = Path('..', 'Build', ball_agent_models_dir_name)
    sub_build_dirs = [d for d in build_dir.glob('*') if d.is_dir()]

    if target_dir is None:
        target_dir = ball_agent_models_dir_name

    for dir in sub_build_dirs:
        sub_scores = Path(dir, "SupervisorML_Data", "Scores")
        target = Path('..', 'Scores', "Evaluations", target_dir, dir.name)

        print("Copy results {} to {}".format(sub_scores, target))
        print('\033[1A', end='\x1b[2K')

        if copy_raw_data:
            copytree(__get_max_path(sub_scores), __get_max_path(target), dirs_exist_ok=True, ignore=ignore_patterns('*switching_scores*'))
        else:
            copytree(__get_max_path(sub_scores), __get_max_path(target), dirs_exist_ok=True, ignore=ignore_patterns('*switching_scores*', '*raw*'))


def __get_max_path(path):
    prefix = "\\\\?\\"

    return prefix + os.path.abspath(path)



if __name__ == '__main__':
    Main()