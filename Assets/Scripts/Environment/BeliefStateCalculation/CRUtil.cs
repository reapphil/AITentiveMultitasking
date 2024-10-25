using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR;
using static ObjectIn2DGridProbabilitiesUpdateJob;

public static class CRUtil
{
    public static Vector3[] GetNormalDistributionForVelocity(int numberOfSamples, Vector3 velocity, double sigma, System.Random rand)
    {
        Vector3[] normal = new Vector3[numberOfSamples];

        NormalDistribution normalDistributionX = new NormalDistribution(velocity.x, sigma);
        NormalDistribution normalDistributionZ = new NormalDistribution(velocity.z, sigma);

        for (int i = 0; i < numberOfSamples; i++)
        {
            normal[i] = new Vector3((float)normalDistributionX.Sample(rand), velocity.y, (float)normalDistributionZ.Sample(rand)); //TODO: change velocity.y to the actual position of y in respect of the sample of x and z
        }
        return normal;
    }

    public static Vector2[] GetNormalDistributionForVelocity(int numberOfSamples, Vector2 velocity, double sigma, System.Random rand)
    {
        Vector2[] normal = new Vector2[numberOfSamples];

        NormalDistribution normalDistributionX = new NormalDistribution(velocity.x, sigma);
        NormalDistribution normalDistributionY = new NormalDistribution(velocity.y, sigma);

        for (int i = 0; i < numberOfSamples; i++)
        {
            normal[i] = new Vector2((float)normalDistributionX.Sample(rand), (float)normalDistributionY.Sample(rand));
        }
        return normal;
    }

    public static float[] GetNormalDistributionForVelocity(int numberOfSamples, float velocity, double sigma, System.Random rand)
    {
        float[] normal = new float[numberOfSamples];

        NormalDistribution normalDistributionX = new NormalDistribution(velocity, sigma);

        for (int i = 0; i < numberOfSamples; i++)
        {
            normal[i] = (float)normalDistributionX.Sample(rand); //TODO: change velocity.y to the actual position of y in respect of the sample of x and z
        }
        return normal;
    }

    public static Vector3 GetAverageVelocity(Vector3[] normalDistributionForVelocity)
    {
        float x, y, z;
        x = y = z = 0;

        foreach (Vector3 vector in normalDistributionForVelocity)
        {
            x = x + vector.x;
            y = y + vector.y;
            z = z + vector.z;
        }

        int numberOfSamples = normalDistributionForVelocity.Length;

        return new Vector3(x / numberOfSamples, y / numberOfSamples, z / numberOfSamples);
    }

    public static float GetAverageVelocity(float[] normalDistributionForVelocity)
    {
        float x;
        x = 0;

        foreach (float vector in normalDistributionForVelocity)
        {
            x = x + vector;
        }

        int numberOfSamples = normalDistributionForVelocity.Length;

        return x / numberOfSamples;
    }

    public static List<VisualStateSpace> GetFocusableGameObjectsOfTasks(List<ITask> tasks)
    {
        List<VisualStateSpace> focusableObjects = new();

        for (int i = 0; i < tasks.Count; i++)
        {
            VisualStateSpace visualStateSpace = new()
            {
                VisualElements = new(),
                Camera = tasks[i].GetGameObject().transform.parent.transform.GetChildByName("Camera").GetComponent<Camera>()
            };

            focusableObjects.Insert(i, visualStateSpace);

            if (tasks[i].GetType().GetInterfaces().Contains(typeof(ICrTask)))
            {
                focusableObjects[i].VisualElements.AddRange(((ICrTask)tasks[i]).FocusStateSpace.VisualElements);
            }
        }

        return focusableObjects;
    }

    public static Vector3? GetScreenCoordinatesForActiveGameObject(List<VisualStateSpace> visualStateSpaces)
    {
        foreach (VisualStateSpace visualStateSpace in visualStateSpaces) 
        { 
            if (visualStateSpace.HasActiveElement())
            {
                return visualStateSpace.GetScreenCoordinatesForActiveGameObject();
            }
        }

        return null;
    }

    public static GameObjectPosition ConvertToGameObjectPosition(GameObject gameObject, Camera camera)
    {
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            throw new System.Exception("GameObject does not have a RectTransform component.");
        }

        // Get the world position of the button
        Vector3 worldPosition = rectTransform.position;

        // Convert world position to screen position
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);

        // Get the size of the button in world space
        Vector2 worldSize = rectTransform.rect.size;

        // Convert world size to screen size (assuming no scaling issues)
        Vector2 screenSize = worldSize * rectTransform.lossyScale;

        return new GameObjectPosition
        {
            position = new float2(screenPosition.x, screenPosition.y),
            size = new float2(screenSize.x, screenSize.y),
        };
    }

    public static float PixelToCM(float pixel, float screenWidthPixel = 0, float screenHightPixel = 0, float screenDiagonalInch = 0)
    {
        //fallback value which is 96 on Windows
        float ppi = Screen.dpi;

        if (screenWidthPixel == 0 || screenHightPixel == 0 || screenDiagonalInch == 0)
        {
            if (ppi == 0)
            {
                ppi = 96;
            }
        }
        else
        {
            float screenDiagonalPixel = Mathf.Sqrt(Mathf.Pow(screenWidthPixel, 2) + Mathf.Pow(screenHightPixel, 2));
            ppi = screenDiagonalPixel / screenDiagonalInch;
        }


        float inches = pixel / ppi;

        return inches * 2.54f;
    }

    public static Vector2 PixelToCM(Vector2 pixel, float screenWidthPixel = 0, float screenHightPixel = 0, float screenDiagonalInch = 0)
    {
        return new Vector2(PixelToCM(pixel.x, screenWidthPixel, screenHightPixel, screenDiagonalInch), PixelToCM(pixel.y, screenWidthPixel, screenHightPixel, screenDiagonalInch));
    }
}
