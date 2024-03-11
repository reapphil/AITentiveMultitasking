using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class stopwatch : MonoBehaviour
{
    float currentTime;
    public Text currentTimeText;

    void Start()
    {
        currentTime = 0f;
    }

    void Update()
    {
        currentTime = currentTime + Time.deltaTime;
        TimeSpan time = TimeSpan.FromSeconds(currentTime);
        currentTimeText.text = time.ToString(@"mm\:ss\:fff");
    }

    public void Reset()
    {
        currentTime = 0f;
    }
}
