import numpy as np
import pandas as pd
import util as u
import copy


def min_max_norm_dict(*argv, norm_key_hierarchy, x_min=None, x_max=None):
    d = copy.deepcopy(argv)

    min_max_norm_dict_helper(*d, norm_key_hierarchy=norm_key_hierarchy, x_min=x_min, x_max=x_max)
        
    return d


def min_max_norm_dict_helper(*argv, norm_key_hierarchy, x_min, x_max):
    k = norm_key_hierarchy[0]

    if len(norm_key_hierarchy) > 1:
        min_max_norm_dict_helper(*(v[k] for v in argv), norm_key_hierarchy = norm_key_hierarchy[1:], x_min=x_min, x_max=x_max)
    else:
        for v in argv:
            if not isinstance(v[k], pd.Series):
                if x_min is None:
                    x_min = min_dict(argv, k)
                if x_max is None:
                    x_max = max_dict(argv, k)

                v[k] = u.map_recursive_dict(v[k], min_max_norm, x_min = x_min, x_max = x_max)
            else:
                try:
                    if x_min is None:
                        x_min = min_df(argv, k)
                    if x_max is None:
                        x_max = max_df(argv, k)

                    #print(column + ": " + x_min)

                    v[k] =  (v[k] - x_min) / (x_max - x_min)
                except TypeError:
                    pass


def min_df(dicts, key):
    mins = []

    for d in dicts:
        mins.append(d[key].min())

    return min(mins)


def max_df(dicts, key):
    maxs = []

    for d in dicts:
        maxs.append(d[key].max())

    return max(maxs)


def min_dict(dicts, key):
    mins = []
    
    for d in dicts:
        try:
            mins.append(u.aggregate(d[key], min, aggregateKey = False, default=0))
            
        except ValueError:
            mins.append(u.aggregate(d[key], np.minimum.reduce, aggregateKey = False, default_reduce=np.array([0, 0, 0])))

    try:
        return min(mins)
    except ValueError:
        return np.minimum.reduce(mins)



def max_dict(dicts, key):
    maxs = []

    for d in dicts:
        try:
            maxs.append(u.aggregate(d[key], max, aggregateKey = False, default=0))
            
        except ValueError:
            maxs.append(u.aggregate(d[key], np.maximum.reduce, aggregateKey = False, default_reduce=np.array([0, 0, 0])))

    try:
        return max(maxs)
    except ValueError:
        return np.maximum.reduce(maxs)


def min_max_norm(x, x_min = None, x_max = None):
    if x_min is None:
        x_min = np.min(x)
    if x_max is None:
        x_max = np.max(x)
        
    if not isinstance(x, (list, np.ndarray, np.generic)) and (x_min == x_max):
        return 0
    else: 
        return (x - x_min) / (x_max - x_min)
    