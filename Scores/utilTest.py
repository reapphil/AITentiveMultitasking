import unittest
import util as u
import math


class UtilTest(unittest.TestCase):
    data = None


    @classmethod
    def setUpClass(cls):
        cls.data = [[
                     [{0: 5, 1: 12}, {0: 10}],
                     [{0: 1, 1: 2, 2: 3}, {}]
                    ],
                    [
                     [{}, {}],
                     [{}, {0: 5, 1: 10}]
                    ]
                    ]


    def test_aggregateMax(self):
        self.assertEqual(u.aggregate(self.data, max, default=0), 12)
        self.assertEqual(u.aggregate(self.data, max, aggregateKey=True, default=0), 2)

    
    def test_aggregateSum(self):
        self.assertEqual(u.aggregate(self.data, sum), 48)
        self.assertEqual(u.aggregate(self.data, sum, aggregateKey=True), 5)


    def test_flatten(self):
        data = [
                    [
                     [{0: 5, 1: 12}, {0: 10}],
                     [{0: 1, 1: 2, 2: 3}, {}]
                    ],
                    [
                     [{}, {}],
                     [{}, {0: 5, 1: 10}]
                    ]
                ]
        
        self.assertEqual(u.flatten(data, 2), [5, 12, math.nan,
                                                    10, math.nan, math.nan,
                                                    1, 2, 3,
                                                    math.nan, math.nan, math.nan,
                                                    math.nan, math.nan, math.nan,
                                                    math.nan, math.nan, math.nan,
                                                    math.nan, math.nan, math.nan,
                                                    5, 10, math.nan])


    def test_intersect(self):
        d1 =    [
                    [
                     [{1: 12}, {0: 10}],
                     [{0: 1, 1: 2, 2: 3}, {}]
                    ],
                    [
                     [{}, {}],
                     [{}, {0: 5, 1: 10}]
                    ]
                ]
        
        d2 =    [
                    [
                     [{0: 5, 1: 12}, {0: 10}],
                     [{0: 1, 1: 2}, {}]
                    ],
                    [
                     [{}, {}],
                     [{}, {0: 5, 1: 10}]
                    ]
                ]
        
        d3 =    [
                    [
                     [{0: 5, 1: 12}, {0: 10}],
                     [{0: 1, 1: 2, 2: 3}, {}]
                    ],
                    [
                     [{}, {}],
                     [{}, {}]
                    ]
                ]
        
        r =    [
                    [
                     [{1: 12}, {0: 10}],
                     [{0: 1, 1: 2}, {}]
                    ],
                    [
                     [{}, {}],
                     [{}, {}]
                    ]
                ]
        
        self.assertEqual(u.intersect(d1, d2, d3), r)


if __name__ == '__main__':
    unittest.main()
