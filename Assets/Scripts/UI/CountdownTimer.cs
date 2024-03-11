using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountdownTimer : MonoBehaviour
{
    public float CurrentTime { get; private set; } = 0f;

    private TextMeshProUGUI _countDownText;


    public void StartCountDown(float startingTime)
    {
        CurrentTime = startingTime;
        _countDownText = gameObject.GetComponent<TextMeshProUGUI>();
    }


    // Update is called once per frame
    private void Update()
    {
        if( CurrentTime != 0)
        {
            updateTimer();
        }
    }

    private void updateTimer()
    {
        CurrentTime -= Time.unscaledDeltaTime;

        if (CurrentTime <= 0)
        {
            CurrentTime = 0;
        }

        if (CurrentTime > 0)
        {
            _countDownText.text = CurrentTime.ToString("0");
        }
        else
        {
            _countDownText.text = "";
        }
    }
}
