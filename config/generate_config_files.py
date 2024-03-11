#!/usr/bin/env python3

import argparse
import itertools
import json
import copy
import numbers
import numpy as np
import shutil
from pathlib import Path

def Main():
    parser = argparse.ArgumentParser()
    parser.add_argument("config_file", 
    help="Config file in JSON format consisting of lists of the different parameters to generate the environment config files.", type=str)
    parser.add_argument("--number_of_environments", "-n", 
    help="If this value is passed, only a subset of the possible parameter combinations is generated. The subset consists of combinations with a" 
    + "maximal distance.", default=0, type=int)
    parser.add_argument("--verbose", "-v", 
    help="Prints the iterated distance table. Therefore a higher distance is indicating that the value was added later. The first added value is" 
    + "marked with a distance of -1.", action='store_true')
    parser.add_argument("--start_index", "-i", 
    help="Is ignored if number_of_environments is not given. Returns the environments with the highest distance at starting index Start_index." 
    + "For instance if number_of_environments = 10 and start_index=5 then the first 5 environments with the largest distance are ignored and the" 
    + "next 10 environments are returned.", default=0, type=int)

    args = parser.parse_args()

    f = open(args.config_file)

    data = json.load(f)

    print("Generate config files...")

    if args.number_of_environments <= 0:
        generate_config_files(data)
    else:
        generate_opt_config_files(data, args.number_of_environments, args.verbose)

    print("Config file generation completed!")


def generate_Config_files_directory(environment_config_list_file, number_of_environments, verbose, start_index = 0):
    """Generates a directory called \"envConf\" with the different config files based on the given file \"environment_config_list_file\"."""

    f = open(environment_config_list_file)
    data = json.load(f)

    try:
        shutil.rmtree(Path("envConf"))
    except FileNotFoundError:
        print("Could not delete directory {}.".format(Path("envConf")))

    Path("envConf").mkdir()

    print("Generate config files...")

    if number_of_environments <= 0:
        total_number = generate_config_files(data)
    else:
        total_number = generate_opt_config_files(data, number_of_environments, verbose, start_index)

    print("Config file generation completed!")

    return total_number


def generate_config_files(data):
    dummyBall3DAgentHumanCognitionSettings = {
        'numberOfBins' : 0,
        'showBeliefState' : False,
        'numberOfSamples' : 0,
        'sigma' : 0,
        'sigmaMean' : 0,
        'updatePeriode' : 0,
        'observationProbability' : 0,
        'constantReactionTime' : 0,
        'oldDistributionPersistenceTime' : 0
    }
    
    paramsList = __generate_permutations(data)


    for param in paramsList:
        if param['hyperparameters']['agentChoice'] == 'Ball3DAgentOptimal':
            param['ball3DAgentHumanCognitionSettings'] = dummyBall3DAgentHumanCognitionSettings

    l = __save_params_to_file(paramsList)

    return l


def generate_opt_config_files(data, number_of_environments, verbose, start_index = 0):
    params_list, distances = __optimize_parameters(data, number_of_environments, start_index)
    params_list_full = __generate_permutations(data)

    if verbose:
        for i, v in enumerate(params_list_full):
            if distances[i] != -9999999:
                print("Distance for {}: {}".format(__generateFileName(v), distances[i]))


    l = __save_params_to_file(params_list)

    return l


def __save_params_to_file(paramsList):
    for param in paramsList:

        filename = __generateFileName(param)
        
        with open("envConf\\" + filename, 'w') as fp:
            json.dump(param, fp)

    return len(paramsList)


def __flatten_dict(d, parent_key='', sep='_'):
    items = []
    for k, v in d.items():
        new_key = f"{parent_key}{sep}{k}" if parent_key else k
        if isinstance(v, dict):
            items.extend(__flatten_dict(v, new_key, sep=sep).items())
        else:
            items.append((new_key, v))
    return dict(items)


def __generate_permutations(input_json):
    flattened_json = __flatten_dict(input_json)
    keys = flattened_json.keys()
    values_lists = [flattened_json[key] for key in keys]

    permutations = list(itertools.product(*values_lists))

    result_json_list = []
    for perm in permutations:
        result_json = dict(zip(keys, perm))
        result_json = __unflatten_dict(result_json)
        result_json_list.append(result_json)

    return result_json_list


def convert_to_list_of_lists(json_list):
    list_of_lists = []
    for json_data in json_list:
        flattened_data = __flatten_dict(json_data, sep='_')
        list_of_lists.append([flattened_data[key] for key in sorted(flattened_data.keys())])
    return list_of_lists


def __unflatten_dict(d, sep='_'):
    result = {}
    for key, value in d.items():
        keys = key.split(sep)
        temp_dict = result
        for k in keys[:-1]:
            temp_dict = temp_dict.setdefault(k, {})
        temp_dict[keys[-1]] = value
    return result


def __build_para_combination_list(parameters):
    permutations = __generate_permutations(parameters)
    
    return convert_to_list_of_lists(permutations)


def __optimize_parameters(parameters_dict, number_of_environments, start_index = 0):
    result_list = []
    index_chosen_candidates = []

    parameters_list =  __build_para_combination_list(parameters_dict)

    index_min = __get_index_of_min(parameters_dict)

    result_list.append(parameters_list[index_min])
    index_chosen_candidates.append(index_min)
    iterative_distances = [-9999999]*len(parameters_list)
    iterative_distances[index_min] = -1

    total_possible_env = len(parameters_list)

    num_env = number_of_environments - 1 + start_index if (number_of_environments - 1 + start_index < total_possible_env) else total_possible_env

    for _ in range(num_env):
        (index_max_distance, max_distance) = __get_distance_index_of_next_chosen_candidate(parameters_list, index_chosen_candidates)

        iterative_distances[index_max_distance] = max_distance

        result_list.append(parameters_list[index_max_distance])
        index_chosen_candidates.append(index_max_distance)

    del result_list[:start_index]

    original_structure = sorted(__flatten_dict(parameters_dict, sep='_').keys())
    return (__convert_to_dict_list(result_list, original_structure), iterative_distances)


def __get_distance_index_of_next_chosen_candidate(parameters_list, index_chosen_candidates):
    distances = []

    for i, parameters_candidate in enumerate(parameters_list):
        if i not in index_chosen_candidates:
            distance = __calculate_accumulated_distance_between_canditate_and_chosen_canditates(parameters_candidate, parameters_list, index_chosen_candidates)

            distances.append(distance)
        else:
            #0 distance must be added for every index otherwise the index would not be useable
            distances.append(-9999999)

    return (distances.index(max(distances)), max(distances))


def __calculate_accumulated_distance_between_canditate_and_chosen_canditates(parameters_candidate, parameters_list, index_chosen_candidates):
    distance = 0

    for index_chosen_candidate in index_chosen_candidates:
        for x, parameter in enumerate(parameters_list[index_chosen_candidate]):
            if not isinstance(parameter, str):
                if parameter == parameters_candidate[x]:
                    distance -= 1000

    return distance


def __convert_to_dict_list(list_of_lists, original_structure):
    json_list = []
    for perm_list in list_of_lists:
        reconstructed_dict = dict(zip(original_structure, perm_list))
        reconstructed_dict = __unflatten_dict(reconstructed_dict)
        json_list.append(reconstructed_dict)
    return json_list


def __get_index_of_min(list):
    tmp = []

    for v in list:
        tmp.append(sum(i for i in v if not isinstance(i, str)))

    return tmp.index(max(tmp))


def __normalize_list_values(list_value):
    result = []

    if isinstance(list_value, dict):
        result = {k: __normalize_list_values(v) for k, v in list_value.items()}
    elif isinstance(list_value[0], numbers.Number):
        if max(np.abs(list_value)) != 0:
            result = [float(abs(v))/max(np.abs(list_value)) for v in list_value]
        else:
            result = [0]
    else:
        for v in list_value:
            if v == 'Ball3DAgentHumanCognition':
                result.append(-10)
            elif v == 'Ball3DAgentHumanCognitionSingleProbabilityDistribution':
                result.append(10)
            else:
                result.append(v)

    return result


def __generateFileName(param):
    ac = 'e'

    if param['hyperparameters']['agentChoice'] == 'Ball3DAgentHumanCognition':
        ac = 'hc'
    elif param['hyperparameters']['agentChoice'] == 'Ball3DAgentHumanCognitionSingleProbabilityDistribution':
        ac = 'hcs'
    elif param['hyperparameters']['agentChoice'] == 'Ball3DAgentOptimal':
        ac = 'o'

    try:
        full_vision = param['ball3DAgentHumanCognitionSettings']['fullVision']
    except:
        full_vision = False


    if ac == 'o' or full_vision:
        filename = "EnvA{}DP{}R{}A{}T{}TB{}.json".format(                   __get_identifier(param, "['autonomous']"),
                                                                            __get_identifier(param, "['decisionPeriod']"),
                                                                            __get_identifier(param, "['resetPlatformToIdentity']"),
                                                                            ac,
                                                                            __get_identifier(param, "['trainSupervisor']"), 
                                                                            __get_identifier(param, "['trainBallAgent']"))
    else:
        filename = "EnvF{}A{}DP{}R{}A{}T{}TB{}N{}NS{}S{}SM{}U{}O{}RT{}OD{}.json".format(__get_identifier(param, "['ball3DAgentHumanCognitionSettings']['useFocusAgent']"),
                                                                            __get_identifier(param, "['autonomous']"),
                                                                            __get_identifier(param, "['decisionPeriod']"),
                                                                            __get_identifier(param, "['resetPlatformToIdentity']"),
                                                                            ac,
                                                                            __get_identifier(param, "['trainSupervisor']"), 
                                                                            __get_identifier(param, "['trainBallAgent']"), 
                                                                            __get_identifier(param, "['ball3DAgentHumanCognitionSettings']['numberOfBins']"), 
                                                                            __get_identifier(param, "['ball3DAgentHumanCognitionSettings']['numberOfSamples']"), 
                                                                            __get_identifier(param, "['ball3DAgentHumanCognitionSettings']['sigma']"), 
                                                                            __get_identifier(param, "['ball3DAgentHumanCognitionSettings']['sigmaMean']"), 
                                                                            __get_identifier(param, "['ball3DAgentHumanCognitionSettings']['updatePeriode']"), 
                                                                            __get_identifier(param, "['ball3DAgentHumanCognitionSettings']['observationProbability']"),
                                                                            __get_identifier(param, "['ball3DAgentHumanCognitionSettings']['constantReactionTime']"),
                                                                            __get_identifier(param, "['ball3DAgentHumanCognitionSettings']['oldDistributionPersistenceTime']"))

    return filename


def __get_identifier(param, key):
    try:
        value = eval("param"+key)

        if isinstance(value, bool):
            return str(value)[0].lower()
        if isinstance(value, numbers.Number):
            return value
        if isinstance(value, str):
            return value[0]
    except:
        return "_"


if __name__ == '__main__':
    Main()