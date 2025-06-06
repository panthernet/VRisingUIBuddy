﻿using System;
using UIBuddy.UI.Classes;
using UIBuddy.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace UIBuddy.UI.ScrollView;

/// <summary>
/// A scrollbar which automatically resizes itself (and its handle) depending on the size of the content and viewport.
/// </summary>
public class AutoSliderScrollbar : UIBehaviourModel
{
    protected override GameObject UIRoot
    {
        get
        {
            if (Slider)
                return Slider.gameObject;
            return null;
        }
    }

    public Slider Slider { get; }
    public Scrollbar Scrollbar { get; }
    public RectTransform ContentRect { get; }
    public RectTransform ViewportRect { get; }

    private float _lastAnchorPosition;
    private float _lastContentHeight;
    private float _lastViewportHeight;
    private bool _refreshWanted;

    public AutoSliderScrollbar(Scrollbar scrollbar, Slider slider, RectTransform contentRect, RectTransform viewportRect)
    {
        Scrollbar = scrollbar;
        Slider = slider;
        ContentRect = contentRect;
        ViewportRect = viewportRect;

        Scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
        Slider.onValueChanged.AddListener(OnSliderValueChanged);

        Slider.Set(0f, false);

        UpdateSliderHandle();
    }

    public override void Update()
    {
        if (!Enabled)
            return;

        _refreshWanted = false;
        if (Math.Abs(ContentRect.localPosition.y - _lastAnchorPosition) > 0.0001)
        {
            _lastAnchorPosition = ContentRect.localPosition.y;
            _refreshWanted = true;
        }
        if (Math.Abs(ContentRect.rect.height - _lastContentHeight) > 0.0001)
        {
            _lastContentHeight = ContentRect.rect.height;
            _refreshWanted = true;
        }
        if (Math.Abs(ViewportRect.rect.height - _lastViewportHeight) > 0.0001)
        {
            _lastViewportHeight = ViewportRect.rect.height;
            _refreshWanted = true;
        }

        if (_refreshWanted)
            UpdateSliderHandle();
    }

    private void UpdateSliderHandle()
    {
        // calculate handle size based on viewport / total data height
        float totalHeight = ContentRect.rect.height;
        float viewportHeight = ViewportRect.rect.height;

        if (totalHeight <= viewportHeight)
        {
            Slider.handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
            Slider.value = 0f;
            Slider.interactable = false;
            return;
        }

        float handleHeight = viewportHeight * Math.Min(1, viewportHeight / totalHeight);
        handleHeight = Math.Max(15f, handleHeight);

        // resize the handle container area for the size of the handle (bigger handle = smaller container)
        RectTransform container = Slider.m_HandleContainerRect;
        container.offsetMax = new Vector2(container.offsetMax.x, -(handleHeight * 0.5f));
        container.offsetMin = new Vector2(container.offsetMin.x, handleHeight * 0.5f);

        // set handle size
        Slider.handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handleHeight);

        // if slider is 100% height then make it not interactable
        Slider.interactable = !Mathf.Approximately(handleHeight, viewportHeight);

        float val = 0f;
        if (totalHeight > 0f)
            val = (float)((decimal)ContentRect.localPosition.y / (decimal)(totalHeight - ViewportRect.rect.height));

        Slider.Set(val);
    }

    private void OnScrollbarValueChanged(float value)
    {
        value = 1f - value;
        // Don't update the value if it is the same, as we don't want to loop setting the values
        if (Math.Abs(Slider.value - value) > 0.0001)
        {
            // Make sure we send the callback to correctly propagate the value
            Slider.Set(value, true);
        }
    }

    private void OnSliderValueChanged(float value)
    {
        value = 1f - value;
        // Don't update the value if it is the same, as we don't want to loop setting the values
        if (Math.Abs(Scrollbar.value - value) > 0.0001)
        {
            // Make sure we send the callback to correctly propagate the value
            Scrollbar.Set(value, true);
        }
    }
}