using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CrossedBinTests
{
    private float _platformRadius;
    private int _numberOFBinsPerDirection;


    [Test]
    public void SingleBinCrossedTest()
    {
        // Arrange
        Vector2 velocity = new Vector2(0, 0); // No movement, stays in the same bin.
        int targetBin = 5;
        float rectangleWidth = 10f;
        float rectangleHeight = 10f;
        int numberOfBins = 9; // 3x3 grid.

        // Act
        List<int> result = PositionConverter.GetRectangleCrossedBins(velocity, targetBin, rectangleWidth, rectangleHeight, numberOfBins).ToArray().ToList();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(targetBin, result[0]);
    }

    [Test]
    public void MultipleBinsCrossedTest()
    {
        // Arrange
        Vector2 velocity = new Vector2(5, 5); // Diagonal movement.
        int targetBin = 8;
        float rectangleWidth = 9f;
        float rectangleHeight = 9f;
        int numberOfBins = 9; // 3x3 grid.

        // Act
        List<int> result = PositionConverter.GetRectangleCrossedBins(velocity, targetBin, rectangleWidth, rectangleHeight, numberOfBins).ToArray().ToList();

        // Assert
        Assert.IsTrue(result.Count > 1); // We expect it to cross multiple bins.
        Assert.Contains(8, result); // Target bin should be included.
        Assert.Contains(4, result); // Should cross through bin 4.
        Assert.Contains(0, result); // Should cross through bin 0 (starting point).
    }

    [Test]
    public void CrossingPlatformBoundariesTest()
    {
        // Arrange
        Vector2 velocity = new Vector2(10, 10); // Large velocity to move outside bounds.
        int targetBin = 8;
        float rectangleWidth = 10f;
        float rectangleHeight = 10f;
        int numberOfBins = 9; // 3x3 grid.

        // Act
        List<int> result = PositionConverter.GetRectangleCrossedBins(velocity, targetBin, rectangleWidth, rectangleHeight, numberOfBins).ToArray().ToList();

        // Assert
        Assert.IsTrue(result.Count > 1); // Should cross multiple bins.
        Assert.Contains(targetBin, result); // Target bin should be included.
        Assert.IsTrue(result[result.Count - 1] != targetBin); // Should have crossed other bins.
    }

    [Test]
    public void EdgeCaseMovementTest()
    {
        // Arrange
        Vector2 velocity = new Vector2(0, 10); // Movement vertically only.
        int targetBin = 5;
        float rectangleWidth = 10f;
        float rectangleHeight = 10f;
        int numberOfBins = 9; // 3x3 grid.

        // Act
        List<int> result = PositionConverter.GetRectangleCrossedBins(velocity, targetBin, rectangleWidth, rectangleHeight, numberOfBins).ToArray().ToList();

        // Assert
        Assert.IsTrue(result.Count > 1); // Should cross multiple bins.
        Assert.Contains(targetBin, result); // Target bin should be included.
    }


    //0  1  2  3  4  5  6  7  8  9
    [Test]
    public void CrossedBins1DTest()
    {
        int numberOfBins = 10;
        float rangeMin = -6.5f;
        float rangeMax = 6.5f;

        //13/10 = 1.3

        float v1 = 3;
        int bin = 0;

        int[] array = PositionConverter.GetCrossedBins1DList(v1, bin, numberOfBins, rangeMin, rangeMax).ToArray();
        Assert.AreEqual(2, array.Length);
        Assert.AreEqual(1, array[0]);
        Assert.AreEqual(2, array[1]);

        v1 = -4;
        bin = 9;

        array = PositionConverter.GetCrossedBins1DList(v1, bin, numberOfBins, rangeMin, rangeMax).ToArray();
        Assert.AreEqual(3, array.Length);
        Assert.AreEqual(8, array[0]);
        Assert.AreEqual(7, array[1]);
        Assert.AreEqual(6, array[2]);
    }

    [Test]
    public void CrossedBins1DNativeTest()
    {
        int numberOfBins = 10;
        float rangeMin = -6.5f;
        float rangeMax = 6.5f;

        //13/10 = 1.3

        float v1 = 3;
        int bin = 0;

        int[] array = PositionConverter.GetCrossedBins1DNativeList(v1, bin, numberOfBins, rangeMin, rangeMax).ToArray();
        Assert.AreEqual(2, array.Length);
        Assert.AreEqual(1, array[0]);
        Assert.AreEqual(2, array[1]);

        v1 = -4;
        bin = 9;

        array = PositionConverter.GetCrossedBins1DNativeList(v1, bin, numberOfBins, rangeMin, rangeMax).ToArray();
        Assert.AreEqual(3, array.Length);
        Assert.AreEqual(8, array[0]);
        Assert.AreEqual(7, array[1]);
        Assert.AreEqual(6, array[2]);
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

        int[] array = PositionConverter.GetCrossedBins2DList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(3, array.Length);
        Assert.AreEqual(6, array[0]);
        Assert.AreEqual(7, array[1]);
        Assert.AreEqual(12, array[2]);


        v1 = new Vector3(4, 0, 4);
        bin = 0;

        array = PositionConverter.GetCrossedBins2DList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
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

        int[] array = PositionConverter.GetCrossedBins2DList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(2, array.Length);
        Assert.AreEqual(17, array[0]);
        Assert.AreEqual(11, array[1]);


        v1 = new Vector3(-4, 0, 2);
        bin = 21;

        array = PositionConverter.GetCrossedBins2DList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
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

        int[] array = PositionConverter.GetCrossedBins2DList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
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

        int[] array = PositionConverter.GetCrossedBins2DList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
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

        int[] array = PositionConverter.GetCrossedBins2DNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(3, array.Length);
        Assert.AreEqual(6, array[0]);
        Assert.AreEqual(7, array[1]);
        Assert.AreEqual(12, array[2]);


        v1 = new Vector3(4, 0, 4);
        bin = 0;

        array = PositionConverter.GetCrossedBins2DNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
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

        int[] array = PositionConverter.GetCrossedBins2DNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(2, array.Length);
        Assert.AreEqual(17, array[0]);
        Assert.AreEqual(11, array[1]);


        v1 = new Vector3(-4, 0, 2);
        bin = 21;

        array = PositionConverter.GetCrossedBins2DNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
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

        int[] array = PositionConverter.GetCrossedBins2DNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
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

        int[] array = PositionConverter.GetCrossedBins2DNativeList(v1, bin, _numberOFBinsPerDirection, _platformRadius).ToArray();
        Assert.AreEqual(6, array.Length);
        Assert.AreEqual(6, array[0]);
        Assert.AreEqual(7, array[1]);
        Assert.AreEqual(12, array[2]);
        Assert.AreEqual(17, array[3]);
        Assert.AreEqual(18, array[4]);
        Assert.AreEqual(23, array[5]);
    }
}
