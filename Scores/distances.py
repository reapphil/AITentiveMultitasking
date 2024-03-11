import pandas as pd
from pathlib import Path, PurePath
import numpy as np
import os
import math
import concurrent.futures
import normalizer

def calculateAbsoluteDistances(df1, df2):
    result = df1.fillna(0) - df2.fillna(0)
    
    return result.abs().sum().sum()


def calculateAbsoluteWeightedDistancePenalty(x, xN, y, yN, total, minValue, maxValue, usePenalty=True):
    if not usePenalty and (math.isnan(x) or math.isnan(y)):
        return 0

    if math.isnan(x) and math.isnan(y):
        return 0
    
    if math.isnan(x):
        return max((yN/total) * abs(minValue - y), (yN/total) * abs(maxValue - y))
    
    if math.isnan(y):
        return max((xN/total) * abs(minValue - x), (xN/total) * abs(maxValue - x))
    
    return ((xN+yN)/total) * abs(x-y)


#In case there is a NaN value in one of the two values, the maximum distance between either the maximum value or the minimum value is used. 
# Therefore, a NaN value in one of the two values results in the maximum possible distance. Furthermore, the result is weighted based on the number
# of times a bin was visited divided by the total number of visits over both datasets.
def calculateAbsoluteWeightedDistancesPenalty(df1, df1N, df2, df2N, minValue, maxValue, usePenalty=True):
    total = df1N.sum().sum() + df2N.sum().sum()
    
    new_vals = [calculateAbsoluteWeightedDistancePenalty(x=x,
                                 xN=xN,
                                 y=y,
                                 yN=yN,
                                 total = total,
                                 minValue=minValue,
                                 maxValue=maxValue,
                                 usePenalty=usePenalty) for x, xN, y, yN in zip(df1.values.flat, df1N.values.flat, df2.values.flat, df2N.values.flat)]
    df_dist = pd.DataFrame(np.array(new_vals).reshape(df1.shape), columns=df1.columns)
    
    return df_dist.sum().sum()


def calculateWeightedDistancePenalty(x, xN, y, yN, total, minValue, maxValue, usePenalty=True):
    if not usePenalty and (math.isnan(x) or math.isnan(y)):
        return 0

    if math.isnan(x) and math.isnan(y):
        return 0
    
    if math.isnan(x):
        return max((yN/total) * (minValue - y), (yN/total) * (maxValue - y))
    
    if math.isnan(y):
        return max((xN/total) * (minValue - x), (xN/total) * (maxValue - x))
    
    return ((xN+yN)/total) * (x-y)


def calculateWeightedDistancesPenalty(df1, df1N, df2, df2N, minValue, maxValue, usePenalty=True):
    total = df1N.sum().sum() + df2N.sum().sum()
    
    new_vals = [calculateWeightedDistancePenalty(x=x,
                                 xN=xN,
                                 y=y,
                                 yN=yN,
                                 total = total,
                                 minValue=minValue,
                                 maxValue=maxValue,
                                 usePenalty=usePenalty) for x, xN, y, yN in zip(df1.values.flat, df1N.values.flat, df2.values.flat, df2N.values.flat)]
    df_dist = pd.DataFrame(np.array(new_vals).reshape(df1.shape), columns=df1.columns)
    
    return df_dist.sum().sum()


def calculateWeightedEuclideanDistancePenalty(x, xN, y, yN, total, minValue, maxValue, usePenalty=True):
    if not usePenalty and (not isinstance(x, np.ndarray) or not isinstance(y, np.ndarray)):
        return 0
    
    if not isinstance(x, np.ndarray) and not isinstance(y, np.ndarray):
        return 0
    
    if not isinstance(x, np.ndarray):
        return max([(yN/total) * np.linalg.norm(minValue-y), (yN/total) * np.linalg.norm(maxValue-y), (yN/total) * np.linalg.norm(np.array([maxValue[0], 0, minValue[2]])-y), (yN/total) * np.linalg.norm(np.array([minValue[0], 0, maxValue[2]])-y)])
    
    if not isinstance(y, np.ndarray):
        return max([(xN/total) * np.linalg.norm(minValue-x), (xN/total) * np.linalg.norm(maxValue-x), (xN/total) * np.linalg.norm(np.array([maxValue[0], 0, minValue[2]])-x), (xN/total) * np.linalg.norm(np.array([minValue[0], 0, maxValue[2]])-x)])
    
    x = np.nan_to_num(x)
    y = np.nan_to_num(y)

    return ((xN+yN)/total) * np.linalg.norm(x-y)


#Function to calculate the overall sum of Euclidean distances
def calculateWeightedEuclideanDistancesPenalty(df1, df1N, df2, df2N, minValue, maxValue, usePenalty=True):
    total = df1N.sum().sum() + df2N.sum().sum()
    
    new_vals = [calculateWeightedEuclideanDistancePenalty(x=x,
                                                          xN=xN,
                                                          y=y, 
                                                          yN=yN,
                                                          total=total, 
                                                          minValue=minValue, 
                                                          maxValue=maxValue,
                                                          usePenalty=usePenalty) for x, xN, y, yN in zip(df1.values.flat, df1N.values.flat, df2.values.flat, df2N.values.flat)]
    df_dist = pd.DataFrame(np.array(new_vals).reshape(df1.shape), columns=df1.columns)

    return df_dist.sum().sum()


#function to calculate the different distances for all models
def calculateDistances(humanData, agentDataDicts, ReactionTimeMax, ReactionTimeStdMax, ActionMin, ActionMax, BehaviourStdMax, calculateAbsoluteWeightedDistancesPenalty = calculateAbsoluteWeightedDistancesPenalty, calculateAbsoluteDistances = calculateAbsoluteDistances, calculateWeightedEuclideanDistancesPenalty = calculateWeightedEuclideanDistancesPenalty, calculateWeightedDistancesPenalty = calculateWeightedDistancesPenalty, calculateAccumulatedOverallDistance = None, suspendedMax = 0):
    columns=['Average Reactiontime', 'Average Reactiontime No Penalty', 'Relative Average Reactiontime', 'Relative Average Reactiontime No Penalty', 'Standard Deviation Reactiontime', 'N Reactiontime', 'Average Behaviour', 'Average Behaviour No Penalty', 'Standard Deviation Behaviour', 'N Behaviour', 'Average Duration per Episode', 'Average Distance to Center per Episode']
    if 'Average Suspended Measurements' in humanData:
        columns.insert(6, 'Relative Average Suspended Measurements No Penalty')
        columns.insert(6, 'Relative Average Suspended Measurements')
        columns.insert(6, 'Average Suspended Measurements No Penalty')
        columns.insert(6, 'Average Suspended Measurements')

    if calculateAccumulatedOverallDistance is not None:
        columns.append('Accumulated Overall Normalized Distance')
        for c in range(1, 8):
            columns.append('Accumulated Overall Normalized Distance {}'.format(c))


    result = pd.DataFrame(columns=columns)
    result.index.name = 'Model Name'
    count = 1
    
    with concurrent.futures.ProcessPoolExecutor() as executor:
        future_to_agentData = {executor.submit(calculateDistance, humanData, agentDataDicts[key], ReactionTimeMax, ReactionTimeStdMax, ActionMin, ActionMax, BehaviourStdMax, calculateAbsoluteWeightedDistancesPenalty = calculateAbsoluteWeightedDistancesPenalty, calculateAbsoluteDistances = calculateAbsoluteDistances, calculateWeightedEuclideanDistancesPenalty = calculateWeightedEuclideanDistancesPenalty, calculateWeightedDistancesPenalty = calculateWeightedDistancesPenalty, calculateAccumulatedOverallDistance = calculateAccumulatedOverallDistance, suspendedMax = suspendedMax): key for key in agentDataDicts}

        for future in concurrent.futures.as_completed(future_to_agentData):
            key = future_to_agentData[future]

            try:
                result.loc[key] = future.result()
            except KeyError as e:
                print("Could not calculate distance for {0}: {1}".format(key, repr(e)))

            print("{0}/{1} Distances Calculated...".format(count, len(agentDataDicts)), end='\r')
            count += 1
        
    return result


def calculateDistance(data1, data2, ReactionTimeMax, ReactionTimeStdMax, ActionMin, ActionMax, BehaviourStdMax, calculateAbsoluteWeightedDistancesPenalty = calculateAbsoluteWeightedDistancesPenalty, calculateAbsoluteDistances = calculateAbsoluteDistances, calculateWeightedEuclideanDistancesPenalty = calculateWeightedEuclideanDistancesPenalty, calculateWeightedDistancesPenalty = calculateWeightedDistancesPenalty, calculateAccumulatedOverallDistance = None, suspendedMax = 0):
    averageAbsolutReactionTime = calculateAbsoluteWeightedDistancesPenalty(df1=data1['Average Reactiontime'],
                                                                       df1N=data1['N Reactiontime'],
                                                                       df2=data2['Average Reactiontime'],
                                                                       df2N=data2['N Reactiontime'],
                                                                       minValue=0, 
                                                                       maxValue=ReactionTimeMax)
    averageAbsolutReactionTimeNoPenalty = calculateAbsoluteWeightedDistancesPenalty(df1=data1['Average Reactiontime'],
                                                                       df1N=data1['N Reactiontime'],
                                                                       df2=data2['Average Reactiontime'],
                                                                       df2N=data2['N Reactiontime'],
                                                                       minValue=0, 
                                                                       maxValue=ReactionTimeMax,
                                                                       usePenalty=False)
    averageReactionTime = calculateWeightedDistancesPenalty(df1=data1['Average Reactiontime'],
                                                          df1N=data1['N Reactiontime'],
                                                          df2=data2['Average Reactiontime'],
                                                          df2N=data2['N Reactiontime'],
                                                          minValue=0, 
                                                          maxValue=ReactionTimeMax)
    averageReactionTimeNoPenalty = calculateWeightedDistancesPenalty(df1=data1['Average Reactiontime'],
                                                          df1N=data1['N Reactiontime'],
                                                          df2=data2['Average Reactiontime'],
                                                          df2N=data2['N Reactiontime'],
                                                          minValue=0, 
                                                          maxValue=ReactionTimeMax,
                                                          usePenalty=False)
    stdReactionTime = calculateAbsoluteWeightedDistancesPenalty(df1=data1['Standard Deviation Reactiontime'],
                                                                df1N=data1['N Reactiontime'],
                                                                df2=data2['Standard Deviation Reactiontime'],
                                                                df2N=data2['N Reactiontime'],
                                                                minValue=0,
                                                                maxValue=ReactionTimeStdMax)
    nReactionTime = calculateAbsoluteDistances(df1=data1['N Reactiontime'],
                                                df2=data2['N Reactiontime'])
    averageBehaviour = calculateWeightedEuclideanDistancesPenalty(df1=data1['Average Behaviour'], 
                                                                    df1N=data1['N Behaviour'],
                                                                    df2=data2['Average Behaviour'],
                                                                    df2N=data2['N Behaviour'],
                                                                    minValue=ActionMin,
                                                                    maxValue=ActionMax)
    averageBehaviourNoPenalty = calculateWeightedEuclideanDistancesPenalty(df1=data1['Average Behaviour'], 
                                                                    df1N=data1['N Behaviour'],
                                                                    df2=data2['Average Behaviour'],
                                                                    df2N=data2['N Behaviour'],
                                                                    minValue=ActionMin,
                                                                    maxValue=ActionMax,
                                                                    usePenalty=False)
    stdBehaviour = calculateWeightedEuclideanDistancesPenalty(df1=data1['Standard Deviation Behaviour'],
                                                                df1N=data1['N Behaviour'],
                                                                df2=data2['Standard Deviation Behaviour'],
                                                                df2N=data2['N Behaviour'],
                                                                minValue=np.array([0, 0, 0]), 
                                                                maxValue=BehaviourStdMax)
    nBehaviour = calculateAbsoluteDistances(df1=data1['N Behaviour'],
                                            df2=data2['N Behaviour'])
    averageDuration = data1['Scores']['Duration'].mean() - data2['Scores']['Duration'].mean()
    averageDistanceToCenter = data1['Scores']['AverageDistanceToCenter'].mean() - data2['Scores']['AverageDistanceToCenter'].mean()

    result = {'Average Reactiontime': averageAbsolutReactionTime, 'Average Reactiontime No Penalty': averageAbsolutReactionTimeNoPenalty, 'Relative Average Reactiontime': averageReactionTime, 'Relative Average Reactiontime No Penalty': averageReactionTimeNoPenalty, 'Standard Deviation Reactiontime': stdReactionTime, 'N Reactiontime': nReactionTime, 'Average Behaviour': averageBehaviour, 'Average Behaviour No Penalty': averageBehaviourNoPenalty, 'Standard Deviation Behaviour': stdBehaviour, 'N Behaviour': nBehaviour, 'Average Duration per Episode': averageDuration, 'Average Distance to Center per Episode': averageDistanceToCenter}

    if calculateAccumulatedOverallDistance is not None:
        total, details = calculateAccumulatedOverallDistance(data1, data2, norm_func=normalizer.min_max_norm_dict, verbose=True)
        result['Accumulated Overall Normalized Distance'] = total

        for c, detail in enumerate(details):
            result['Accumulated Overall Normalized Distance {}'.format(c+1)] = detail



    if ('Average Suspended Measurements' in data1 and 'Average Suspended Measurements' in data2):
        averageSuspended = calculateAbsoluteWeightedDistancesPenalty(df1=data1['Average Suspended Measurements'],
                                                          df1N=data1['N Reactiontime'],
                                                          df2=data2['Average Suspended Measurements'],
                                                          df2N=data2['N Reactiontime'],
                                                          minValue=0, 
                                                          maxValue=suspendedMax)
        averageSuspendedNoPenalty = calculateAbsoluteWeightedDistancesPenalty(df1=data1['Average Suspended Measurements'],
                                                          df1N=data1['N Reactiontime'],
                                                          df2=data2['Average Suspended Measurements'],
                                                          df2N=data2['N Reactiontime'],
                                                          minValue=0, 
                                                          maxValue=suspendedMax,
                                                          usePenalty=False)
        averageRelativeSuspended = calculateWeightedDistancesPenalty(df1=data1['Average Suspended Measurements'],
                                                          df1N=data1['N Reactiontime'],
                                                          df2=data2['Average Suspended Measurements'],
                                                          df2N=data2['N Reactiontime'],
                                                          minValue=0, 
                                                          maxValue=suspendedMax)
        averageRelativeSuspendedNoPenalty = calculateWeightedDistancesPenalty(df1=data1['Average Suspended Measurements'],
                                                    df1N=data1['N Reactiontime'],
                                                    df2=data2['Average Suspended Measurements'],
                                                    df2N=data2['N Reactiontime'],
                                                    minValue=0, 
                                                    maxValue=suspendedMax,
                                                    usePenalty=False)

        
        result['Average Suspended Measurements'] = averageSuspended
        result['Average Suspended Measurements No Penalty'] = averageSuspendedNoPenalty
        result['Relative Average Suspended Measurements'] = averageRelativeSuspended
        result['Relative Average Suspended Measurements No Penalty'] = averageRelativeSuspendedNoPenalty


    return result


def compare_distances(df, model_names):
    df = df.loc[model_names]

    c_max = df[["N Reactiontime", "Average Suspended Measurements No Penalty", "Average Behaviour No Penalty", "Standard Deviation Behaviour", "N Behaviour", "Average Duration per Episode", "Average Distance to Center per Episode"]].abs().max()

    df['Accumulated Overall Distance Comparison'] = (df / c_max).abs().sum(axis=1)
    
    return df