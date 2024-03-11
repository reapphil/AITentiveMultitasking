using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace timeTools
{
    public class Stopwatch
    {
        bool stopwatchActive = false;
        float currentTime;
        public Text currentTimeText;

        void Start()
        {
            currentTime = 0f;
        }

        void Update()
        {
            if (stopwatchActive)
            {
                currentTime = currentTime + Time.deltaTime;

            }
            TimeSpan time = TimeSpan.FromSeconds(currentTime);
            currentTimeText.text = time.ToString(@"mm\:ss\:fff");
            Debug.Log("Time: " + time.ToString(@"mm\:ss\:fff"));
        }

        public void StartStopwatch()
        {
            stopwatchActive = true;
        }

        public void StopStopwatch()
        {
            stopwatchActive = false;
        }
    }
}
