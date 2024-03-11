import pandas as pd
from pathlib import Path, PurePath
import numpy as np
import os
import json
import sys
import evaluation as ev
import util as u


def mapJSONList(list, func):
    if isinstance(list, dict):
        return {(int(k) if isinstance(k, str) and k.isdigit() else k):func(v) for (k,v) in list.items()}
    else:
        return [mapJSONList(e, func) for e in list]
    

#The Tuples of the JSON files are divided in separate numpy arrays based on the total count, the average value and the standard deviation.
def getNumpyArrays(path, indexCol):
    f = open(path)
    data = json.load(f)

    data = mapJSONList(data, parseValue)

    average = mapJSONList(data, ev.calculateAverage)
    sd = mapJSONList(data, ev.calculateStandardDeviation)
    N = mapJSONList(data, ev.getN)
    
    return (average, sd, N)


def getNumpyArraysInclSuspendedCount(path, indexCol):
    f = open(path)
    data = json.load(f)

    data = mapJSONList(data, parseValue)

    average = mapJSONList(data, ev.calculateAverage)
    sd = mapJSONList(data, ev.calculateStandardDeviation)
    N = mapJSONList(data, ev.getN)
    scAverage = mapJSONList(data, ev.calculateAverageSuspendedMeasurementCount)
    
    return (average, sd, N, scAverage)


def parseValue(value):
    try:
        return parseSingleTupleFormat(value)
    except KeyError:
        return parseTwoTupleFormat(value)
    

def parseSingleTupleFormat(value):
    if isinstance(value["Item2"], dict):
        d1 = value["Item2"]
        d2 = value["Item3"]

        return [int(value["Item1"]), parseSerializedVector3(d1), parseSerializedVector3(d2)]
    else:
        return [int(value["Item1"]), float(value["Item2"]), float(value["Item3"])]


def parseTwoTupleFormat(value):
    if isinstance(value["Item2"]["Item1"], dict):
        d1 = value["Item2"]["Item1"]
        d2 = value["Item2"]["Item2"]

        return [int(value["Item1"]), parseSerializedVector3(d1), parseSerializedVector3(d2)]
    else:
        return [int(value["Item1"]), float(value["Item2"]["Item2"]), float(value["Item2"]["Item3"]), int(value["Item2"]["Item1"])]
    

def parseSerializedVector3(v):
    return np.array([float(v["x"]), float(v["y"]), float(v["z"])])


def getMaxVector(list):
    maxSum = 0
    maxArray = np.array([0, 0, 0])

    for v in list:
        if sum(v) > maxSum:
            maxSum = sum(v)
            maxArray = v
                
    return maxArray


def getStatistics(modelPath, comparisonFiles=None, reactionTimeFileName=None, behaviouralDataFileName=None, scoreFileName=None):
    if comparisonFiles is not None:
        (behaviouralDataFileName, reactionTimeFileName, scoreFileName) = ev.getBehaviouralDataFileNames(modelPath, comparisonFiles)
    result = ev.getEvaluationResult(PurePath(modelPath, reactionTimeFileName), PurePath(modelPath, behaviouralDataFileName), PurePath(modelPath, scoreFileName), getNumpyArrays, getNumpyArraysInclSuspendedCount)

    statistics = {}

    statistics['Average Behaviour'] = u.aggregate(result['Average Behaviour'], np.nanmean) 
    statistics['Standard Deviation Behaviour'] = u.aggregate(result['Standard Deviation Behaviour'], np.nanmean)
    statistics['Average Suspended Measurements'] = u.aggregate(result['Average Suspended Measurements'], np.nanmean)
    statistics['Average Reactiontime'] = u.aggregate(result['Average Reactiontime'], np.nanmean)
    statistics['Standard Deviation Reactiontime'] = u.aggregate(result['Standard Deviation Reactiontime'], np.nanmean)

    return statistics
