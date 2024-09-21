using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
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
    public void GetBinsInsideRectTest()
    {
        //Rect size:    20x10
        //Bin size:     1x1
        //Bins are counted row-vise from left to right and bottom to top. The outer rectangle is centered at (0,0), therefore
        //rectCenter=Vector2(0.0f, 0.0f) and rectSize=Vector2(0.9f, 0.9f) returns 94, 95, 104, 105.
        float rectangleWidth = 20;
        float rectangleHeight = 10;
        int numberOfBins = 200;

        Vector2 rectCenter = new Vector2(-4.0f, -4.0f);
        Vector2 rectSize = new Vector2(0.9f, 0.9f);
        List<int> bins = PositionConverter.GetBinsInsideRect(rectCenter, rectSize, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.That(bins, Is.EquivalentTo(new List<int> { 50, 51, 60, 61 }));

        rectCenter = new Vector2(-2f, 1.5f);
        rectSize = new Vector2(3.9f, 2.9f);
        bins = PositionConverter.GetBinsInsideRect(rectCenter, rectSize, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.That(bins, Is.EquivalentTo(new List<int> { 65, 66, 67, 75, 76, 77, 85, 86, 87, 95, 96, 97 }));
    }

    [Test]
    public void BinToCoordinatesCoordinatesToBinTest()
    {
        _numberOFBinsPerDirection = 15;
        _platformRadius = 5;

        int bin = 0;
        Vector3 coordinates = PositionConverter.BinToSquareCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.SquareCoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 3;
        coordinates = PositionConverter.BinToSquareCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.SquareCoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 20;
        coordinates = PositionConverter.BinToSquareCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.SquareCoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 100;
        coordinates = PositionConverter.BinToSquareCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.SquareCoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 224;
        coordinates = PositionConverter.BinToSquareCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.SquareCoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        _numberOFBinsPerDirection = 19;
        _platformRadius = 23;

        bin = 0;
        coordinates = PositionConverter.BinToSquareCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.SquareCoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 3;
        coordinates = PositionConverter.BinToSquareCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.SquareCoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 20;
        coordinates = PositionConverter.BinToSquareCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.SquareCoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 100;
        coordinates = PositionConverter.BinToSquareCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.SquareCoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));

        bin = 224;
        coordinates = PositionConverter.BinToSquareCoordinates(bin, _platformRadius, _numberOFBinsPerDirection, 0);
        Assert.AreEqual(bin, PositionConverter.SquareCoordinatesToBin(coordinates, _platformRadius, _numberOFBinsPerDirection));
    }

    [Test]
    public void BinToRectangleCoordinatesCoordinatesToBinTest()
    {
        int numberOfBins = 500;
        float rectangleHeight = 5;
        float rectangleWidth = 10;


        int bin = 0;
        Vector3 coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 3;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 20;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 100;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 224;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 490;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 495;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));


        numberOfBins = 666;
        rectangleHeight = 10;
        rectangleWidth = 10;

        bin = 0;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 3;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 20;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 100;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 224;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 649;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));


        numberOfBins = 1000;
        rectangleHeight = 9;
        rectangleWidth = 17;

        bin = 0;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 3;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 20;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 100;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 224;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));


        numberOfBins = 500;
        rectangleHeight = 500;
        rectangleWidth = 1000;

        bin = 0;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 3;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 20;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 100;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));

        bin = 495;
        coordinates = PositionConverter.BinToRectangleCoordinates(bin, rectangleWidth, rectangleHeight, numberOfBins);
        Assert.AreEqual(bin, PositionConverter.RectangleCoordinatesToBin(coordinates, rectangleWidth, rectangleHeight, numberOfBins));
    }

    [Test]
    public void IsEdgeBinTest()
    {
        _numberOFBinsPerDirection = 5;

        //first row
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(0, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(2, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(4, _numberOFBinsPerDirection));

        //last row
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(20, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(22, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(24, _numberOFBinsPerDirection));

        //first column
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(5, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(10, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(15, _numberOFBinsPerDirection));

        //last column
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(9, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(14, _numberOFBinsPerDirection));
        Assert.IsTrue(PositionConverter.IsSquareEdgeBin(19, _numberOFBinsPerDirection));

        //no edge columns
        Assert.IsFalse(PositionConverter.IsSquareEdgeBin(6, _numberOFBinsPerDirection));
        Assert.IsFalse(PositionConverter.IsSquareEdgeBin(12, _numberOFBinsPerDirection));
        Assert.IsFalse(PositionConverter.IsSquareEdgeBin(18, _numberOFBinsPerDirection));
        Assert.IsFalse(PositionConverter.IsSquareEdgeBin(16, _numberOFBinsPerDirection));
        Assert.IsFalse(PositionConverter.IsSquareEdgeBin(8, _numberOFBinsPerDirection));
    }

    [Test]
    public void IsRectangleEdgeBinTest()
    {
        int numberOfBins = 500;
        float rectangleHight = 500;
        float rectangleWidth = 1000;

        //first row
        int bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(-499.9f, 249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(-111f, 249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(0, 249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(111f, 249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(499.9f, 249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Debug.Log(bin);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));

        //last row
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(-499.9f, -249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(-111f, -249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(0, -249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(111f, -249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(499.9f, -249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));

        //first column
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(-499.9f, -249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(-499.9f, -100f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(-499.9f, 0f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(-499.9f, 100f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(-499.9f, 249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));

        //last column
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(499.9f, -249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(499.9f, -100f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(499.9f, 0f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(499.9f, 100f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(499.9f, 249.9f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsTrue(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));

        //no edge columns
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(0, 0f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsFalse(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(300, 200f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsFalse(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(-210, 100f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsFalse(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(240, 189f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsFalse(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
        bin = PositionConverter.RectangleCoordinatesToBin(new Vector2(441, -149f), rectangleWidth, rectangleHight, numberOfBins);
        Assert.IsFalse(PositionConverter.IsRectangleEdgeBin(bin, rectangleWidth, rectangleHight, numberOfBins));
    }

    [Test]
    public void GetBinDimensionsTest()
    {
        int numberOfBins = 500;
        float rectangleHeight = 500;
        float rectangleWidth = 1000;

        (int, int) binDimensions = PositionConverter.GetBinDimensions(rectangleWidth, rectangleHeight, numberOfBins);
        (int, int) binDimensionsNew = PositionConverter.GetBinDimensions(rectangleWidth, rectangleHeight, binDimensions.Item1 * binDimensions.Item2);
        Assert.AreEqual(binDimensions, binDimensionsNew);

        numberOfBins = 483;
        rectangleHeight = 500;
        rectangleWidth = 1000;

        binDimensions = PositionConverter.GetBinDimensions(rectangleWidth, rectangleHeight, numberOfBins);
        binDimensionsNew = PositionConverter.GetBinDimensions(rectangleWidth, rectangleHeight, binDimensions.Item1 * binDimensions.Item2);
        Assert.AreEqual(binDimensions, binDimensionsNew);
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
            int bin = PositionConverter.SquareCoordinatesToBin(coordinate, _platformRadius, _numberOFBinsPerDirection);

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
