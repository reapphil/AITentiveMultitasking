import unittest
import distances as dist
import distances_3d as dist_3d
import numpy as np
import pandas as pd
from pathlib import Path, PurePath
import os
import math
import json
import evaluation as ev
import evaluation_3d as ev_3d
import util as u


class DistancesTest(unittest.TestCase):
    humanDataDict = {}
    agentDataDicts = {}
    distanceDf = None


    @classmethod
    def setUpClass(cls):
        currentPath = Path(os.getcwd())
        scoresPath = PurePath(currentPath, "TestData")

        cls.humanDataDict = ev.getEvaluationResult(scoresPath=PurePath(scoresPath, "scores_evalBehavioral.csv"),
                                                reactionTimePath=PurePath(scoresPath, "reactionTime.json"),
                                                behaviouralDataPath=PurePath(scoresPath, "behaviouralData.json"),
                                                getData=ev_3d.getNumpyArrays)

        cls.agentDataDicts['agent'] = ev.getEvaluationResult(scoresPath=PurePath(scoresPath, "scores_evalBehavioral.csv"),
                                        reactionTimePath=PurePath(scoresPath, "reactionTimeHuman.json"),
                                        behaviouralDataPath=PurePath(scoresPath, "behaviouralDataHuman.json"),
                                        getData=ev_3d.getNumpyArrays)

        reactionTimeMax = max(max([[max(d.values(), default=0) for d in dicts] for dicts in cls.humanDataDict['Average Reactiontime']]))
        reactionTimeStdMax = max(max([[max(d.values(), default=0) for d in dicts] for dicts in cls.humanDataDict['Standard Deviation Reactiontime']]))
        behaviourStdMax = ev_3d.getMaxVector([ev_3d.getMaxVector([ev_3d.getMaxVector(d.values()) for d in dicts]) for dicts in cls.humanDataDict['Standard Deviation Behaviour']])
        actionMax = np.array([1, 0, 1])
        actionMin = np.array([-1, 0, -1])
        cls.distanceDf = dist.calculateDistances(cls.humanDataDict, 
                                                 cls.agentDataDicts, 
                                                 reactionTimeMax, 
                                                 reactionTimeStdMax, 
                                                 actionMin, 
                                                 actionMax, 
                                                 behaviourStdMax,
                                                 calculateAbsoluteWeightedDistancesPenalty = dist_3d.calculateWeightedDistancesPenalty,
                                                 calculateAbsoluteDistances = dist_3d.calculateAbsoluteDistances,
                                                 calculateWeightedEuclideanDistancesPenalty = dist_3d.calculateWeightedEuclideanDistancesPenalty,
                                                 calculateWeightedDistancesPenalty = dist_3d.calculateWeightedDistancesPenalty)


    def test_distanceNReationTime(self):
       #...
       #    "6": {                              -,,-
       #        "Item1": 10,                    "Item1": 1,
       #        "Item2": 573.2342,              -,,-
       #        "Item3": 328597.448
       #    }
       #},
       #{            
	   #	"1": {                              -
       #        "Item1": 3,                     -
       #        "Item2": 573.2342,              -
       #        "Item3": 328597.448             -
       #    }},
       #...
        self.assertEqual(self.distanceDf.loc['agent']['N Reactiontime'], 12)


    def test_distanceNBehaviour(self):
       #...
       #"5": {                                  -,,-
	   #		"Item1": 11,                    "Item1": 15  
	   #		"Item2": {                      -,,-            
	   #			"x": -7.664,
	   #			"y": 0.0,
	   #			"z": -1.118
	   #		},
	   #		"Item3": {
	   #			"x": 4.572,
	   #			"y": 0.0,
	   #			"z": 0.688
	   #		}
	   #	}
       #...
       #"2": {                                  -,,-
	   #		"Item1": 8,                     "Item1": 1
	   #		"Item2": {                      -,,-
	   #			"x": -2.03,
	   #			"y": 0.0,
	   #			"z": 6.025
	   #		},
	   #		"Item3": {
	   #			"x": 0.599,
	   #			"y": 0.0,
	   #			"z": 4.318
	   #		}
	   #	}
	   #},
	   #{},
	   #{
	   #	"4": {                              -
	   #		"Item1": 2,                     -
	   #		"Item2": {                      -
	   #			"x": -2.03,                 -
	   #			"y": 0.0,                   -
	   #			"z": 6.025                  -
	   #		},                              -
	   #		"Item3": {                      -
	   #			"x": 0.599,                 -
	   #			"y": 0.0,                   -
	   #			"z": 4.318                  -
	   #		}
	   #	}
        self.assertEqual(self.distanceDf.loc['agent']['N Behaviour'], 13)




if __name__ == '__main__':
    unittest.main()
