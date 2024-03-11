from pygments import highlight
from pygments.lexers import PythonLexer
from pygments.formatters import HtmlFormatter
import IPython
import math
import re
import numpy as np


def aggregate(list, func, aggregateKey = False, default_reduce = None, **kwargs):
    try:
        if isinstance(list, dict):
            if aggregateKey:
                return func([k for (k,v) in list.items()], **kwargs) if kwargs else func([k for (k,v) in list.items()])
            else:
                return func([v for (k,v) in list.items()], **kwargs) if kwargs else func([v for (k,v) in list.items()])
        else:
            return func([aggregate(e, func, aggregateKey, default_reduce, **kwargs) for e in list]) if kwargs else func([aggregate(e, func, aggregateKey, default_reduce) for e in list])
    except ValueError as e:
        if not re.search('zero-size array to reduction operation .+ which has no identity', str(e)):
            raise
        else:
            return default_reduce
    

#Flattens multi dim format
def flatten(data, maxKeyNumber, default = math.nan):
    result = []
    flatten_helper(data, maxKeyNumber, result, default)

    return result


def map_recursive_dict(list, func, **kwargs):
    if isinstance(list, dict):
        return {k: func(v, **kwargs) for (k,v) in list.items()}
    else:
        return [map_recursive_dict(e, func, **kwargs) for e in list]
    

def map_recursive(array, func):
    if not isinstance(array[0], (list, np.ndarray, np.generic)):
        return func(array)
    else:
        return [map_recursive(e, func) for e in array]

        
def flatten_helper(data, maxKeyNumber, list, default):
    if (isinstance(data, dict)):
        for key in range(maxKeyNumber+1):
            if key in data:
                list.append(data[key])
            else:
                list.append(default)
    else:
        [flatten_helper(d, maxKeyNumber, list, default) for d in data]
    

def intersect(d1, d2, d3):
    max_key = max([aggregate(d1, max, aggregateKey = True, default=0), aggregate(d2, max, aggregateKey = True, default=0), aggregate(d3, max, aggregateKey = True, default=0)])

    return intersect_helper(d1, d2, d3, max_key)


def intersect_helper(v1, v2, v3, maxKeyNumber):
    if (isinstance(v1, dict)):
        d = {}
        for key in range(maxKeyNumber+1):
            if key in v1 and key in v2 and key in v3:
                d[key] = v1[key]

        return d
    else:
        l = []
        for c, v in enumerate(v1):
            l.append(intersect_helper(v, v2[c], v3[c], maxKeyNumber))

        return l
    

def getFunctionText(path, signature):
    text = []
    addLine = False
    
    with open(path) as f:
        for line in f.readlines():
            if addLine and ('def' in line):
                addLine = False
                
                for t in reversed(text):
                    if t == '' or t[0] == '#' :
                        text.pop()

            if '(' in signature:
                if ('def ' + signature in line):
                    addLine = True
            else:
                if ('def ' + signature + '(' in line):
                    addLine = True
            
            if addLine:
                text.append(line)
                
    code = ''

    for t in text:
        code = code + t
        
    formatter = HtmlFormatter()
    return IPython.display.HTML('<style type="text/css">{}</style>{}'.format(
                                formatter.get_style_defs('.highlight'),
                                highlight(code, PythonLexer(), formatter)))


def getParametersFromString(s):
    try:
        parts = s.split('AC', 1)
        parts = parts[1].split('TS', 1)
        agentChoice = parts[0]
        parts = parts[1].split('S', 2)
        parts = parts[2].split('SM', 1)
        sigma = float(parts[0])
        parts = parts[1].split('UP', 1)
        sigmaMean = float(parts[0])
        parts = parts[1].split('OP', 1)
        updatePeriod = float(parts[0])
        observationProbability = float(parts[1])

        return {'Agent Choice': agentChoice,
                'Sigma': sigma, 
                'Sigma Mean': sigmaMean, 
                'Update Period': updatePeriod, 
                'Observation Probability': observationProbability}

    except IndexError:
        try:
            remainder = s.split('D', 1)[1]
            parts = remainder.split('R', 1)
            decisionPeriod = int(parts[0])
            remainder = parts[1].split('A', 1)[1]
            parts = remainder.split('T', 1)
            agentChoice = parts[0]
            remainder = parts[1].split('S', 2)[2]
            parts = remainder.split('SM', 1)
            sigma = float(parts[0])
            remainder = parts[1]
            parts = remainder.split('U', 1)
            remainder = parts[1]
            sigmaMean = float(parts[0])
            parts = remainder.split('O', 1)
            remainder = parts[1]
            updatePeriod = float(parts[0])
            parts = remainder.split('RT', 1)
            reactionTime = float(parts[1])
            observationProbability = float(parts[0])

            return {'Decision Period': decisionPeriod, 
                    'Agent Choice': agentChoice, 
                    'Sigma': sigma, 
                    'Sigma Mean': sigmaMean, 
                    'Update Period': updatePeriod, 
                    'Observation Probability': observationProbability, 
                    'Reaction Time': reactionTime}
    
        except ValueError:
            remainder = s.split('D', 1)[1]
            parts = remainder.split('R', 1)
            decisionPeriod = int(parts[0])
            remainder = parts[1].split('A', 1)[1]
            parts = remainder.split('T', 1)
            agentChoice = parts[0]
            remainder = parts[1].split('S', 2)[2]
            parts = remainder.split('SM', 1)
            sigma = float(parts[0])
            remainder = parts[1]
            parts = remainder.split('U', 1)
            remainder = parts[1]
            sigmaMean = float(parts[0])
            parts = remainder.split('O', 1)
            remainder = parts[1]
            updatePeriod = float(parts[0])
            parts = remainder.split('RT', 1)
            remainder = parts[1]
            observationProbability = float(parts[0])
            parts = remainder.split('OD', 1)
            reactionTime = float(parts[0])
            oldDistributionPersistenceTime = float(parts[1])

            return {'Decision Period': decisionPeriod, 
                    'Agent Choice': agentChoice, 
                    'Sigma': sigma, 
                    'Sigma Mean': sigmaMean, 
                    'Update Period': updatePeriod, 
                    'Observation Probability': observationProbability, 
                    'Reaction Time': reactionTime,
                    'Old Distribution Persistence Time': oldDistributionPersistenceTime}      


def prettyPrintConfig(s):

    try:
        parameters = getParametersFromString(s)

        try:
            print("\tDecision Period: {}\n\tAgent Choice: {}\n\tSigma: {}\n\tSigma Mean: {}\n\tUpdateperiode: {}\n\tObservation Probability:{}\n\tReaction Time:{}, '\n\tOld Distribution Persistence Time:{}'\n".format(parameters['Decision Period'], 
                                                                                                                                                                                parameters['Agent Choice'], 
                                                                                                                                                                                parameters['Sigma'], 
                                                                                                                                                                                parameters['Sigma Mean'], 
                                                                                                                                                                                parameters['Update Period'], 
                                                                                                                                                                                parameters['Observation Probability'],
                                                                                                                                                                                parameters['Reaction Time'],
                                                                                                                                                                                parameters['Old Distribution Persistence Time']))
        except KeyError:
            try:
                print("\tDecision Period: {}\n\tAgent Choice: {}\n\tSigma: {}\n\tSigma Mean: {}\n\tUpdateperiode: {}\n\tObservation Probability:{}\n\tReaction Time:{}\n".format(parameters['Decision Period'], 
                                                                                                                                                                                parameters['Agent Choice'], 
                                                                                                                                                                                parameters['Sigma'], 
                                                                                                                                                                                parameters['Sigma Mean'], 
                                                                                                                                                                                parameters['Update Period'], 
                                                                                                                                                                                parameters['Observation Probability'],
                                                                                                                                                                                parameters['Reaction Time']))
            except KeyError:
                print("\tAgent Choice: {}\n\tSigma: {}\n\tSigma Mean: {}\n\tUpdateperiod: {}\n\tObservation Probability:{}\n".format(parameters['Agent Choice'], 
                                                                                                                                    parameters['Sigma'], 
                                                                                                                                    parameters['Sigma Mean'], 
                                                                                                                                    parameters['Update Period'], 
                                                                                                                                    parameters['Observation Probability']))
    except Exception:
        print("OPT")


    
