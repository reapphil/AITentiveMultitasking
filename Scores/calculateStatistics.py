#!/usr/bin/env python3

import argparse
import pandas as pd
import os
from pathlib import Path, PurePath
import numpy as np
from evaluation import getEvaluationResult
from evaluation_3d import getNumpy3DArrays
import json


def Main():
    parser = argparse.ArgumentParser()
    parser.add_argument("behaviouralData_file", help="File with the behavioural data to process", type=str)
    parser.add_argument("reactionTimes_file", help="File with the reaction times to process", type=str)

    args = parser.parse_args()

    currentPath = Path(os.getcwd())
    behaviouralDataPath = PurePath(currentPath, args.behaviouralData_file)
    reactionTimesPath = PurePath(currentPath, args.reactionTimes_file)

    _, file_extension = os.path.splitext(reactionTimesPath)

    description = Path(behaviouralDataPath).resolve().stem

    final_directory = PurePath(currentPath, 'Behavioural Statistics', description)

    print('Calculation of statistics...')
    if file_extension == '.csv':
        result = getEvaluationResult(reactionTimesPath, behaviouralDataPath)

        if not os.path.exists(final_directory):
            os.makedirs(final_directory)

        for key in result:
            file_path = PurePath(final_directory, key + ' ' + description[:15] + '.csv') if len(description) > 15 else PurePath(final_directory, key + ' ' + description + '.csv')

            #numpy arrays are saved to CSV without commas between the values of the array
            df = result[key].applymap(lambda x: npArrayToList(x))
            df.to_csv(file_path)

    else:
        result = getEvaluationResult(reactionTimesPath, behaviouralDataPath, getData=getNumpy3DArrays)

        if not os.path.exists(final_directory):
            os.makedirs(final_directory)

        for key in result:
            file_path = PurePath(final_directory, key + ' ' + description[:15] + '.json') if len(description) > 15 else PurePath(final_directory, key + ' ' + description + '.json')

            data = [[{k:npArrayToList(v) for (k,v) in dict.items()} for dict in dicts] for dicts in result[key]]

            with open(file_path, 'w') as f:
                json.dump(data, f)



    

def npArrayToList(value):
    if isinstance(value, np.ndarray):
        #there is a strange rounding error when converting np.array to list
        r = []
        for num in list(value):
            if isinstance(num, np.int32):
                r.append(int(num))
            else:
                r.append(num)
        
        return r
    else:
        return value


if __name__ == '__main__':
    Main()