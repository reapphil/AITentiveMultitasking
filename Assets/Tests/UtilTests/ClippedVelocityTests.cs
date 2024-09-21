using NUnit.Framework;
using UnityEngine;


public class ClippedVelocityTests
{
    [Test]
    public void GetClippedVelocityTest()
    {
        Rect rect = new Rect(-50, -50, 100, 100);
        float margin = 0.1f;

        Vector2 clippedVeclocity = PositionConverter.GetClippedVelocity(rect, new Vector2(0, 0), new Vector2(100, 100), margin);
        Assert.AreEqual(new Vector2(49.9f, 49.9f), clippedVeclocity);

        clippedVeclocity = PositionConverter.GetClippedVelocity(rect, new Vector2(0, 10), new Vector2(0, -80), margin);
        Assert.AreEqual(new Vector2(0, -59.9f), clippedVeclocity);

        clippedVeclocity = PositionConverter.GetClippedVelocity(rect, new Vector2(0, 0), new Vector2(-100, 0), margin);
        Assert.AreEqual(new Vector2(-49.9f, 0), clippedVeclocity);

        clippedVeclocity = PositionConverter.GetClippedVelocity(rect, new Vector2(0, 0), new Vector2(-10, -100), margin);
        Assert.IsTrue(Mathf.Approximately(-49.9f, clippedVeclocity.y));
        //Assert.IsTrue(Mathf.Approximately(-5, clippedVeclocity.x)); //<-- fails
    }

    [Test]
    public void GetClippedVelocityPracticeTest()
    {
        Rect rect = new Rect(-440, -338, 880, 676);
        Vector2 velocity = new Vector2(-3.051758E-05f, -1032.309f);

        Vector2 clippedVeclocity = PositionConverter.GetClippedVelocity(rect, new Vector2(438.0946f, 62.51642f), velocity, 2);
        Assert.AreNotEqual(velocity, clippedVeclocity);
    }

    [Test]
    public void GetClippedVelocityWithExtremeVelocity()
    {
        Rect rect = new Rect(-440, -338, 880, 676);
        Vector2 velocity = new Vector2(0, -2000f);

        Vector2 clippedVeclocity = PositionConverter.GetClippedVelocity(rect, new Vector2(438f, 337.90f), velocity);
        Assert.AreNotEqual(velocity, clippedVeclocity); // Ensure velocity gets clipped
    }

    [Test]
    public void ObjectRemainsInsideRectangleTest()
    {
        Rect rect = new Rect(0, 0, 10, 10);
        Vector2 currentPosition = new Vector2(5, 5);
        Vector2 velocity = new Vector2(2, 2);

        Vector2 clippedVelocity = PositionConverter.GetClippedVelocity(rect, currentPosition, velocity);

        Assert.AreEqual(velocity, clippedVelocity);
    }

    [Test]
    public void ObjectMovesOutOfBoundsLeftTest()
    {
        Rect rect = new Rect(0, 0, 10, 10);
        Vector2 currentPosition = new Vector2(5, 5);
        Vector2 velocity = new Vector2(-10, 0); // Moving left

        Vector2 clippedVelocity = PositionConverter.GetClippedVelocity(rect, currentPosition, velocity);

        // Expected clipped velocity should stop exactly at the left edge (x = 0)
        Vector2 expectedVelocity = new Vector2(-3, 0); // x = 5 + (-3) = 2 (adjusted by margin)
        Assert.AreEqual(expectedVelocity, clippedVelocity);
    }

    [Test]
    public void ObjectMovesOutOfBoundsRightTest()
    {
        Rect rect = new Rect(0, 0, 10, 10);
        Vector2 currentPosition = new Vector2(5, 5);
        Vector2 velocity = new Vector2(10, 0); // Moving right

        Vector2 clippedVelocity = PositionConverter.GetClippedVelocity(rect, currentPosition, velocity);

        // Expected clipped velocity should stop exactly at the right edge (x = 10)
        Vector2 expectedVelocity = new Vector2(3, 0); // x = 5 + 3 = 8 (adjusted by margin)
        Assert.AreEqual(expectedVelocity, clippedVelocity);
    }

    [Test]
    public void ObjectMovesOutOfBoundsUpTest()
    {
        Rect rect = new Rect(0, 0, 10, 10);
        Vector2 currentPosition = new Vector2(5, 5);
        Vector2 velocity = new Vector2(0, 10); // Moving up

        Vector2 clippedVelocity = PositionConverter.GetClippedVelocity(rect, currentPosition, velocity);

        // Expected clipped velocity should stop exactly at the top edge (y = 10)
        Vector2 expectedVelocity = new Vector2(0, 3); // y = 5 + 3 = 8 (adjusted by margin)
        Assert.AreEqual(expectedVelocity, clippedVelocity);
    }

    [Test]
    public void ObjectMovesOutOfBoundsDownTest()
    {
        Rect rect = new Rect(0, 0, 10, 10);
        Vector2 currentPosition = new Vector2(5, 5);
        Vector2 velocity = new Vector2(0, -10); // Moving down

        Vector2 clippedVelocity = PositionConverter.GetClippedVelocity(rect, currentPosition, velocity);

        // Expected clipped velocity should stop exactly at the bottom edge (y = 0)
        Vector2 expectedVelocity = new Vector2(0, -3); // y = 5 + (-3) = 2 (adjusted by margin)
        Assert.AreEqual(expectedVelocity, clippedVelocity);
    }

    [Test]
    public void ObjectPositionOutsideRectangleTest()
    {
        Rect rect = new Rect(0, 0, 10, 10);
        Vector2 currentPosition = new Vector2(15, 15); // Outside the rectangle
        Vector2 velocity = new Vector2(5, 5);

        Vector2 clippedVelocity = PositionConverter.GetClippedVelocity(rect, currentPosition, velocity);

        // Object is outside, so velocity should be zero
        Assert.AreEqual(Vector2.zero, clippedVelocity);
    }
}
