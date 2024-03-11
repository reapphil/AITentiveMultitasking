import pandas as pd
from pathlib import Path, PurePath
import numpy as np
import os

def parseVector3(stringValue):
    stringValue = stringValue[1:-1]
    
    result = stringValue.split(", ")
    
    return np.array([float(result[0]), float(result[1]), float(result[2])])


def parseTuple(stringValue):
    
    if not isinstance(stringValue, str):
        return stringValue
    
    stringValue = stringValue[1:-1]
    result = stringValue.split(", ", 1)
    
    
    value1 = int(result[0])
    stringValue = result[1]
    
    
    if '(' in stringValue:
        result = stringValue.split("(")
                
        s2 = '(' + result[1][:-2]
        s3 = '(' + result[2]

        return [value1, parseVector3(s2), parseVector3(s3)]
    else:
        result = stringValue.split(", ")
        return [value1, float(result[0]), float(result[1])]


def calculateAverage(listValue):
    if not isinstance(listValue, list):
        return listValue
    
    return listValue[1]/listValue[0]


def calculateAverageSuspendedMeasurementCount(listValue):
    if not isinstance(listValue, list):
        return listValue
    
    return listValue[3]/listValue[0]


def getN(listValue):
    if not isinstance(listValue, list):
        return listValue
    
    return listValue[0]


def calculateStandardDeviation(listValue):
    if not isinstance(listValue, list):
        return listValue
    
    n = listValue[0]
    average = listValue[1]/n
    s2 = listValue[2]
    
    if n == 1:
        if isinstance(listValue[1], np.ndarray):
            return np.array([0, 0, 0])
        else:
            return 0
        
    value = (((s2/n))-np.power(average, 2))
    if isinstance(listValue[1], np.ndarray):
        if  value[0] < 0:
            value[0] = 0

        if  value[2] < 0:
            value[2] = 0
    
    return np.sqrt(value)


#The Tuples of the CSV files are divided in separate dataframes based on the total count, the average value and the standard deviation.
def getDataframes(path, indexCol):
    tupleDf = pd.read_csv(path, index_col=indexCol, dtype=str)
    df = tupleDf.applymap(lambda x: parseTuple(x))
    
    dfAverage = df.applymap(lambda x: calculateAverage(x))
    dfSd = df.applymap(lambda x: calculateStandardDeviation(x))
    dfN = df.applymap(lambda x: getN(x))
    
    return (dfAverage, dfSd, dfN)


def getEvaluationResult(reactionTimePath=None, behaviouralDataPath=None, scoresPath=None, getData = getDataframes, getDataReactionTime = None):
    if (getDataReactionTime is None):
        getDataReactionTime = getData

    if scoresPath is not None:
        resultDict = {'Scores' : pd.read_csv(scoresPath)}
    else:
        resultDict = {}
        
    if reactionTimePath is not None:
        resultDict.update(getReactiontimeEvaluation(reactionTimePath, getDataReactionTime))

    if behaviouralDataPath is not None:
        resultDict.update(getBehaviouralDataEvaluation(behaviouralDataPath, getData))
    
    return resultDict


def getReactiontimeEvaluation(reactionTimePath, getData):
    dfSAverage = None
    
    try:
        (dfAverage, dfSd, dfN, dfSAverage) = getData(reactionTimePath, 'Distance Bin')
    except (ValueError):
        (dfAverage, dfSd, dfN) = getData(reactionTimePath, 'Distance Bin')

    result = {
             'Average Reactiontime' : dfAverage,
             'Standard Deviation Reactiontime' : dfSd,
             'N Reactiontime' : dfN
             }

    if (dfSAverage is not None):
        result['Average Suspended Measurements'] = dfSAverage

    return result


def getBehaviouralDataEvaluation(behaviouralDataPath, getData):
    (dfAverage, dfSd, dfN) = getData(behaviouralDataPath, 'Area Bin')

    return  {
            'Average Behaviour' : dfAverage,
            'Standard Deviation Behaviour' : dfSd,
            'N Behaviour' : dfN
            }


def getEvaluationResults(sessionPath, comparisonFiles, scoresPath=None, getData = getDataframes, getDataReactionTime = None, configString = None):
    if (getDataReactionTime is None):
        getDataReactionTime = getData

    modelDirs = [x for x in next(os.walk(sessionPath))][1]
    result = {}
    count = 1
    
    for modelDir in modelDirs:
        if scoresPath is None:
            absoluteScoresPath = PurePath(sessionPath, modelDir, 'SupervisorML_Data', 'Scores')
        else:
            absoluteScoresPath = PurePath(sessionPath, modelDir, scoresPath)

        try:
            (behaviouralDataFileName, reactionTimeFileName, scoreFileName) = getBehaviouralDataFileNames(absoluteScoresPath, comparisonFiles, configString)
            result[modelDir] = getEvaluationResult(PurePath(absoluteScoresPath, reactionTimeFileName), PurePath(absoluteScoresPath, behaviouralDataFileName), PurePath(absoluteScoresPath, scoreFileName), getData, getDataReactionTime)
        except (FileNotFoundError, IndexError):
            print("Could not find scores for {0} ({1}/{2})!".format(absoluteScoresPath, count, len(modelDirs)))

        print("{0}/{1} Evaluation Results Calculated...".format(count, len(modelDirs)), end='\r')
        count += 1
    
    return result


def getBehaviouralDataFileNames(absoluteScoresPath, comparisonFiles, configString = None):
    if (configString is not None):
        return getBehaviouralDataFileNamesByConfigString(absoluteScoresPath, comparisonFiles, configString)
    else:
        return getBehaviouralDataFileNamesWithoutConfigString(absoluteScoresPath, comparisonFiles)


def getBehaviouralDataFileNamesByConfigString(absoluteScoresPath, comparisonFiles=[], configString=None):
    parts = configString.split('NT')
    parts[1] = 'NT' + parts[1]

    evalFiles = os.listdir(absoluteScoresPath)

    scoreFileName = [s for s in evalFiles if ("scores" in s) and (s not in comparisonFiles)][0]
    reactionTimeFileName = [s for s in evalFiles if ("reactionTime" in s or "rt" in s) and 
                                                    (s not in comparisonFiles) and 
                                                    (parts[1] in s)][0]
    behaviouralDataFileName = [s for s in evalFiles if (not ("reactionTime" in s or "rt" in s) and 
                                                       ("NAB" in s or "NAN" in s) and 
                                                       not ("scores" in s) and 
                                                       (s not in comparisonFiles) and 
                                                       (parts[0] in s))][0]

    return (behaviouralDataFileName, reactionTimeFileName, scoreFileName)


def getBehaviouralDataFileNamesWithoutConfigString(absoluteScoresPath, comparisonFiles):
    evalFiles = os.listdir(absoluteScoresPath)
    scoreFileName = [s for s in evalFiles if ("scores" in s) and (s not in comparisonFiles)][0]
    reactionTimeFileName = [s for s in evalFiles if ("reactionTime" in s or "rt" in s) and 
                                                    (s not in comparisonFiles)][0]
    behaviouralDataFileName = [s for s in evalFiles if (not ("reactionTime" in s or "rt" in s) and 
                                                       ("NAB" in s or "NAN" in s) and 
                                                       not ("scores" in s) and 
                                                       (s not in comparisonFiles))][0]

    return (behaviouralDataFileName, reactionTimeFileName, scoreFileName)


def getScoresResult(evaluations):
    result = pd.DataFrame(columns=['Average Duration per Episode', 'Average Distance to Center per Episode'])
    result.index.name = 'Model Name'
    count = 1

    for key in evaluations:
        averageDuration = evaluations[key]['Scores']['Duration'].mean()
        averageDistanceToCenter = evaluations[key]['Scores']['AverageDistanceToCenter'].mean()

        result.loc[key] = {'Average Duration per Episode': averageDuration, 'Average Distance to Center per Episode': averageDistanceToCenter}

        print("{0}/{1} Distances Calculated...".format(count, len(evaluations)), end='\r')
        count += 1

    return result


def getMaxVector(df):
    maxSum = 0
    maxArray = np.array([0, 0, 0])
    
    for v in df.to_numpy().flatten().tolist():
        if isinstance(v, np.ndarray):
            if sum(v) > maxSum:
                maxSum = sum(v)
                maxArray = v
                
    return maxArray