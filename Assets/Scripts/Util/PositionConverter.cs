using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public static class PositionConverter
{
    /// <summary>
    /// Discretization of discrete bin to Vector3 coordinates for a square platform.
    /// </summary>
    /// <param name="bin"></param>
    /// <param name="platformRadius"></param>
    /// <param name="binSize"></param>
    /// <param name="numberOFBinsPerDirection"></param>
    /// <param name="ballPositionY"></param>
    /// <returns>returns local position for the given bin</returns>
    public static Vector3 BinToCoordinates(int bin, float platformRadius, int numberOFBinsPerDirection, float ballPositionY)
    {
        float binSize = (platformRadius * 2) / numberOFBinsPerDirection;

        Vector3 coordinates = new Vector3(0, 0, 0);
        coordinates.x = ((int)(bin / numberOFBinsPerDirection) * binSize) - platformRadius + binSize / 2; //-platformRadiusX since the platform has a range from -platformRadius to +platformRadius (center at (0,0,0))
        coordinates.z = ((int)(bin % numberOFBinsPerDirection) * binSize) - platformRadius + binSize / 2;
        coordinates.y = ballPositionY;

        return coordinates;
    }

    /// <summary>
    /// Discretization of continuous Vector3 coordinates to discrete bin for a square platform. coordinates must have its zero value in the center of
    /// the platform.
    /// </summary>
    /// <param name="coordinates"></param>
    /// <param name="platformRadius"></param>
    /// <param name="binSize"></param>
    /// <param name="numberOFBinsPerDirection"></param>
    /// <returns>returns the bin of the given local position</returns>
    public static int CoordinatesToBin(Vector3 coordinates, float platformRadius, int numberOFBinsPerDirection)
    {
        Vector3 normCoordinates = new Vector3(coordinates.x + platformRadius, 0, coordinates.z + platformRadius);
        float localScale = platformRadius * 2;
        float binSize = localScale / numberOFBinsPerDirection;

        //check for e.g. >= 0 is necessary otherwise e.g. CoordinatesToBin(new Vector3(-5.1, 3.79, -4.78)) would still evaluate to 0 bin. -1 bin is ignored
        if (normCoordinates.x < 0 || normCoordinates.x >= localScale || normCoordinates.z < 0 || normCoordinates.z >= localScale)
        {
            return -1;
        }
        else
        {
            int discreteX = (int)(normCoordinates.x / binSize);
            int discreteZ = (int)(normCoordinates.z / binSize);

            return discreteX * numberOFBinsPerDirection + discreteZ;
        }
    }

    public static bool IsEdgeBin(int bin, int numberOFBinsPerDirection)
    {
        //first row && last row
        if (bin < numberOFBinsPerDirection || bin > (numberOFBinsPerDirection-1) * numberOFBinsPerDirection)
        {
            return true;
        }

        //first column && last column
        if (bin % numberOFBinsPerDirection == 0 || bin % numberOFBinsPerDirection == numberOFBinsPerDirection-1)
        {
            return true;
        }

        return false;
    }

    public static List<int> GetCrossedBinsList(Vector3 vector, int bin, int numberOFBinsPerDirection, float platformRadius)
    {
        //truncating of vector necessary because otherwise numberOFBinsPerDirection*2 samples could be insufficient to get all bins along the vector
        Vector3 cutVector = CutVector(vector, platformRadius);

        float numberOfVectorSamples = numberOFBinsPerDirection * 2f;

        List<int> result = new List<int>();

        Vector3 end = BinToCoordinates(bin, platformRadius, numberOFBinsPerDirection, 0);

        for (int i = 0; i <= (int)numberOfVectorSamples; i++)
        {
            int crossedBin = CoordinatesToBin(new Vector3(end.x + (i / numberOfVectorSamples) * cutVector.x, 0, end.z + (i / numberOfVectorSamples) * cutVector.z), platformRadius, numberOFBinsPerDirection);

            if (!result.Contains(crossedBin) && crossedBin != -1 && crossedBin != bin)
            {
                result.Add(crossedBin);
            }
        }

        return result;
    }

    public static NativeList<int> GetCrossedBinsNativeList(Vector3 vector, int bin, int numberOFBinsPerDirection, float platformRadius)
    {
        //truncating of vector necessary because otherwise numberOFBinsPerDirection*2 samples could be insufficient to get all bins along the vector
        Vector3 cutVector = CutVector(vector, platformRadius);

        float numberOfVectorSamples = numberOFBinsPerDirection * 1.5f;

        NativeList<int> result = new NativeList<int>(Allocator.Temp);

        Vector3 end = BinToCoordinates(bin, platformRadius, numberOFBinsPerDirection, 0);

        for (int i = 0; i <= (int)numberOfVectorSamples; i++)
        {
            int crossedBin = CoordinatesToBin(new Vector3(end.x + (i / numberOfVectorSamples) * cutVector.x, 0, end.z + (i / numberOfVectorSamples) * cutVector.z), platformRadius, numberOFBinsPerDirection);

            if (!result.Contains(crossedBin) && crossedBin != -1 && crossedBin != bin)
            {
                result.Add(crossedBin);
            }
        }

        return result;
    }

    /// <summary>
    /// Discretization of continuous Vector to discrete bin
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="vectorRange"></param>
    /// <param name="numberOfBinsPerAxis"></param>
    /// <param name="minVector"></param>
    /// <returns>returns the bin of the given vector</returns>
    public static int RangeVectorToBin(Vector3 vector, Vector3 vectorRange, int numberOfBinsPerAxis, Vector3 minVector)
    {
        float binSizeX = vectorRange.x / (float)numberOfBinsPerAxis;
        float binSizeY = vectorRange.y / (float)numberOfBinsPerAxis;
        float binSizeZ = vectorRange.z / (float)numberOfBinsPerAxis;

        Vector3 normVector = new Vector3(vector.x - minVector.x, vector.y - minVector.y, vector.z - minVector.z);

        int discreteX = (int)(normVector.x / binSizeX);
        int discreteY = (int)(normVector.y / binSizeY);
        int discreteZ = (int)(normVector.z / binSizeZ);

        CheckIfVectorInRange(vector, minVector, vectorRange);

        if (vectorRange.y == 0)
        {
            return discreteX * numberOfBinsPerAxis + discreteZ;
        }

        int bin = discreteX * (int)Math.Pow(numberOfBinsPerAxis, 2) + discreteY * (int)Math.Pow(numberOfBinsPerAxis, 1) + discreteZ;

        return ResolveBinOutOfRange(vector, bin, vectorRange, (int)Math.Pow(numberOfBinsPerAxis, 3), "RangeVectorToBin");
    }

    public static Vector3 BinToRangeVector(int bin, Vector3 vectorRange, int numberOfBinsPerAxis, Vector3 minVector)
    {
        float binSizeX = vectorRange.x / (float)numberOfBinsPerAxis;
        float binSizeY = vectorRange.y / (float)numberOfBinsPerAxis;
        float binSizeZ = vectorRange.z / (float)numberOfBinsPerAxis;

        float x = (((int)(bin / Math.Pow(numberOfBinsPerAxis, 2)) * binSizeX) + minVector.x) + binSizeX / 2;
        float y = (((int)(bin / Math.Pow(numberOfBinsPerAxis, 1)) % numberOfBinsPerAxis) * binSizeY) + minVector.y + binSizeY / 2;
        float z = (bin % numberOfBinsPerAxis) * binSizeZ + minVector.z + binSizeZ / 2;

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Discretization of continuous float to discrete bin. Range starts at 0.
    /// </summary>
    /// <param name="distance"></param>
    /// <param name="range"></param>
    /// <param name="numberOfBins"></param>
    /// <returns>returns the bin of the given float</returns>
    public static int ContinuousValueToBin(float value, float range, int numberOfBins)
    {
        float binSize = range / numberOfBins;
        int bin = (int)(value / binSize);

        return ResolveBinOutOfRange(value, bin, range, numberOfBins, "ContinuousValueToBin");
    }

    /// <summary>
    ///  Discrete bin to continuous float. Range starts at 0.
    /// </summary>
    /// <param name="bin"></param>
    /// <param name="range"></param>
    /// <param name="numberOfBins"></param>
    /// <returns>returns the float of the given bin/returns>
    public static float BinToContinuousValue(int bin, float range, int numberOfBins)
    {
        float binSize = range / numberOfBins;

        return bin * binSize + binSize / 2;
    }

    /// <summary>
    /// Discretization of continuous float to discrete bin
    /// </summary>
    /// <param name="distance"></param>
    /// <param name="range"></param>
    /// <param name="numberOfBins"></param>
    /// <returns>returns the bin of the given float</returns>
    public static int ContinuousValueToBin(float value, float min, float max, int numberOfBins)
    {
        if (numberOfBins == 1)
        {
            return 0;
        }

        if (value > max)
        {
            return numberOfBins - 1;
        }

        float range = max - min;

        float binSize = range / numberOfBins;

        int bin = (int)((value - min) / binSize);

        return ResolveBinOutOfRange(value, bin, range, numberOfBins, "ContinuousValueToBin");
    }

    /// <summary>
    ///  Discrete bin to continuous float
    /// </summary>
    /// <param name="bin"></param>
    /// <param name="range"></param>
    /// <param name="numberOfBins"></param>
    /// <returns>returns the float of the given bin/returns>
    public static float BinToContinuousValue(int bin, float min, float max, int numberOfBins)
    {
        float range = max - min;

        float binSize = range / numberOfBins;

        return (bin * binSize + binSize / 2) + min;
    }

    /// <summary>
    ///  Discrete vector to bin
    /// </summary>
    /// <param name="vector2"></param>
    /// <param name="x_range"></param>
    /// <returns>returns the index of the given vector/returns>
    public static int DiscreteVectorToBin(Vector2Int vector2, int x_range)
    {
        return vector2.x + (vector2.y * x_range);
    }


    private static Vector3 CutVector(Vector3 vector, float platformRadius)
    {
        float diameter = platformRadius * 2;

        float max = Math.Max(Math.Abs(vector.x), Math.Abs(vector.z));

        if (max > diameter)
        {
            return new Vector3(vector.x * (diameter / max), 0, vector.z * (diameter / max));
        }

        return vector;
    }

    private static int ResolveBinOutOfRange<T>(T value, int bin, T range, int numberOfBins, string functionName)
    {
        if (bin < 0)
        {
            Debug.LogWarning(string.Format("Bin out of range: value={0}, bin={1}, numberOfBins={2}, range={3}, functionName={4}", value, bin, numberOfBins, range, functionName));

            return 0;
        }
        else if(bin >= numberOfBins)
        {
            Debug.LogWarning(string.Format("Bin out of range: value={0}, bin={1}, numberOfBins={2}, range={3}, functionName={4}", value, bin, numberOfBins, range, functionName));

            return numberOfBins - 1;
        }

        return bin;
    }

    private static void CheckIfVectorInRange(Vector3 vector, Vector3 minVector, Vector3 vectorRange)
    {
        if (!(Mathf.Approximately(vector.x, vectorRange.x + minVector.x) || vector.x < vectorRange.x + minVector.x) ||
            !(Mathf.Approximately(vector.z, vectorRange.z + minVector.z) || vector.z < vectorRange.z + minVector.z) ||
            !(Mathf.Approximately(vector.x, minVector.x) || vector.x > minVector.x) ||
            !(Mathf.Approximately(vector.z, minVector.z) || vector.z > minVector.z))
        {
            Debug.LogWarning(string.Format("Vector is not in defined range: minVector: {0}, vectorRange:{1}, vector: {2}", minVector, vectorRange, vector));
        }
    }
}