using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeImageAnimation : MonoBehaviour
{
    private bool _isFadeIn = false;

    private bool _isFadeOut = false;

    private const float DISPLAYTIME = 2f;

    private const float FADESPEED = 2f;

    private float _displayTime;

    private CanvasGroup _canvasGroup;

    public void FadeIn()
    {
        _isFadeIn = true;
    }

    public void FadeOut()
    {
        _isFadeOut = true;
    }

    public void FadeInOut()
    {
        _isFadeIn = true;
        _isFadeOut = true;
        _displayTime = DISPLAYTIME;
    }

    private void UpdateFadeIn()
    {
        if (_isFadeIn)
        {
            if (_canvasGroup.alpha < 1)
            {
                _canvasGroup.alpha += Time.deltaTime * FADESPEED;
            }
            else
            {
                _isFadeIn = false;
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
            if (_canvasGroup.alpha > 0)
            {
                _canvasGroup.alpha -= Time.deltaTime * FADESPEED;
            }
            else
            {
                _isFadeOut = false;
            }
        }
    }

    private void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        UpdateFadeIn();
        UpdateFadeOut();
    }
}
