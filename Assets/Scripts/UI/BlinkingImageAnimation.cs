using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkingImageAnimation : MonoBehaviour
{
    public float FadeSpeed { get; set; } = 2f;


    private bool _isFadeIn = false;

    private bool _isFadeOut = false;

    private const float DISPLAYTIME = 2f;

    private float _displayTime;

    private CanvasGroup _canvasGroup;

    private void UpdateFadeIn()
    {
        if (_isFadeIn)
        {
            if (_canvasGroup.alpha < 1)
            {
                _canvasGroup.alpha += Time.deltaTime * FadeSpeed;
            }
            else
            {
                _isFadeIn = false;
                _isFadeOut = true;  
            }
        }
    }

    private void UpdateFadeOut()
    {
        if (_displayTime > 0)
        {
            _displayTime -= Time.deltaTime;
        }

        if (_isFadeOut && !_isFadeIn && _displayTime <= 0)
        {
            if (_canvasGroup.alpha > 0.2)
            {
                _canvasGroup.alpha -= Time.deltaTime * FadeSpeed;
            }
            else
            {
                _isFadeOut = false;
                _isFadeIn = true;
            }
        }
    }

    private void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _isFadeOut = true;
    }

    private void Update()
    {
        UpdateFadeIn();
        UpdateFadeOut();
    }
}
