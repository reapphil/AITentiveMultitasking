import unittest
import evaluation as ev
import numpy as np
import pandas as pd
from pathlib import Path, PurePath
import os
import math


class EvaluationTest(unittest.TestCase):
    reactionTimePath = None
    behaviouralDataPath = None
    dfDict = None
    reactionTimeDf = None
    behaviouralDataDf = None


    @classmethod
    def setUpClass(cls):
        currentPath = Path(os.getcwd())
        #Distance Bin,  Velocity Bin 0,     Velocity Bin 1
        #0,             (3, 600, 120000),   nan
        #1,             nan,                (5, 55, 625)
        #2,             nan,                (6, 60, 600)
        cls.reactionTimePath = PurePath(currentPath, 'TestData', 'reactionTime.csv')
        #Area Bin,  Velocity Bin 0,                                                                 Velocity Bin 1
        #0,         "(3, (-0.6000000, 0.0000000, 0.6000000), (0.1200000, 0.0000000, 0.1200000))",   "(5, (-2.5000000, 0.0000000, 0.5500000), (1.2500000, 0.0000000, 0.0062500))"
        #1,         nan,                                                                            nan
        #2,         nan,                                                                            "(22, (2.2000000, 0.0000000, 0.0000000), (0.2750000, 0.0000000, 0.000000))"
        cls.behaviouralDataPath = PurePath(currentPath, 'TestData', 'behaviouralData.csv')

        cls.dfDict = ev.getEvaluationResult(cls.reactionTimePath, cls.behaviouralDataPath)
        cls.reactionTimeDf = pd.read_csv(cls.reactionTimePath, index_col='Distance Bin', dtype=str)
        cls.behaviouralDataDf = pd.read_csv(cls.behaviouralDataPath, index_col='Area Bin', dtype=str)


    def test_parseReactionTimeTuple(self):
        self.assertEqual(ev.parseTuple("(1, 197.9984, 39203.36640256)"), [1, 197.9984, 39203.36640256])


    def test_parseVectorTuple(self):
        self.assertEqual(ev.parseTuple("(2, (-0.3926410, 0.0000000, -1.0510770), (0.0770835, 0.0000000, 0.5523817))")[0], 2)
        np.testing.assert_array_equal(ev.parseTuple("(2, (-0.3926410, 0.0000000, -1.0510770), (0.0770835, 0.0000000, 0.5523817))")[1], np.array([-0.3926410, 0.0000000, -1.0510770]))
        np.testing.assert_array_equal(ev.parseTuple("(2, (-0.3926410, 0.0000000, -1.0510770), (0.0770835, 0.0000000, 0.5523817))")[2], np.array([0.0770835, 0.0000000, 0.5523817]))


    def test_getEvaluationResultAverageReactionTime(self):
        self.assertEqual(self.dfDict['Average Reactiontime']['Velocity Bin 0'][0], 200)
        self.assertEqual(self.dfDict['Average Reactiontime'].sum().sum(), 221)


    def test_getEvaluationResultStandardDeviationReactiontime(self):
        t00 = calculateStd(600, 120000, 3)
        t11 = calculateStd(55, 625, 5)
        t21 = calculateStd(60, 600, 6)

        self.assertEqual(self.dfDict['Standard Deviation Reactiontime']['Velocity Bin 0'][0], t00)
        self.assertEqual(self.dfDict['Standard Deviation Reactiontime'].sum().sum(), sum([t00, t11, t21]))


    def test_getEvaluationResultNReactionTime(self):
        self.assertEqual(self.dfDict['N Reactiontime']['Velocity Bin 0'][0], 3)
        self.assertEqual(self.dfDict['N Reactiontime'].sum().sum(), 14)

    
    def test_getEvaluationResultAverageBehaviour(self):
        np.testing.assert_allclose(self.dfDict['Average Behaviour']['Velocity Bin 0'][0], np.array([-0.2, 0, 0.2]))
        np.testing.assert_allclose(self.dfDict['Average Behaviour'].sum().sum(), np.array([-0.6, 0, 0.31]))


    def test_getEvaluationResultStandardDeviationBehaviour(self):
        t00 = np.array([calculateStd(-0.6, 0.12, 3), 0, calculateStd(0.6, 0.12, 3)])
        t01 = np.array([calculateStd(-2.5, 1.25, 5), 0, calculateStd(0.55, 0.00625, 5)])
        t21 = np.array([calculateStd(-2.2, 0.275, 22), 0, calculateStd(0, 0, 22)])

        np.testing.assert_allclose(self.dfDict['Standard Deviation Behaviour']['Velocity Bin 0'][0], t00)
        np.testing.assert_allclose(self.dfDict['Standard Deviation Behaviour'].sum().sum(), sum([t00, t01, t21]))

    
    def test_getEvaluationResultNBehaviour(self):
        self.assertEqual(self.dfDict['N Behaviour']['Velocity Bin 0'][0], 3)
        self.assertEqual(self.dfDict['N Behaviour'].sum().sum(), 30)


    def test_getMaxVector(self):
        currentPath = Path(os.getcwd())
        
        humanData = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralDataHuman', 'Standard Deviation Behaviour behaviouralDataHuman.csv'), index_col='Area Bin', dtype=str).applymap(lambda x: parseVector3(x))
        np.testing.assert_allclose(humanData['Velocity Bin 0'][0], np.array([0.0, 0.0, 0.14142136], dtype=float))

        behaviourStdMax = ev.getMaxVector(humanData)
        np.testing.assert_allclose(behaviourStdMax, np.array([0.0, 0.0, 0.14142136], dtype=float))



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
