using NUnit.Framework;
using System;
using UnityEngine;

public class DistancesTest
{
    [Test]
    public void DistanceToEdgeTest()
    {
        float distance = Distances.DistanceToEdge(5, new Vector3(-1, 0, -1), new Vector3(0, 0, 1));
        Assert.AreEqual(5.899, Math.Round(distance, 3));

        distance = Distances.DistanceToEdge(5, new Vector3(-1, 0, -1), new Vector3(2, 0, 2.5f));
        Assert.AreEqual(6.403, Math.Round(distance, 3));

        distance = Distances.DistanceToEdge(5, new Vector3(2, 0, 0.5f), new Vector3(0.25f, 0, -1.125f));
        Assert.AreEqual(4.61, Math.Round(distance, 3));

        distance = Distances.DistanceToEdge(5, new Vector3(3, 0, 3), new Vector3(-1f, 0, (float)-6/7));
        Assert.AreEqual(9.22, Math.Round(distance, 3));

        distance = Distances.DistanceToEdge(5, new Vector3(0, 0, 0), new Vector3(-40f, 0, 30));
        Assert.AreEqual(5, Math.Round(distance, 3));

        distance = Distances.DistanceToEdge(5, new Vector3(-3, 0, -3), new Vector3(-0.33303f, 0, -0.2f));
        Assert.AreEqual(0.777, Math.Round(distance, 3));
    }
}
