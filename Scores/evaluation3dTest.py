import unittest
import evaluation as ev
import evaluation_3d as ev_3d
import numpy as np
import pandas as pd
from pathlib import Path, PurePath
import os
import math
import json


class Evaluation3dTest(unittest.TestCase):
    reactionTimePath = None
    behaviouralDataPath = None
    dfDict = None
    reactionTimeDf = None
    behaviouralDataDf = None


    @classmethod
    def setUpClass(cls):
        currentPath = Path(os.getcwd())
        cls.reactionTimePath = PurePath(currentPath, 'TestData', 'reactionTime.json')
        cls.behaviouralDataPath = PurePath(currentPath, 'TestData', 'behaviouralData.json')

        cls.dfDict = ev.getEvaluationResult(cls.reactionTimePath, cls.behaviouralDataPath, getData = ev_3d.getNumpyArrays)


    def test_getEvaluationResultAverageReactionTime(self):
        self.assertAlmostEqual(self.dfDict['Average Reactiontime'][0][0][4], 89.79, 2)
        self.assertAlmostEqual(sum([sum([sum(d.values()) for d in dicts]) for dicts in self.dfDict['Average Reactiontime']]), 2263.57, 1)
        


    def test_getEvaluationResultStandardDeviationReactiontime(self):
        self.assertEqual(self.dfDict['Standard Deviation Reactiontime'][1][0][4], calculateStd(2642.096, 1035333.068, 7))
        self.assertEqual(self.dfDict['Standard Deviation Reactiontime'][1][0][3], calculateStd(290, 84100, 1))


    def test_getEvaluationResultNReactionTime(self):
        self.assertEqual(self.dfDict['N Reactiontime'][0][0][4], 19)
        self.assertEqual(sum([sum([sum(d.values()) for d in dicts]) for dicts in self.dfDict['N Reactiontime']]), 34)

    
    def test_getEvaluationResultAverageBehaviour(self):
        np.testing.assert_allclose(self.dfDict['Average Behaviour'][1][1][5], np.array([-0.51093333, 0, -0.07453333]))
        np.testing.assert_allclose(sum([sum([sum(d.values()) for d in dicts]) for dicts in self.dfDict['Average Behaviour']]), np.array([-2.5004787, 0, 6.485693942]))


    def test_getEvaluationResultStandardDeviationBehaviour(self):
        np.testing.assert_allclose(self.dfDict['Standard Deviation Behaviour'][1][1][5], np.array([calculateStd(-7.664, 4.572, 15), 0, calculateStd(-1.118, 0.688, 15)]))
        np.testing.assert_allclose(self.dfDict['Standard Deviation Behaviour'][2][0][2], np.array([calculateStd(-2.03, 0.599, 1), 0, calculateStd(6.025, 4.318, 1)]))


    
    def test_getEvaluationResultNBehaviour(self):
        self.assertEqual(self.dfDict['N Behaviour'][2][0][2], 1)
        self.assertEqual(sum([sum([sum(d.values()) for d in dicts]) for dicts in self.dfDict['N Behaviour']]), 29)

    
    def test_getMaxVector(self):
        max_array = ev_3d.getMaxVector([np.array([0, 0, 0]), np.array([-1, 0, -6]), np.array([15, 0, 23]), np.array([14, 0, -28])])
        np.testing.assert_allclose(max_array, np.array([15, 0, 23]))

    
    def test_mapJSONListReactionTimes(self):
        f = """[
                    [
                        [
                            {
                                "0": {
                                    "Item1": 2,
                                    "Item2": 3.1,
                                    "Item3": 9
                                },
                                "1": {
                                    "Item1": 1,
                                    "Item2": 1,
                                    "Item3": 1
                                }
                            },
                            {},
                            {
                                "0": {
                                    "Item1": 2,
                                    "Item2": 2,
                                    "Item3": 2
                                }
                            }
                        ]
                    ]
                ]"""
        
        result = ev_3d.mapJSONList(json.loads(f), ev_3d.parseValue)
        
        self.assertEqual(result[0][0][0][0], [2, 3.1, 9.0])
        


def calculateStd(s1, s2, n):
    result = np.sqrt(s2/n-pow(s1/n, 2))

    if math.isnan(result):
        return 0

    return np.sqrt(s2/n-pow(s1/n, 2))


def parseVector3(stringValue):
    if not isinstance(stringValue, str):
        return stringValue

    stringValue = stringValue[1:-1]
    
    result = stringValue.split(", ")
    
    return np.array([float(result[0]), float(result[1]), float(result[2])], dtype=float)


if __name__ == '__main__':
    unittest.main()
