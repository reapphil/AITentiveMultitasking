#!/usr/bin/env python3

import argparse
from pathlib import Path
import train
import generate_config_files
import time
import textUtils


def Main():
    parser = argparse.ArgumentParser()
    parser.add_argument("model_config_file", help="Config file in YAML format of the hyperparameters for model training.", type=str)
    parser.add_argument("environment_config_list_file", help="Config file in JSON format consisting of lists of the different parameters to generate the environment config files.", type=str)
    parser.add_argument("--number_of_environments", "-n", help="If this value is passed, only a subset of the possible parameter combinations is generated. The subset consists of combinations with a maximal distance.", default=0, type=int)
    parser.add_argument("--verbose", "-v", help="Prints the iterated distance table. Therefore a higher distance is indicating that the value was added later. The first added value is marked with a distance of -1.", action='store_true')

    args = parser.parse_args()

    total_number = generate_config_files.generate_Config_files_directory(args.environment_config_list_file, args.number_of_environments, args.verbose)

    environment_config_files = [f for f in Path("envConf").glob('*.json') if f.is_file()]

    count = 1

    startTime = time.time()

    for environment_config_file in environment_config_files:
        print("training step {}/{}...".format(count, total_number))

        train.train_model(model_config_file = args.model_config_file, 
                          environment_config_file = environment_config_file, 
                          session_dir = Path(args.environment_config_list_file).with_suffix(''))
        train.enrich_model(model_config_file = args.model_config_file, 
                          environment_config_file = environment_config_file, 
                          session_dir = Path(args.environment_config_list_file).with_suffix(''))

        count += 1

    endTime = time.time()

    textUtils.printElapsedTime(startTime, endTime, "run_trainings")


if __name__ == '__main__':
    Main()