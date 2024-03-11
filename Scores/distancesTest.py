import unittest
import distances as dist
import numpy as np
import pandas as pd
from pathlib import Path, PurePath
import os
import math


class DistancesTest(unittest.TestCase):
    humanDataDict = {}
    agentDataDicts = {}
    distanceDf = None


    @classmethod
    def setUpClass(cls):
        currentPath = Path(os.getcwd())

        cls.humanDataDict['Average Behaviour'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralDataHuman', 'Average Behaviour behaviouralDataHuman.csv'), index_col='Area Bin', dtype=str).applymap(lambda x: parseVector3(x))
        np.testing.assert_allclose(cls.humanDataDict['Average Behaviour']['Velocity Bin 0'][0], np.array([-0.05, 0., 0.2], dtype=float))
        cls.humanDataDict['Average Reactiontime'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralDataHuman', 'Average Reactiontime behaviouralDataHuman.csv'), index_col='Distance Bin', dtype=float)
        cls.humanDataDict['N Behaviour'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralDataHuman', 'N Behaviour behaviouralDataHuman.csv'), index_col='Area Bin', dtype=float)
        cls.humanDataDict['N Reactiontime'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralDataHuman', 'N Reactiontime behaviouralDataHuman.csv'), index_col='Distance Bin', dtype=float)
        cls.humanDataDict['Standard Deviation Behaviour'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralDataHuman', 'Standard Deviation Behaviour behaviouralDataHuman.csv'), index_col='Area Bin', dtype=str).applymap(lambda x: parseVector3(x))
        np.testing.assert_allclose(cls.humanDataDict['Standard Deviation Behaviour']['Velocity Bin 0'][0], np.array([0.0, 0.0, 0.14142136], dtype=float))
        cls.humanDataDict['Standard Deviation Reactiontime'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralDataHuman', 'Standard Deviation Reactiontime behaviouralDataHuman.csv'), index_col='Distance Bin', dtype=float)
        cls.humanDataDict['Scores'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralDataHuman', 'Scores behaviouralDataHuman.csv'))

        cls.agentDataDicts['agent'] = {}
        cls.agentDataDicts['agent']['Average Behaviour'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralData', 'Average Behaviour behaviouralData.csv'), index_col='Area Bin', dtype=str).applymap(lambda x: parseVector3(x))
        np.testing.assert_allclose(cls.agentDataDicts['agent']['Average Behaviour']['Velocity Bin 0'][0], np.array([-0.2, 0.0, 0.2], dtype=float))
        cls.agentDataDicts['agent']['Average Reactiontime'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralData', 'Average Reactiontime behaviouralData.csv'), index_col='Distance Bin', dtype=float)
        cls.agentDataDicts['agent']['N Behaviour'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralData', 'N Behaviour behaviouralData.csv'), index_col='Area Bin', dtype=float)
        cls.agentDataDicts['agent']['N Reactiontime'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralData', 'N Reactiontime behaviouralData.csv'), index_col='Distance Bin', dtype=float)
        cls.agentDataDicts['agent']['Standard Deviation Behaviour'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralData', 'Standard Deviation Behaviour behaviouralData.csv'), index_col='Area Bin', dtype=str).applymap(lambda x: parseVector3(x))
        np.testing.assert_allclose(cls.agentDataDicts['agent']['Standard Deviation Behaviour']['Velocity Bin 1'][2], np.array([0.05, 0.0, 0.0], dtype=float))
        cls.agentDataDicts['agent']['Standard Deviation Reactiontime'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralData', 'Standard Deviation Reactiontime behaviouralData.csv'), index_col='Distance Bin', dtype=float)
        cls.agentDataDicts['agent']['Scores'] = pd.read_csv(PurePath(currentPath, 'TestData', 'behaviouralData', 'Scores behaviouralData.csv'))

        reactionTimeMax = 200 #humanDataDict['Average Reactiontime'].max().max()
        reactionTimeStdMax = 2.0412414 #humanDataDict['Standard Deviation Reactiontime'].max().max()
        behaviourStdMax = np.array([0.0, 0.0, 0.14142136]) #ev.getMaxVector(humanDataDict['Standard Deviation Behaviour'])
        actionMax = np.array([1, 0, 1])
        actionMin = np.array([-1, 0, -1])
        cls.distanceDf = dist.calculateDistances(cls.humanDataDict, cls.agentDataDicts, reactionTimeMax, reactionTimeStdMax, actionMin, actionMax, behaviourStdMax)


    def test_distanceAverageReationTime(self):
        #human (total = 26)
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             (4, 200.0),
        #1,             ,               (5, 11.0)
        #2,             (5, 11.0),      (12, 5.0)

        #agent (total: = 14)
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             (3, 200.0),
        #1,             ,               (5, 11.0)
        #2,             ,               (6, 10.0)

        #distances
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             0,              0               *e.g. (7/40)*abs(200-200)=0
        #1,             0,              0
        #2,             23.625,         2.25            *dist(11, 0) < dist(11, 200)
        
        self.assertEqual(self.distanceDf.loc['agent']['Average Reactiontime'], 25.875)


    def test_distanceAverageReationTimeNoPenalty(self):
        #human (total = 26)
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             (4, 200.0),
        #1,             ,               (5, 11.0)
        #2,             (5, 11.0),      (12, 5.0)

        #agent (total: = 14)
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             (3, 200.0),
        #1,             ,               (5, 11.0)
        #2,             ,               (6, 10.0)

        #distances
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             0,              0               *e.g. (7/40)*abs(200-200)=0
        #1,             0,              0
        #2,             0,              2.25            
        
        self.assertEqual(self.distanceDf.loc['agent']['Average Reactiontime No Penalty'], 2.25)

    
    def test_distanceStandardDeviationReationTime(self):
        #human (total = 26)
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             (4, 0),
        #1,             ,               (5, 2)
        #2,             (5, 2),         (12, 2.041241452319315)

        #agent (total = 14)
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             (3, 0),
        #1,             ,               (5, 2.0)
        #2,             ,               (6, 0.0)

        #distances
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             0,              0               *e.g. (7/40)*abs(0-0)=0
        #1,             0,              0
        #2,             0.25,           0.918558654     *dist(2, 0) > dist(2, 2.0412414)
        
        self.assertEqual(round(self.distanceDf.loc['agent']['Standard Deviation Reactiontime'], 5), round(1.168558654, 5))


    def test_distanceNReationTime(self):
        #human (total = 26)
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             4,
        #1,             ,               5
        #2,             5,              12

        #agent (total = 14)
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             3,
        #1,             ,               5
        #2,             ,               6

        #distances
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             1,              0               *e.g. abs(4-3)=1
        #1,             0,              0
        #2,             5,              6               
        
        self.assertEqual(self.distanceDf.loc['agent']['N Reactiontime'], 12)


    def test_distanceDuration(self):
        #human
        #Duration
        #90
        #110

        #agent
        #Duration
        #70
        #80

        #distances
        #Duration
        #25         *abs(200/2-150/2)    
        
        self.assertEqual(self.distanceDf.loc['agent']['Average Duration per Episode'], 25)


    def test_distanceAverageDistanceToCenter(self):
        #human
        #Duration
        #3.5
        #4

        #agent
        #Duration
        #2.5
        #3

        #distances
        #Duration
        #1         *abs(7.5/2-5.5/2)    
        
        self.assertEqual(self.distanceDf.loc['agent']['Average Distance to Center per Episode'], 1)


    def test_distanceAverageBehaviour(self):
        #human (total = 10)
        #Area Bin,  Velocity Bin 0,             Velocity Bin 1
        #0,         "(5, [-0.05, 0.0, 0.2]"),
        #1,         ,
        #2,         ,                           "(5, [-0.5, 0.0, 0.11]")

        #agent (total: = 30)
        #Area Bin,  Velocity Bin 0,             Velocity Bin 1
        #0,         "(3, [-0.2, 0.0, 0.2]"),    "(5, [-0.5, 0.0, 0.11]")
        #1,         ,
        #2,         ,                           "(22, [0.1, 0.0, 0.0]")

        #distances
        #Area Bin,  Velocity Bin 0, Velocity Bin 1
        #0,         0.03,           0.2332548       *[0]:(8/40)*sqrt(((-0.05+0.2)^2)+((0.2-0.2)^2));    [1]:(5/40)*sqrt(((1+0.5)^2)+((-1-0.11)^2)) 
        #-                                                                                              dist([-0.5, 0.0, 0.11], [1, 0.0, 1]) > dist([-0.5, 0.0, 0.11], [-1, 0.0, -1])
        #1,         0,              0
        #2,         0,              0.41175         *[1]:(27/40)*sqrt(((-0.5-0.1)^2)+((0.11-0)^2))
        
        self.assertEqual(round(self.distanceDf.loc['agent']['Average Behaviour'], 6), round(0.6750048, 6))


    def test_distanceAverageBehaviourNoPenalty(self):
        #human (total = 10)
        #Area Bin,  Velocity Bin 0,             Velocity Bin 1
        #0,         "(5, [-0.05, 0.0, 0.2]"),
        #1,         ,
        #2,         ,                           "(5, [-0.5, 0.0, 0.11]")

        #agent (total: = 30)
        #Area Bin,  Velocity Bin 0,             Velocity Bin 1
        #0,         "(3, [-0.2, 0.0, 0.2]"),    "(5, [-0.5, 0.0, 0.11]")
        #1,         ,
        #2,         ,                           "(22, [0.1, 0.0, 0.0]")

        #distances
        #Area Bin,  Velocity Bin 0, Velocity Bin 1
        #0,         0.03,           0               *[0]:(8/40)*sqrt(((-0.05+0.2)^2)+((0.2-0.2)^2));
        #-                                                                                              dist([-0.5, 0.0, 0.11], [1, 0.0, 1]) > dist([-0.5, 0.0, 0.11], [-1, 0.0, -1])
        #1,         0,              0
        #2,         0,              0.41175         *[1]:(27/40)*sqrt(((-0.5-0.1)^2)+((0.11-0)^2))
        
        self.assertEqual(round(self.distanceDf.loc['agent']['Average Behaviour No Penalty'], 6), 0.44175)


    def test_distanceAverageBehaviour(self):
        #human (total = 10)
        #Area Bin,  Velocity Bin 0,             Velocity Bin 1
        #0,         "(5, [-0.05, 0.0, 0.2]"),
        #1,         ,
        #2,         ,                           "(5, [-0.5, 0.0, 0.11]")

        #agent (total: = 30)
        #Area Bin,  Velocity Bin 0,             Velocity Bin 1
        #0,         "(3, [-0.2, 0.0, 0.2]"),    "(5, [-0.5, 0.0, 0.11]")
        #1,         ,
        #2,         ,                           "(22, [0.1, 0.0, 0.0]")

        #distances
        #Area Bin,  Velocity Bin 0, Velocity Bin 1
        #0,         0.03,           0.2332548       *[0]:(8/40)*sqrt(((-0.05+0.2)^2)+((0.2-0.2)^2));    [1]:(5/40)*sqrt(((1+0.5)^2)+((-1-0.11)^2)) 
        #-                                                                                              dist([-0.5, 0.0, 0.11], [1, 0.0, 1]) > dist([-0.5, 0.0, 0.11], [-1, 0.0, -1])
        #1,         0,              0
        #2,         0,              0.41175         *[1]:(27/40)*sqrt(((-0.5-0.1)^2)+((0.11-0)^2))
        
        self.assertEqual(round(self.distanceDf.loc['agent']['Average Behaviour'], 6), round(0.6750048, 6))


    def test_distanceStandardDeviationBehaviour(self):
        #human (total = 10)
        #Area Bin,  Velocity Bin 0,                     Velocity Bin 1
        #0,         "(5, "[0.0, 0.0, 0.14142136]""),
        #1,         ,
        #2,         ,                                   "(5, [0.0, 0.0, 0.0]")

        #agent (total: = 30)
        #Area Bin,  Velocity Bin 0,                     Velocity Bin 1
        #0,         "(3, [0.0, 0.0, 0.0])",             "(5, [0.0, 0.0, 0.0]")
        #1,         ,
        #2,         ,                                   (22, "[0.05, 0.0, 0.0]")

        #distances
        #Area Bin,  Velocity Bin 0,                     Velocity Bin 1
        #0,         0.02828427,                         0.01767767      *[0]:(8/40)*sqrt(((0-0)^2)+((0.14142136-0)^2));     [1]:(5/40)*sqrt(((0-0)^2)+((0.14142136-0)^2)) 
        #-                                                                                                                  dist([0.0, 0.0, 0.0], [1, 0.0, 1]) = dist([0.0, 0.0, 0.0], [-1, 0.0, -1])
        #1,         0,                                  0
        #2,         0,              0.03375                             *[1]:(27/40)*sqrt(((0.05-0)^2)+((0-0)^2))
        
        self.assertEqual(round(self.distanceDf.loc['agent']['Standard Deviation Behaviour'], 6), round(0.07971194, 6))


    def test_distanceNBehaviour(self):
        #human (total = 10)
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             5,
        #1,             ,               
        #2,             ,              5

        #agent (total = 30)
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             3,
        #1,             ,               5
        #2,             ,               22

        #distances
        #Distance Bin,  Velocity Bin 0, Velocity Bin 1
        #0,             2,              0               *e.g. abs(5-3)=2
        #1,             0,              5
        #2,             0,              17               
        
        self.assertEqual(self.distanceDf.loc['agent']['N Behaviour'], 24)


def parseVector3(stringValue):
    if not isinstance(stringValue, str):
        return stringValue

    stringValue = stringValue[1:-1]
    
    result = stringValue.split(", ")
    
    return np.array([float(result[0]), float(result[1]), float(result[2])], dtype=float)


if __name__ == '__main__':
    unittest.main()
