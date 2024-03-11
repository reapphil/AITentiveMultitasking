import pandas as pd
from pathlib import Path, PurePath
import numpy as np
import os
import math
import distances
import util as u
from normalizer import min_max_norm_dict

import logging
logger = logging.getLogger()

def calculateAbsoluteWeightedDistancesPenalty(df1, df1N, df2, df2N, minValue, maxValue, usePenalty=True):
    total = sum([u.aggregate(df1N, sum), u.aggregate(df2N, sum)])

    max_key = max([u.aggregate(df1, max, aggregateKey = True, default=0), u.aggregate(df2, max, aggregateKey = True, default=0)])
    
    new_vals = [distances.calculateAbsoluteWeightedDistancePenalty(x=x,
                                                                   xN=xN,
                                                                   y=y,
                                                                   yN=yN,
                                                                   total = total,
                                                                   minValue=minValue,
                                                                   maxValue=maxValue,
                                                                   usePenalty=usePenalty) for x, xN, y, yN in zip(u.flatten(df1, max_key), u.flatten(df1N, max_key), u.flatten(df2, max_key), u.flatten(df2N, max_key))]
    
    return sum(new_vals)


def calculateWeightedDistancesPenalty(df1, df1N, df2, df2N, minValue, maxValue, usePenalty=True):
    total = sum([u.aggregate(df1N, sum), u.aggregate(df2N, sum)])

    max_key = max([u.aggregate(df1, max, aggregateKey = True, default=0), u.aggregate(df2, max, aggregateKey = True, default=0)])

    new_vals = [distances.calculateWeightedDistancePenalty(x=x,
                                                           xN=xN,
                                                           y=y,
                                                           yN=yN,
                                                           total = total,
                                                           minValue=minValue,
                                                           maxValue=maxValue,
                                                           usePenalty=usePenalty) for x, xN, y, yN in zip(u.flatten(df1, max_key), u.flatten(df1N, max_key), u.flatten(df2, max_key), u.flatten(df2N, max_key))]
    
    return sum(new_vals)


def calculateWeightedEuclideanDistancesPenalty(df1, df1N, df2, df2N, minValue, maxValue, usePenalty=True, normalize=False):
    total = sum([u.aggregate(df1N, sum), u.aggregate(df2N, sum)])

    max_key = max([u.aggregate(df1, max, aggregateKey = True, default=0), u.aggregate(df2, max, aggregateKey = True, default=0)])
    
    new_vals = [distances.calculateWeightedEuclideanDistancePenalty(x=x,
                                                 xN=xN,
                                                 y=y,
                                                 yN=yN,
                                                 total = total,
                                                 minValue=minValue,
                                                 maxValue=maxValue,
                                                 usePenalty=usePenalty) for x, xN, y, yN in zip(u.flatten(df1, max_key), u.flatten(df1N, max_key), u.flatten(df2, max_key), u.flatten(df2N, max_key))]
    
    if normalize:
        max_distance=np.linalg.norm(minValue-maxValue)
        return sum([x / max_distance for x in new_vals])

    return sum(new_vals)


def calculateAbsoluteDistances(df1, df2):
    max_key = max([u.aggregate(df1, max, aggregateKey = True, default=0), u.aggregate(df2, max, aggregateKey = True, default=0)])
    result = 0

    for d1, d2 in zip(u.flatten(df1, max_key, 0), u.flatten(df2, max_key, 0)):
        result += abs(d1 - d2)
    
    return result


def calculateAbsoluteProportionalDistances(df1, df2):
    max_key = max([u.aggregate(df1, max, aggregateKey = True, default=0), u.aggregate(df2, max, aggregateKey = True, default=0)])
    d1_total = u.aggregate(df1, sum)
    d2_total = u.aggregate(df2, sum)

    result = 0

    for d1, d2 in zip(u.flatten(df1, max_key, 0), u.flatten(df2, max_key, 0)):
        result += abs(d1/d1_total - d2/d2_total)
    
    return result/2


def distance_bin_data(X, Y):
    return cdist_dict(X, Y, distance_bin, norm_func=min_max_norm_dict)


def cdist_dict(X, Y, dist_func, **kwargs):
    d = np.zeros(len(X) * len(Y))
    c = 0

    for dx in X:
        for dy in Y:
            d[c] = np.array([dist_func(dx[0], dy[0], **kwargs)])
            c += 1

    #print("Distance: " + str(d))

    return d


def distance_bin(X, Y, norm_func = None, verbose=False):
    if norm_func is not None:
        X, Y = norm_func(X, Y, norm_key_hierarchy=['Scores', 'Duration'], x_min = 0, x_max = 10)
        X, Y = norm_func(X, Y, norm_key_hierarchy=['Average Suspended Measurements'], x_min = 0, x_max = 30)

    distances = []

    distances.append(calculateAbsoluteProportionalDistances(df1=X['N Reactiontime'],
                                               df2=Y['N Reactiontime']))
    
    distances.append(calculateAbsoluteWeightedDistancesPenalty(df1=X['Average Suspended Measurements'],
                                                            df1N=X['N Reactiontime'],
                                                            df2=Y['Average Suspended Measurements'],
                                                            df2N=Y['N Reactiontime'],
                                                            minValue=0, 
                                                            maxValue=max([u.aggregate(X['Average Suspended Measurements'], max, default=0), u.aggregate(Y['Average Suspended Measurements'], max, default=0)]),
                                                            usePenalty=True))
    
    distances.append(calculateWeightedEuclideanDistancesPenalty(df1=X['Average Behaviour'], 
                                                             df1N=X['N Behaviour'],
                                                             df2=Y['Average Behaviour'],
                                                             df2N=Y['N Behaviour'],
                                                             minValue=np.array([-1, 0, -1]),
                                                             maxValue=np.array([1, 0, 1]),
                                                             usePenalty=True,
                                                             normalize=True))
    
    distances.append(calculateWeightedEuclideanDistancesPenalty(df1=X['Standard Deviation Behaviour'],
                                                             df1N=X['N Behaviour'],
                                                             df2=Y['Standard Deviation Behaviour'],
                                                             df2N=Y['N Behaviour'],
                                                             minValue=np.array([0, 0, 0]), 
                                                             maxValue=np.maximum.reduce([u.aggregate(X['Standard Deviation Behaviour'], np.maximum.reduce, aggregateKey = False, default_reduce=np.array([0, 0, 0])), u.aggregate(Y['Standard Deviation Behaviour'], np.maximum.reduce, aggregateKey = False, default_reduce=np.array([0, 0, 0]))]),
                                                             usePenalty=True,
                                                             normalize=True))
    
    distances.append(calculateAbsoluteProportionalDistances(df1=X['N Behaviour'],
                                             df2=Y['N Behaviour']))
    
    distances.append(abs(X['Scores']['Duration'].mean() - Y['Scores']['Duration'].mean()))
    
    distances.append(abs(X['Scores']['AverageDistanceToCenter'].mean() - Y['Scores']['AverageDistanceToCenter'].mean()))

    logger.debug("Distances: {}, Total Distance: {}".format(distances, sum(distances)))

    if verbose:
        return sum(distances), distances

    return sum(distances)