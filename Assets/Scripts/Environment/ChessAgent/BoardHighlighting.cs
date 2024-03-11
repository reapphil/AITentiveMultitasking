using System.Collections.Generic;
using UnityEngine;

public class BoardHighlighting : MonoBehaviour
{
    public GameObject highlightPrefab;


    private List<GameObject> highlights;


    public void HighlightAllowedMoves(bool[,] moves)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (moves[i, j])
                {
                    GameObject go = GetHighlightObject();
                    go.SetActive(true);
                    go.transform.position = transform.TransformPoint(new Vector3(i + 0.5f - 4, 0.01f, j + 0.5f - 4));
                }
            }
        }
    }

    public void HideHighlights()
    {
        foreach (GameObject go in highlights) go.SetActive(false);
    }


    private void Start()
    {
        highlights = new List<GameObject>();
    }

    private GameObject GetHighlightObject()
    {
        GameObject go = highlights.Find(g => !g.activeSelf);
        if (go == null)
        {
            go = Instantiate(highlightPrefab, gameObject.GetSpawnContainer().transform);
            highlights.Add(go);
        }
        return go;
    }
}