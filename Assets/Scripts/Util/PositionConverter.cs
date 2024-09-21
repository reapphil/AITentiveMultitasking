using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.GlobalIllumination;

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
    public static Vector3 BinToSquareCoordinates(int bin, float platformRadius, int numberOFBinsPerDirection, float ballPositionY)
    {
        float binSize = (platformRadius * 2) / numberOFBinsPerDirection;

        Vector3 coordinates = new Vector3(0, 0, 0);
        coordinates.x = ((int)(bin / numberOFBinsPerDirection) * binSize) - platformRadius + binSize / 2; //-platformRadiusX since the platform has a range from -platformRadius to +platformRadius (center at (0,0,0))
        coordinates.z = ((int)(bin % numberOFBinsPerDirection) * binSize) - platformRadius + binSize / 2;
        coordinates.y = ballPositionY;

        return coordinates;
    }

    /// <summary>
    /// Discretization of discrete bin to Vector2 coordinates for a rectangular platform.
    /// </summary>
    /// <param name="bin"></param>
    /// <param name="rectangleWidth"></param>
    /// <param name="rectangleHeight"></param>
    /// <param name="numberOfBins"></param>
    /// <returns>returns local position for the given bin</returns>
    public static Vector2 BinToRectangleCoordinates(int bin, float rectangleWidth, float rectangleHeight, int numberOfBins)
    {
        (int numberOFBinsPerWidth, int numberOFBinsPerHeight) = GetBinDimensions(rectangleWidth, rectangleHeight, numberOfBins);

        if (bin >= numberOFBinsPerHeight * numberOFBinsPerWidth)
        {
            Debug.LogError($"Bin {bin} is out of bounds (number of bins: {numberOFBinsPerWidth * numberOFBinsPerHeight}).");
        }

        // Calculate bin sizes along both axes
        float binSizeX = rectangleWidth / numberOFBinsPerWidth;
        float binSizeZ = rectangleHeight / numberOFBinsPerHeight;

        Vector2 coordinates = new Vector2(0, 0);

        // Compute the coordinates based on the bin index
        coordinates.x = (bin / numberOFBinsPerHeight) * binSizeX - (rectangleWidth / 2) + binSizeX / 2;
        coordinates.y = (bin % numberOFBinsPerHeight) * binSizeZ - (rectangleHeight / 2) + binSizeZ / 2;

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
    public static int SquareCoordinatesToBin(Vector3 coordinates, float platformRadius, int numberOFBinsPerDirection)
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

    /// <summary>
    /// Converts local coordinates to bin index for a rectangular platform.
    /// </summary>
    /// <param name="coordinates">The coordinates to convert.</param>
    /// <param name="rectangleWidth">The width of the platform.</param>
    /// <param name="rectangleHeight">The height of the platform.</param>
    /// <param name="numberOfBins">The total number of bins.</param>
    /// <returns>The bin index corresponding to the given coordinates.</returns>
    public static int RectangleCoordinatesToBin(Vector2 coordinates, float rectangleWidth, float rectangleHeight, int numberOfBins)
    {
        if (coordinates == null)
        {
            return -1;
        }

        (int numberOFBinsPerWidth, int numberOFBinsPerHeight) = GetBinDimensions(rectangleWidth, rectangleHeight, numberOfBins);

        // Calculate bin sizes along both axes
        float binSizeX = rectangleWidth / numberOFBinsPerWidth;
        float binSizeZ = rectangleHeight / numberOFBinsPerHeight;

        // Convert coordinates to bin row and column
        int column = Mathf.FloorToInt((coordinates.x + (rectangleWidth / 2)) / binSizeX);
        int row = Mathf.FloorToInt((coordinates.y + (rectangleHeight / 2)) / binSizeZ);

        // Clamp column and row to avoid out-of-bound indices
        column = Mathf.Clamp(column, 0, numberOFBinsPerWidth - 1);
        row = Mathf.Clamp(row, 0, numberOFBinsPerHeight - 1);

        // Compute the bin index using row and column
        int binIndex = column * numberOFBinsPerHeight + row;

        // Ensure binIndex does not exceed bounds (max value = numberOfBins - 1)
        return Mathf.Clamp(binIndex, 0, numberOfBins - 1);
    }


    public static bool IsSquareEdgeBin(int bin, int numberOFBinsPerDirection)
    {
        //first row && last row
        if (bin < numberOFBinsPerDirection || bin > (numberOFBinsPerDirection - 1) * numberOFBinsPerDirection)
        {
            return true;
        }

        //first column && last column
        if (bin % numberOFBinsPerDirection == 0 || bin % numberOFBinsPerDirection == numberOFBinsPerDirection - 1)
        {
            return true;
        }

        return false;
    }

    public static (int, int) GetBinDimensions(float rectangleWidth, float rectangleHeight, int numberOfBins)
    {
        float aspectRatio = rectangleWidth / rectangleHeight;

        // Initial approximation of bins along width and height
        int numberOFBinsPerWidthFloor = Mathf.FloorToInt(Mathf.Sqrt(numberOfBins * aspectRatio));
        int numberOFBinsPerWidthCeil = Mathf.CeilToInt(Mathf.Sqrt(numberOfBins * aspectRatio));

        // Calculate height bins based on floor and ceil widths
        int numberOFBinsPerHeightFloor = Mathf.FloorToInt((float)numberOfBins / numberOFBinsPerWidthFloor);
        int numberOFBinsPerHeightCeil = Mathf.CeilToInt((float)numberOfBins / numberOFBinsPerWidthCeil);

        // Calculate the total number of bins for both cases
        int totalBinsFloor = numberOFBinsPerWidthFloor * numberOFBinsPerHeightFloor;
        int totalBinsCeil = numberOFBinsPerWidthCeil * numberOFBinsPerHeightCeil;

        // Choose the configuration that is closest to the desired number of bins
        if (Math.Abs(totalBinsFloor - numberOfBins) <= Math.Abs(totalBinsCeil - numberOfBins))
        {
            return (numberOFBinsPerWidthFloor, numberOFBinsPerHeightFloor);
        }
        else
        {
            return (numberOFBinsPerWidthCeil, numberOFBinsPerHeightCeil);
        }
    }

    /// <summary>
    /// Determines if a given bin is on the edge of the rectangular platform.
    /// </summary>
    /// <param name="bin">The bin index to check.</param>
    /// <param name="rectangleWidth">The width of the platform.</param>
    /// <param name="rectangleHeight">The height of the platform.</param>
    /// <param name="numberOfBins">The total number of bins.</param>
    /// <returns>True if the bin is on the edge, false otherwise.</returns>
    public static bool IsRectangleEdgeBin(int bin, float rectangleWidth, float rectangleHeight, int numberOfBins)
    {
        (int numberOFBinsPerWidth, int numberOFBinsPerHeight) = GetBinDimensions(rectangleWidth, rectangleHeight, numberOfBins);

        // Determine which row and column the bin is in
        int column = bin / numberOFBinsPerHeight;  // Number of full rows
        int row = bin % numberOFBinsPerHeight;     // Position within a row

        // An edge bin is in the first/last row or first/last column
        bool isEdgeRow = (row == 0 || row == numberOFBinsPerHeight - 1);
        bool isEdgeColumn = (column == 0 || column == numberOFBinsPerWidth - 1);

        return isEdgeRow || isEdgeColumn;
    }


    public static bool IsEdgeBin1D(int bin, int numberOFBins)
    {
        if (bin == 0 || bin == numberOFBins - 1)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns all bin indices that fall inside a given RectTransform.
    /// </summary>
    /// <param name="rectCenter">The center of the RectTransform (x, y coordinates).</param>
    /// <param name="rectSize">The size of the RectTransform (width, height).</param>
    /// <param name="rectangleWidth">The width of the rectangle.</param>
    /// <param name="rectangleHeight">The height of the rectangle.</param>
    /// <param name="numberOfBins">The total number of bins on the platform.</param>
    /// <returns>A list of bin indices that fall within the RectTransform.</returns>
    public static List<int> GetBinsInsideRect(Vector2 rectCenter, Vector2 rectSize, float rectangleWidth, float rectangleHeight, int numberOfBins)
    {
        List<int> binsInside = new List<int>();

        // Step 1: Compute number of bins per width and height.
        (int numberOFBinsPerWidth, int numberOFBinsPerHeight) = GetBinDimensions(rectangleWidth, rectangleHeight, numberOfBins);

        // Step 2: Calculate bin sizes
        float binSizeX = rectangleWidth / numberOFBinsPerWidth;
        float binSizeZ = rectangleHeight / numberOFBinsPerHeight;

        // Step 3: Calculate the bounding box of the RectTransform (min and max corners)
        Vector2 rectMin = rectCenter - rectSize / 2;
        Vector2 rectMax = rectCenter + rectSize / 2;

        // Step 4: Convert the rectangle's bounds to bin indices
        int minColumn = Mathf.FloorToInt((rectMin.x + (rectangleWidth / 2)) / binSizeX);
        int maxColumn = Mathf.FloorToInt((rectMax.x + (rectangleWidth / 2)) / binSizeX);
        int minRow = Mathf.FloorToInt((rectMin.y + (rectangleHeight / 2)) / binSizeZ);
        int maxRow = Mathf.FloorToInt((rectMax.y + (rectangleHeight / 2)) / binSizeZ);

        // Step 5: Clamp the indices to ensure they are within the platform bounds
        minColumn = Mathf.Clamp(minColumn, 0, numberOFBinsPerWidth - 1);
        maxColumn = Mathf.Clamp(maxColumn, 0, numberOFBinsPerWidth - 1);
        minRow = Mathf.Clamp(minRow, 0, numberOFBinsPerHeight - 1);
        maxRow = Mathf.Clamp(maxRow, 0, numberOFBinsPerHeight - 1);

        // Step 6: Collect all bin indices within the bounds of the RectTransform
        for (int column = minColumn; column <= maxColumn; column++)
        {
            for (int row = minRow; row <= maxRow; row++)
            {
                int binIndex = column * numberOFBinsPerHeight + row;
                binsInside.Add(binIndex);
            }
        }

        return binsInside;
    }


    public static List<int> GetCrossedBins2DList(Vector3 vector, int bin, int numberOFBinsPerDirection, float platformRadius)
    {
        //truncating of vector necessary because otherwise numberOFBinsPerDirection*2 samples could be insufficient to get all bins along the vector
        Vector3 cutVector = CutVector(vector, platformRadius);

        float numberOfVectorSamples = numberOFBinsPerDirection * 2f;

        List<int> result = new List<int>();

        Vector3 end = BinToSquareCoordinates(bin, platformRadius, numberOFBinsPerDirection, 0);

        for (int i = 0; i <= (int)numberOfVectorSamples; i++)
        {
            int crossedBin = SquareCoordinatesToBin(new Vector3(end.x + (i / numberOfVectorSamples) * cutVector.x, 0, end.z + (i / numberOfVectorSamples) * cutVector.z), platformRadius, numberOFBinsPerDirection);

            if (!result.Contains(crossedBin) && crossedBin != -1 && crossedBin != bin)
            {
                result.Add(crossedBin);
            }
        }

        return result;
    }

    public static NativeList<int> GetCrossedBins2DNativeList(Vector3 vector, int bin, int numberOFBinsPerDirection, float platformRadius)
    {
        //truncating of vector necessary because otherwise numberOFBinsPerDirection*2 samples could be insufficient to get all bins along the vector
        Vector3 cutVector = CutVector(vector, platformRadius);

        float numberOfVectorSamples = numberOFBinsPerDirection * 2f;

        NativeList<int> result = new NativeList<int>(Allocator.Temp);

        Vector3 end = BinToSquareCoordinates(bin, platformRadius, numberOFBinsPerDirection, 0);

        for (int i = 0; i <= (int)numberOfVectorSamples; i++)
        {
            int crossedBin = SquareCoordinatesToBin(new Vector3(end.x + (i / numberOfVectorSamples) * cutVector.x, 0, end.z + (i / numberOfVectorSamples) * cutVector.z), platformRadius, numberOFBinsPerDirection);

            if (!result.Contains(crossedBin) && crossedBin != -1 && crossedBin != bin)
            {
                result.Add(crossedBin);
            }
        }

        return result;
    }

    public static List<int> GetCrossedBins1DList(float velocity, int bin, int numberOFBins, float rangeMin, float rangeMax)
    {
        List<int> result = new();

        float binSize = (rangeMax - rangeMin) / numberOFBins;

        int numberOfCrossedBins = (int)(Math.Abs(velocity) / binSize);

        if (bin == 0 && velocity > 0)
        {
            for (int i = 1; i <= numberOfCrossedBins; i++)
            {
                result.Add(i);
            }
        }

        if (bin == numberOFBins - 1 && velocity < 0)
        {
            for (int i = numberOFBins - 2; i + 1 >= numberOFBins - numberOfCrossedBins; i--)
            {
                result.Add(i);
            }
        }

        return result;
    }

    public static NativeList<int> GetCrossedBins1DNativeList(float velocity, int bin, int numberOFBins, float rangeMin, float rangeMax)
    {
        NativeList<int> result = new NativeList<int>(Allocator.Temp);

        float binSize = (rangeMax - rangeMin) / numberOFBins;

        int numberOfCrossedBins = (int)(Math.Abs(velocity) / binSize);

        if (bin == 0 && velocity > 0)
        {
            for (int i = 1; i <= numberOfCrossedBins; i++)
            {
                result.Add(i);
            }
        }

        if (bin == numberOFBins - 1 && velocity < 0)
        {
            for (int i = numberOFBins - 2; i + 1 >= numberOFBins - numberOfCrossedBins; i--)
            {
                result.Add(i);
            }
        }

        return result;
    }

    public static NativeList<int> GetRectangleCrossedBins(Vector2 velocity, int targetBin, float rectangleWidth, float rectangleHeight, int numberOfBins)
    {
        NativeList<int> crossedBins = new NativeList<int>(Allocator.Temp);

        // Compute the number of bins per width and height.
        (int numberOfBinsPerWidth, int numberOfBinsPerHeight) = GetBinDimensions(rectangleWidth, rectangleHeight, numberOfBins);

        // Get the bin size.
        float binSizeX = rectangleWidth / numberOfBinsPerWidth;
        float binSizeY = rectangleHeight / numberOfBinsPerHeight;

        // Get the position of the target bin.
        Vector2 targetPosition = BinToRectangleCoordinates(targetBin, rectangleWidth, rectangleHeight, numberOfBins);

        // Track forward using velocity to find all crossed bins.
        Vector2 step = velocity.normalized * Mathf.Min(binSizeX, binSizeY) / 2f;
        float distance = velocity.magnitude;
        Vector2 currentPosition = targetPosition;

        crossedBins.Add(targetBin);

        while (distance > 0)
        {
            // Move along the velocity direction.
            currentPosition -= step;
            distance -= step.magnitude;

            // Convert the new position to the corresponding bin.
            int currentBin = RectangleCoordinatesToBin(currentPosition, rectangleWidth, rectangleHeight, numberOfBins);

            // Add the current bin to the list if it hasn't already been added.
            if (!crossedBins.Contains(currentBin))
            {
                crossedBins.Add(currentBin);
            }

            // Stop when the target bin is reached.
            if (currentBin == -1)
            {
                break;
            }

            // Safety measure to avoid infinite loops in edge cases.
            if (step.magnitude < 1e-6f)
            {
                break;
            }
        }

        return crossedBins;
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
    /// Discretization of continuous float to discrete bin. Range starts at 0 i.e. value must be positive
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
    /// Same as above but without function names in warning which does not work within burst jobs.
    /// </summary>
    public static int ContinuousValueToBinBurst(float value, float min, float max, int numberOfBins)
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

        return ResolveBinOutOfRangeBurst(bin, numberOfBins);
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

    /// <summary>
    /// Calculates the clipped velocity of an object to ensure it remains inside a given RectTransform.
    /// If applying the original velocity would move the object outside the rectangle, the velocity is adjusted
    /// so that the object stops exactly at the edge. If the object remains inside, the original velocity is returned.
    /// </summary>
    /// <param name="rectTransform">The RectTransform defining the rectangle bounds.</param>
    /// <param name="currentPosition">The current position of the object inside the rectangle.</param>
    /// <param name="velocity">The original velocity vector to be applied to the object.</param>
    /// <returns>The clipped velocity that ensures the object stays inside the rectangle.</returns>
    public static Vector2 GetClippedVelocity(Rect rect, Vector2 currentPosition, Vector2 velocity, float margin = 2)
    {
        // Small epsilon value to handle very small velocities
        float epsilon = 1e-5f;

        if (!rect.Contains(currentPosition))
        {
            Debug.Log($"Current position is outside the rectangle bounds: {currentPosition}. Returning zero Vector.");
            return Vector2.zero;
        }

        // Get the pivot-adjusted position of the bottom-left corner of the rectangle
        Vector2 rectMin = new Vector2(rect.x + margin, rect.y + margin);
        Vector2 rectMax = rectMin + new Vector2(rect.width - 2 * margin, rect.height - 2 * margin);

        // Calculate the new position after applying velocity
        Vector2 newPosition = currentPosition + velocity;

        // Initialize clipped velocity to the original velocity
        Vector2 clippedVelocity = velocity;

        // Check if the new position is still inside the rectangle bounds
        if (newPosition.x < rectMin.x)
        {
            // Clip the x-velocity to the left edge
            clippedVelocity.x = rectMin.x - currentPosition.x;
        }
        else if (newPosition.x > rectMax.x)
        {
            // Clip the x-velocity to the right edge
            clippedVelocity.x = rectMax.x - currentPosition.x;
        }

        if (newPosition.y < rectMin.y)
        {
            // Clip the y-velocity to the bottom edge
            clippedVelocity.y = rectMin.y - currentPosition.y;
        }
        else if (newPosition.y > rectMax.y)
        {
            // Clip the y-velocity to the top edge
            clippedVelocity.y = rectMax.y - currentPosition.y;
        }

        // If the clipped velocity is significantly different from the original, return the clipped velocity
        if (Mathf.Abs(clippedVelocity.x - velocity.x) > epsilon || Mathf.Abs(clippedVelocity.y - velocity.y) > epsilon)
        {
            return clippedVelocity;
        }

        // If no valid intersection found, return original velocity
        return velocity;
    }

    /// <summary>
    /// Ensures that the resulting position stays inside the rectangle bounds, even if it's at the edge or outside.
    /// This will move the object inside the rectangle if it reaches or goes beyond the edges.
    /// </summary>
    /// <param name="rect">The Rect that defines the rectangle.</param>
    /// <param name="position">The position to be adjusted.</param>
    /// <returns>A clamped position that is guaranteed to be inside the rectangle.</returns>
    public static Vector2 ClampPositionInsideRect(Rect rect, Vector2 position)
    {
        // Get the size of the rectangle
        Vector2 size = rect.size;

        // Get the pivot-adjusted position of the bottom-left corner of the rectangle
        Vector2 rectMin = new Vector2(rect.x, rect.y);
        Vector2 rectMax = rectMin + size;

        // Clamp the position within the rectangle bounds
        float clampedX = Mathf.Clamp(position.x, rectMin.x, rectMax.x);
        float clampedY = Mathf.Clamp(position.y, rectMin.y, rectMax.y);

        return new Vector2(clampedX, clampedY);
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

    private static float CutVelocity(float velocity, float range)
    {
        if (velocity > range)
        {
            return velocity * (velocity / range);
        }

        return velocity;
    }

    private static int ResolveBinOutOfRange<T>(T value, int bin, T range, int numberOfBins, string functionName)
    {
        if (bin < 0)
        {
            Debug.LogWarning(string.Format("Bin out of range: value={0}, bin={1}, numberOfBins={2}, range={3}, function={4}", value, bin, numberOfBins, range, functionName));

            return 0;
        }
        else if (bin >= numberOfBins)
        {
            Debug.LogWarning(string.Format("Bin out of range: value={0}, bin={1}, numberOfBins={2}, range={3}, function={4}", value, bin, numberOfBins, range, functionName));

            return numberOfBins - 1;
        }

        return bin;
    }

    private static int ResolveBinOutOfRangeBurst(int bin, int numberOfBins)
    {
        if (bin < 0)
        {
            return 0;
        }
        else if (bin >= numberOfBins)
        {
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