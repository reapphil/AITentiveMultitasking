using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class VisualStateSpace
{
    [field: SerializeField]
    public List<GameObject> VisualElements { get; set; }

    [field: SerializeField]
    public Camera Camera { get; set; }

    [field: SerializeField]
    public Canvas Canvas { get; set; }

    public List<GameObject> ActiveVisualElements 
    { 
        get 
        {
            return VisualElements.Where(a => a.activeInHierarchy).ToList();
        } 
    }


    public List<float> Encoding
    {
        get
        {
            if (_encoding == null)
            {
                _encoding = new List<float>(new float[VisualElements.Count]);
            }

            return _encoding;
        }
    }


    private List<float> _encoding;


    public GameObject GetFirstActiveElement()
    {
        int index = GetFirstActiveIndex();
        return index == -1 ? null : VisualElements[index];
    }

    public int GetFirstActiveIndex()
    {
        for (int i = 0; i < Encoding.Count; i++)
        {
            if (Encoding[i] == 1)
            {
                return i;
            }
        }

        return -1;
    }

    public bool HasActiveElement()
    {
        return Encoding.Contains(1);
    }

    public bool IsActiveElement(GameObject gameObject)
    {
        return IsActiveElement(VisualElements.FindIndex(a => a == gameObject));
    }

    public bool IsActiveElement(int index)
    {
        return Encoding[index] == 1;
    }

    public bool IsActiveElement(Vector2 screenPosition, float margin = 0)
    {
        List<GameObject> gameObjects = GetGameObjectForScreenCoordinates(screenPosition, margin);

        foreach (GameObject gameObject in gameObjects)
        {
            if (IsActiveElement(gameObject))
            {
                return true;
            }
        }

        return false;
    }

    public int GetActiveState()
    {
        for (int i = 0; i < Encoding.Count; i++)
        {
            if (Encoding[i] == 1)
            {
                return i;
            }
        }

        return -1;
    }

    public Vector3? GetScreenCoordinatesForActiveGameObject()
    {
        if(GetFirstActiveElement() == null)
        {
            return null;
        }

        return GetScreenPositionForWorldPosition(Camera, GetFirstActiveElement().transform.position);
    }

    public Vector3 GetScreenCoordinatesForGameObjectIndex(int index)
    {
        return GetScreenPositionForWorldPosition(Camera, VisualElements[index].transform.position);
    }

    public Vector3 GetScreenCoordinatesForGameObjectName(string name)
    {
        return GetScreenPositionForWorldPosition(Camera, VisualElements.Find(a => a.name == name).transform.position);
    }

    public Vector3 GetScreenCoordinatesForGameObject(GameObject gameObject)
    {
        return GetScreenPositionForWorldPosition(Camera, gameObject.transform.position);
    }

    public GameObject GetGameObjectForName(string name)
    {
        GameObject gameObject = VisualElements.Find(a => a.name == name);

        if (gameObject == null)
        {
            Debug.LogError("GameObject with name " + name + " not found in VisualStateSpace.");
        }

        return gameObject;
    }

    public GameObject GetGameObjectForScreenCoordinates(Vector2 screenCoordinates)
    {
        // Set up the new Pointer Event
        PointerEventData  pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenCoordinates
        };

        // Create a list to store Raycast results
        List<RaycastResult> results = new List<RaycastResult>();

        // Raycast using the GraphicRaycaster and the PointerEventData
        Canvas.GetComponent<GraphicRaycaster>().Raycast(pointerEventData, results);

        foreach (RaycastResult raycastResult in results)
        {
            if (VisualElements.Contains(raycastResult.gameObject))
            {
                return raycastResult.gameObject;
            } 
        }

        return null;
    }

    public List<GameObject> GetGameObjectForScreenCoordinates(Vector2 screenCoordinates, float margin)
    {
        List<GameObject> gameObjects = new();

        foreach (GameObject uiElement in VisualElements)
        {
            // Get the RectTransform of the UI element
            RectTransform rectTransform = uiElement.GetComponent<RectTransform>();

            // Get the object's screen rect
            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            // Convert world corners to screen space
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(Canvas.worldCamera, worldCorners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(Canvas.worldCamera, worldCorners[2]);

            // Expand the rect by the margin
            Rect expandedRect = new Rect(
                bottomLeft.x - margin,
                bottomLeft.y - margin,
                (topRight.x - bottomLeft.x) + 2 * margin,
                (topRight.y - bottomLeft.y) + 2 * margin
            );

            // Check if the mouse position is within this expanded rect
            if (expandedRect.Contains(screenCoordinates))
            {
                gameObjects.Add(uiElement);
            }
        }

        return gameObjects;
    }

    public bool IsObjectAtScreenCoordinates(Vector2 screenCoordinates)
    {
        return GetGameObjectForScreenCoordinates(screenCoordinates) != null;
    }

    public bool ActivateElement(GameObject gameObject)
    {
        int index = VisualElements.FindIndex(a => a == gameObject);

        ActivateElement(index);

        return index != -1;
    }

    public void ActivateElement(int index)
    {
        Encoding[index] = 1;
    }

    public bool DeactivateElement(GameObject gameObject)
    {
        int index = VisualElements.FindIndex(a => a == gameObject);

        DeactivateElement(index);

        return index != -1;
    }

    public void DeactivateElement(int index)
    {
        Encoding[index] = 0;
    }

    public bool ActivateSingleElement(GameObject gameObject)
    {
        int index = VisualElements.FindIndex(a => a == gameObject);

        ActivateSingleElement(index);

        return index != -1;
    }

    public void ActivateSingleElement(int index)
    {
        for (int i = 0; i < Encoding.Count; i++)
        {
            Encoding[i] = i == index ? 1 : 0;
        }
    }

    public void ActivateAllElements()
    {
        for (int i = 0; i < Encoding.Count; i++)
        {
            Encoding[i] =  1;
        }
    }

    public void DeactivateAllElements()
    {
        for (int i = 0; i < Encoding.Count; i++)
        {
            Encoding[i] = 0;
        }
    }

    public Vector2 GetMaxScreenDistanceBetweenVisualElements()
    {
        float maxX = 0;
        float maxY = 0;

        for (int i = 0; i < VisualElements.Count; i++) 
        {
            for (int j = 0; j < VisualElements.Count; j++)
            {
                Vector2 distance = GetScreenCoordinatesForGameObjectIndex(i) - GetScreenCoordinatesForGameObjectIndex(j);

                if (Mathf.Abs(distance.x) > maxX)
                {
                    maxX = Mathf.Abs(distance.x);
                }

                if (Mathf.Abs(distance.y) > maxY)
                {
                    maxY = Mathf.Abs(distance.y);
                }
            }
        }

        return new Vector2(maxX, maxY);
    }


    public static Vector3 GetScreenPositionForWorldPosition(Camera cam, Vector3 worldPosition)
    {
        Vector2 position = RectTransformUtility.WorldToScreenPoint(
            cam,
            worldPosition
        );

        return position;
    }
}
