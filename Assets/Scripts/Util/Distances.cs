using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public static class Distances
{
    public static float DistanceToEdge(float radius, Vector3 ballposition, Vector3 directionVector)
    {
        Vector3 norm = directionVector.normalized;
        double a = Math.Pow(norm.x, 2) + Math.Pow(norm.y, 2) + Math.Pow(norm.z, 2);
        double b = 2 * ballposition.x * norm.x + 2 * ballposition.y * norm.y + 2 * ballposition.z * norm.z;
        double c = Math.Pow(ballposition.x, 2) + Math.Pow(ballposition.y, 2) + Math.Pow(ballposition.z, 2) - Math.Pow(radius, 2);

        double[] ts = SolveQuadratic(a, b, c);

        if(ts.Length == 0)
        {
            return 0;
        }

        double t = ts[0];

        Vector3 edge = new Vector3((float)(ballposition.x + t * norm.x), (float)(ballposition.y + t * norm.y), (float)(ballposition.z + t * norm.z));
        Vector3 edgeVector = edge - ballposition;

        //Debug.Log(string.Format("a: {0}, b: {1}, c: {2}, t: {3}, edge: {4}, edgeVector: {5}, norm: {6}", a, b, c, t, edge, edgeVector, norm));

        return edgeVector.magnitude;
    }

    public static float SecondsUntilGameOverForCurrentSpeed(float radius, Vector3 ballposition, Vector3 directionVector)
    {
        float distanceToEdge = DistanceToEdge(radius, ballposition, directionVector);

        return distanceToEdge / directionVector.magnitude;
    }

    public static float SecondsUntilGameOver(float radius, float slope, Vector3 ballposition, Vector3 directionVector, float globaDrag)
    {
        float distanceToEdge = DistanceToEdge(radius, ballposition, directionVector);
        float currentSpeed = directionVector.magnitude;
        float gravity = Physics.gravity.magnitude;
        float velocity = currentSpeed;
        float frictionCoefficient = 0.6f;
        float cSphere = (2f / 5f);
        float acceleration = (gravity * Mathf.Sin(Mathf.Deg2Rad * slope)) / (1 + cSphere * frictionCoefficient);
        acceleration = directionVector.y > 0 ? -acceleration : acceleration;

        //Formula for distance D: D = vt + (1/2)at^2
        if (acceleration == 0)
        {
            return (distanceToEdge / velocity) * (1 + globaDrag); //approximation of the drag value calculation
        }

        double[] result = SolveQuadratic(acceleration/2, velocity, -distanceToEdge);

        if (result.Length == 0)
        {
            return float.NaN;
        }

        return (float)result[0] * (1 + globaDrag);
    }

    public static double[] SolveQuadratic(double a, double b, double c)
    {
        double discriminant = (Math.Pow(b, 2) - 4 * a * c);

        if (discriminant < 0) 
        {
            return new double[0];
        }
        else if(discriminant == 0)
        {
            return new double[] { (-b + Math.Sqrt(discriminant)) / (2 * a) };
        }
        else
        {
            return new double[] { (-b + Math.Sqrt(discriminant)) / (2 * a), (-b - Math.Sqrt(discriminant)) / (2 * a) };
        }
    }
}
