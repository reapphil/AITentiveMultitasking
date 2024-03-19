using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PositionConverterTest
{
    private float _platformRadius;
    private int _numberOFBinsPerDirection;


    [SetUp]
    public void Initialize()
    {
    }

    [Test]
    public void BinToCoordinatesCoordinatesToBinTest()
    {
        _numberOFBinsPerDirection = 15;
        _platformRadius = 5;

        int bin = 0;
        Vector3 coordinates = PositionConverter.BinToCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.CoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 3;
        coordinates = PositionConverter.BinToCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.CoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 20;
        coordinates = PositionConverter.BinToCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.CoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 100;
        coordinates = PositionConverter.BinToCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.CoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 224;
        coordinates = PositionConverter.BinToCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.CoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        _numberOFBinsPerDirection = 19;
        _platformRadius = 23;

        bin = 0;
        coordinates = PositionConverter.BinToCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.CoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 3;
        coordinates = PositionConverter.BinToCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.CoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 20;
        coordinates = PositionConverter.BinToCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.CoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 100;
        coordinates = PositionConverter.BinToCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.CoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 224;
        coordinates = PositionConverter.BinToCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.CoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));
    }

    [Test]
    public void IsEdgeBinTest()
    {
        _numberOFBinsPerDirection = 5;

        //first row
        Assert.IsTrue(PositionConverter.IsEdgeBin(0, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsEdgeBin(2, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsEdgeBin(4, _numberOFBinsPerDirection));

        //last row
        Assert.IsTrue(PositionConverter.IsEdgeBin(20, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsEdgeBin(22, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsEdgeBin(24, _numberOFBinsPerDirection));

        //first column
        Assert.IsTrue(PositionConverter.IsEdgeBin(5, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsEdgeBin(10, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsEdgeBin(15, _numberOFBinsPerDirection));

        //last column
        Assert.IsTrue(PositionConverter.IsEdgeBin(9, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsEdgeBin(14, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsEdgeBin(19, _numberOFBinsPerDirection));

        //no edge columns
        Assert.IsFalse(PositionConverter.IsEdgeBin(6, _numberOFBinsPerDirection));
        Assert.IsFalse(PositionConverter.IsEdgeBin(12, _numberOFBinsPerDirection));
        Assert.IsFalse(PositionConverter.IsEdgeBin(18, _numberOFBinsPerDirection));
        Assert.IsFalse(PositionConverter.IsEdgeBin(16, _numberOFBinsPerDirection));
        Assert.IsFalse(PositionConverter.IsEdgeBin(8, _numberOFBinsPerDirection));
    }

    //            2,5
    //            z -->
    //       20  21  22  23  24
    //     x 15  16  17  18  19
    //-2,5 | 10  11  12  13  14  2,5
    //     v 5   6   7   8   9
    //       0   1   2   3   4
    //            -2,5
    [Test]
    public void CrossedBinsTest()
    {
        _numberOFBinsPerDirection = 5;
        _platformRadius = 2.5f;

        Vector3 v1 = new Vector3(2, 0, 1);
        int bin = 1;

        int[] array = PositionConverter.GetCrossedBinsList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(3, array.Length);
        Assert.AreEqual(6, array[0]);
        Assert.AreEqual(7, array[1]);
        Assert.AreEqual(12, array[2]);


        v1 = new Vector3(4, 0, 4);
        bin = 0;

        array = PositionConverter.GetCrossedBinsList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(4, array.Length);
        Assert.AreEqual(6, array[0]);
        Assert.AreEqual(12, array[1]);
        Assert.AreEqual(18, array[2]);
        Assert.AreEqual(24, array[3]);
    }

    [Test]
    public void CrossedBinsNegativeVelocityTest()
    {
        _numberOFBinsPerDirection = 5;
        _platformRadius = 2.5f;

        Vector3 v1 = new Vector3(-2, 0, -2);
        int bin = 23;

        int[] array = PositionConverter.GetCrossedBinsList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(2, array.Length);
        Assert.AreEqual(17, array[0]);
        Assert.AreEqual(11, array[1]);


        v1 = new Vector3(-4, 0, 2);
        bin = 21;

        array = PositionConverter.GetCrossedBinsList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(6, array.Length);
        Assert.AreEqual(16, array[0]);
        Assert.AreEqual(17, array[1]);
        Assert.AreEqual(12, array[2]);
        Assert.AreEqual(7, array[3]);
        Assert.AreEqual(8, array[4]);
        Assert.AreEqual(3, array[5]);
    }

    [Test]
    public void CrossedBinsOutOfAreaTest()
    {
        _numberOFBinsPerDirection = 5;
        _platformRadius = 2.5f;

        Vector3 v1 = new Vector3(100, 0, 100);
        int bin = 0;

        int[] array = PositionConverter.GetCrossedBinsList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(4, array.Length);
        Assert.AreEqual(6, array[0]);
        Assert.AreEqual(12, array[1]);
        Assert.AreEqual(18, array[2]);
        Assert.AreEqual(24, array[3]);
    }

    [Test]
    public void CrossedBinsOutOfAreaUndiagonalTest()
    {
        _numberOFBinsPerDirection = 5;
        _platformRadius = 2.5f;

        Vector3 v1 = new Vector3(20, 0, 10);
        int bin = 1;

        int[] array = PositionConverter.GetCrossedBinsList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(6, array.Length);
        Assert.AreEqual(6, array[0]);
        Assert.AreEqual(7, array[1]);
        Assert.AreEqual(12, array[2]);
        Assert.AreEqual(17, array[3]);
        Assert.AreEqual(18, array[4]);
        Assert.AreEqual(23, array[5]);
    }

    [Test]
    public void CrossedBinsNativeTest()
    {
        _numberOFBinsPerDirection = 5;
        _platformRadius = 2.5f;

        Vector3 v1 = new Vector3(2, 0, 1);
        int bin = 1;

        int[] array = PositionConverter.GetCrossedBinsNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(3, array.Length);
        Assert.AreEqual(6, array[0]);
        Assert.AreEqual(7, array[1]);
        Assert.AreEqual(12, array[2]);


        v1 = new Vector3(4, 0, 4);
        bin = 0;

        array = PositionConverter.GetCrossedBinsNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(4, array.Length);
        Assert.AreEqual(6, array[0]);
        Assert.AreEqual(12, array[1]);
        Assert.AreEqual(18, array[2]);
        Assert.AreEqual(24, array[3]);
    }

    [Test]
    public void CrossedBinsNegativeVelocityNativeTest()
    {
        _numberOFBinsPerDirection = 5;
        _platformRadius = 2.5f;

        Vector3 v1 = new Vector3(-2, 0, -2);
        int bin = 23;

        int[] array = PositionConverter.GetCrossedBinsNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(2, array.Length);
        Assert.AreEqual(17, array[0]);
        Assert.AreEqual(11, array[1]);


        v1 = new Vector3(-4, 0, 2);
        bin = 21;

        array = PositionConverter.GetCrossedBinsNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(6, array.Length);
        Assert.AreEqual(16, array[0]);
        Assert.AreEqual(17, array[1]);
        Assert.AreEqual(12, array[2]);
        Assert.AreEqual(7, array[3]);
        Assert.AreEqual(8, array[4]);
        Assert.AreEqual(3, array[5]);
    }

    [Test]
    public void CrossedBinsOutOfAreaNativeTest()
    {
        _numberOFBinsPerDirection = 5;
        _platformRadius = 2.5f;

        Vector3 v1 = new Vector3(100, 0, 100);
        int bin = 0;

        int[] array = PositionConverter.GetCrossedBinsNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(4, array.Length);
        Assert.AreEqual(6, array[0]);
        Assert.AreEqual(12, array[1]);
        Assert.AreEqual(18, array[2]);
        Assert.AreEqual(24, array[3]);
    }

    [Test]
    public void CrossedBinsOutOfAreaUndiagonalNativeTest()
    {
        _numberOFBinsPerDirection = 5;
        _platformRadius = 2.5f;

        Vector3 v1 = new Vector3(20, 0, 10);
        int bin = 1;

        int[] array = PositionConverter.GetCrossedBinsNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(6, array.Length);
        Assert.AreEqual(6, array[0]);
        Assert.AreEqual(7, array[1]);
        Assert.AreEqual(12, array[2]);
        Assert.AreEqual(17, array[3]);
        Assert.AreEqual(18, array[4]);
        Assert.AreEqual(23, array[5]);
    }

    [Test]
    public void CoordinatesToBinUniquenessTest()
    {
        HashSet<int> hashSet = new HashSet<int>();

        _numberOFBinsPerDirection = 3;
        _platformRadius = 5;
        float binSize = (_platformRadius * 2) / _numberOFBinsPerDirection;

        Vector3[] coordinates = new Vector3[]
        {
            new Vector3(-_platformRadius + (binSize/2), 0, -_platformRadius + (binSize/2)),
            new Vector3(-_platformRadius + (binSize/2), 0, -_platformRadius + (binSize/2) + binSize),
            new Vector3(-_platformRadius + (binSize/2), 0, -_platformRadius + (binSize/2) + binSize*2),
            new Vector3(-_platformRadius + (binSize/2) + binSize, 0, -_platformRadius + (binSize/2)),
            new Vector3(-_platformRadius + (binSize/2) + binSize, 0, -_platformRadius + (binSize/2) + binSize),
            new Vector3(-_platformRadius + (binSize/2) + binSize, 0, -_platformRadius + (binSize/2) + binSize*2),
            new Vector3(-_platformRadius + (binSize/2) + binSize*2, 0, -_platformRadius + (binSize/2)),
            new Vector3(-_platformRadius + (binSize/2) + binSize*2, 0, -_platformRadius + (binSize/2) + binSize),
            new Vector3(-_platformRadius + (binSize/2) + binSize*2, 0, -_platformRadius + (binSize/2) + binSize*2),
        };

        foreach (Vector3 coordinate in coordinates)
        {
            int bin = PositionConverter.CoordinatesToBin(coordinate, _platformRadius, _numberOFBinsPerDirection);

            Assert.Less(bin, _numberOFBinsPerDirection * _numberOFBinsPerDirection);
            Assert.GreaterOrEqual(bin, 0);
            Assert.True(hashSet.Add(bin), string.Format("Bin {0} is already in set (_numberOFBinsPerDirection: {1}, _platformRadius: {2}, coordinates: {3})", bin, _numberOFBinsPerDirection, _platformRadius, coordinate));
        }

        Assert.AreEqual(hashSet.Count, _numberOFBinsPerDirection * _numberOFBinsPerDirection);
    }

    [Test]
    public void RangeVectorToBinUniquenessTest()
    {
        HashSet<int> hashSet = new HashSet<int>();

        Vector3 minVector = new Vector3(-5, -2.5f, -10);
        Vector3 vectorRange = new Vector3(10, 5, 20);
        int numberOfBinsPerAxis = 2;

        float binSizeX = vectorRange.x / (float)numberOfBinsPerAxis;
        float binSizeY = vectorRange.y / (float)numberOfBinsPerAxis;
        float binSizeZ = vectorRange.z / (float)numberOfBinsPerAxis;

        Vector3[] coordinates = new Vector3[]
        {
            new Vector3(minVector.x + (binSizeX/2),
                        minVector.y + (binSizeY/2),
                        minVector.z + (binSizeZ/2)),
            new Vector3(minVector.x + (binSizeX/2),
                        minVector.y + (binSizeY/2) + binSizeY,
                        minVector.z + (binSizeZ/2)),
            new Vector3(minVector.x + (binSizeX/2),
                        minVector.y + (binSizeY/2),
                        minVector.z + (binSizeZ/2) + binSizeZ),
            new Vector3(minVector.x + (binSizeX/2) + binSizeX,
                        minVector.y + (binSizeY/2),
                        minVector.z + (binSizeZ/2)),
            new Vector3(minVector.x + (binSizeX/2) + binSizeX,
                        minVector.y + (binSizeY/2) + binSizeY,
                        minVector.z + (binSizeZ/2)),
            new Vector3(minVector.x + (binSizeX/2) + binSizeX,
                        minVector.y + (binSizeY/2),
                        minVector.z + (binSizeZ/2) + binSizeZ),
            new Vector3(minVector.x + (binSizeX/2),
                        minVector.y + (binSizeY/2) + binSizeY,
                        minVector.z + (binSizeZ/2) + binSizeZ),
            new Vector3(minVector.x + (binSizeX/2) + binSizeX,
                        minVector.y + (binSizeY/2) + binSizeY,
                        minVector.z + (binSizeZ/2) + binSizeZ)
        };

        for (int i = 0; i < coordinates.Length; i++)
        {
            int bin = PositionConverter.RangeVectorToBin(coordinates[i], vectorRange, numberOfBinsPerAxis, minVector);

            Assert.Less(bin, Math.Pow(numberOfBinsPerAxis, 3), string.Format("Bin {0} is greater than {1} (coordinates: {2})", bin, Math.Pow(numberOfBinsPerAxis, 3), coordinates[i]));
            Assert.GreaterOrEqual(bin, 0);
            Assert.True(hashSet.Add(bin), string.Format("Bin {0} is already in set (numberOfBinsPerAxis: {1}, vectorRange: {2}, minVector: {3}, coordinates: {4})", bin, numberOfBinsPerAxis, vectorRange, minVector, coordinates[i]));
        }


        Assert.AreEqual(hashSet.Count, Math.Pow(numberOfBinsPerAxis, 3));
    }

    [Test]
    public void RangeVectorToBinUniquenessTestMissingY()
    {
        HashSet<int> hashSet = new HashSet<int>();

        Vector3 minVector = new Vector3(-5, 0, -10);
        Vector3 vectorRange = new Vector3(10, 0, 20);
        int numberOfBinsPerAxis = 2;

        float binSizeX = vectorRange.x / (float)numberOfBinsPerAxis;
        float binSizeZ = vectorRange.z / (float)numberOfBinsPerAxis;

        Vector3[] coordinates = new Vector3[]
        {
            new Vector3(minVector.x + (binSizeX/2),
                        0,
                        minVector.z + (binSizeZ/2)),
            new Vector3(minVector.x + (binSizeX/2) + binSizeX,
                        0,
                        minVector.z + (binSizeZ/2)),
            new Vector3(minVector.x + (binSizeX/2),
                        0,
                        minVector.z + (binSizeZ/2) + binSizeZ),
            new Vector3(minVector.x + (binSizeX/2) + binSizeX,
                        0,
                        minVector.z + (binSizeZ/2) + binSizeZ)
        };

        foreach (Vector3 coordinate in coordinates)
        {
            int bin = PositionConverter.RangeVectorToBin(coordinate, vectorRange, numberOfBinsPerAxis, minVector);

            Assert.Less(bin, Math.Pow(numberOfBinsPerAxis, 2), string.Format("Bin {0} is greater than {1} (coordinates: {2})", bin, Math.Pow(numberOfBinsPerAxis, 2), coordinate));
            Assert.GreaterOrEqual(bin, 0);
            Assert.True(hashSet.Add(bin), string.Format("Bin {0} is already in set (numberOfBinsPerAxis: {1}, vectorRange: {2}, minVector: {3}, coordinates: {4})", bin, numberOfBinsPerAxis, vectorRange, minVector, coordinate));
        }


        Assert.AreEqual(hashSet.Count, Math.Pow(numberOfBinsPerAxis, 2));
    }

    [Test]
    public void CommutativePropertyOfDistanceTest()
    {
        Assert.AreEqual(Vector3.Distance(new Vector3(2, 3, 4), new Vector3(6, 2, 5)), Vector3.Distance(new Vector3(6, 2, 5), new Vector3(2, 3, 4)));
    }

    [Test]
    public void BinToRangeVectorRangeVectorToBinTest()
    {
        Vector3 minVector = new Vector3(-5, -2.5f, -10);
        Vector3 vectorRange = new Vector3(10, 5, 20);
        int numberOfBinsPerAxis = 2;

        int bin = 5;

        Vector3 v = PositionConverter.BinToRangeVector(bin, vectorRange, numberOfBinsPerAxis, minVector);
        int result_bin = PositionConverter.RangeVectorToBin(v, vectorRange, numberOfBinsPerAxis, minVector);

        Assert.AreEqual(bin, result_bin);
    }

    [Test]
    public void BinToRangeVectorRangeVectorToBinAsymmetricRangeTest()
    {
        Vector3 minVector = new Vector3(-5, -2.5f, -10);
        Vector3 vectorRange = new Vector3(17, 25, 30);
        int numberOfBinsPerAxis = 7;

        int bin = 12;

        Vector3 v = PositionConverter.BinToRangeVector(bin, vectorRange, numberOfBinsPerAxis, minVector);
        int result_bin = PositionConverter.RangeVectorToBin(v, vectorRange, numberOfBinsPerAxis, minVector);

        Assert.AreEqual(bin, result_bin);
    }

    [Test]
    public void BinToRangeVectorRangeVectorToBinAsymmetricRangeMinTest()
    {
        Vector3 minVector = new Vector3(-5, -2.5f, -10);
        Vector3 vectorRange = new Vector3(17, 25, 30);
        int numberOfBinsPerAxis = 7;

        int bin = 0;

        Vector3 v = PositionConverter.BinToRangeVector(bin, vectorRange, numberOfBinsPerAxis, minVector);
        int result_bin = PositionConverter.RangeVectorToBin(v, vectorRange, numberOfBinsPerAxis, minVector);

        Assert.AreEqual(bin, result_bin);
    }

    [Test]
    public void RangeVectorToBinBinToRangeVectorTest()
    {
        Vector3 minVector = new Vector3(-5, -2.5f, -10);
        Vector3 vectorRange = new Vector3(10, 5, 20);
        int numberOfBinsPerAxis = 10;

        Vector3 v = new Vector3(4.5f, 2.25f, 9f);

        int result_bin = PositionConverter.RangeVectorToBin(v, vectorRange, numberOfBinsPerAxis, minVector);
        Vector3 result_v = PositionConverter.BinToRangeVector(result_bin, vectorRange, numberOfBinsPerAxis, minVector);

        Assert.AreEqual(v, result_v);
    }

    [Test]
    public void BinToContinuousValueToBinTest()
    {
        int bin = 6;
        float range = 9;
        int numberOfBins = 7;

        float distance = PositionConverter.BinToContinuousValue(bin, range, numberOfBins);
        int result_bin = PositionConverter.ContinuousValueToBin(distance, range, numberOfBins);

        Assert.AreEqual(bin, result_bin);
    }

    [Test]
    public void BinToContinuousValueToBinTest2()
    {
        int bin = 6;
        float min = 0;
        float max = 1;
        int numberOfBins = 10;


        float distance = PositionConverter.BinToContinuousValue(bin, min, max, numberOfBins);
        int result_bin = PositionConverter.ContinuousValueToBin(distance, min, max, numberOfBins);

        Assert.AreEqual(bin, result_bin);
    }

    [Test]
    public void BinToContinuousValueNumBin1Test2()
    {
        int bin = 0;
        float min = 0;
        float max = 9;
        int numberOfBins = 1;

        float distance = PositionConverter.BinToContinuousValue(bin, min, max, numberOfBins);

        Assert.AreEqual(4.5, distance);


        min = 0;
        max = 10;
        distance = PositionConverter.BinToContinuousValue(bin, min, max, numberOfBins);

        Assert.AreEqual(5, distance);
    }

    [Test]
    public void ContinuousValueToBinNumBin1Test2()
    {
        
        float min = 0;
        float max = 9;
        int numberOfBins = 1;

        int bin = PositionConverter.ContinuousValueToBin(5, min, max, numberOfBins);

        Assert.AreEqual(0, bin);
    }

    [Test]
    public void DiscreteVectorToBinTest()
    {
        int bin = PositionConverter.DiscreteVectorToBin(new Vector2Int(0, 0), 8);
        Assert.AreEqual(0, bin);

        bin = PositionConverter.DiscreteVectorToBin(new Vector2Int(5, 0), 8);
        Assert.AreEqual(5, bin);

        bin = PositionConverter.DiscreteVectorToBin(new Vector2Int(7, 0), 8);
        Assert.AreEqual(7, bin);

        bin = PositionConverter.DiscreteVectorToBin(new Vector2Int(5, 1), 8);
        Assert.AreEqual(13, bin);

        bin = PositionConverter.DiscreteVectorToBin(new Vector2Int(7, 7), 8);
        Assert.AreEqual(63, bin);
    }
}
