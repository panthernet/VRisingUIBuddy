﻿using System;
using System.Collections;
using HarmonyLib;
using TMPro;
using UIBuddy.Behaviors;
using UIBuddy.Managers;
using UIBuddy.UI.Classes;
using UIBuddy.Utils;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UIBuddy.UI.Panel;

public class ElementPanel: GenericPanelBase
{
    public bool CanDrag => true;

    public CanvasScaler OwnerCanvasScaler { get; protected set; }
    public Transform Transform { get; protected set; }

    protected RectTransform CustomUIRect { get; set; }
    protected GameObject CustomUIObject { get; set; }
    protected RectOutline Outline { get; set; }

    // Track the original scale to allow proper reset
    protected float OriginalScaleFactor;
    //private ToggleRef _toggleRef;
    private readonly string _shortName;
    protected PanelParameters Parameters { get; set; }

    public ElementPanel(string gameObjectName, string friendlyName, string shortName, string panelParentGameObjectName)
        : base(gameObjectName, friendlyName, panelParentGameObjectName)
    {
        _shortName = shortName;
    }

    protected ElementPanel(GameObject gameObjectName, string friendlyName) 
        : base(gameObjectName, friendlyName)
    {
    }

    public void SetParameters(PanelParameters prms)
    {
        Parameters = prms;
    }

    public override bool Initialize()
    {
        try
        {
            if (RootObject == null)
            {
                Plugin.Log.LogWarning($"Failed to initialize UIElement: {Name}");
                return false;
            }

            OwnerCanvasScaler = RootObject.GetComponent<CanvasScaler>();

            if (OwnerCanvasScaler != null)
                OriginalScaleFactor = OwnerCanvasScaler.scaleFactor;
            Transform = RootObject.GetComponent<Transform>();
            if (OwnerCanvasScaler == null)
                OriginalScaleFactor = Transform.localScale.x;

            ConstructUI();

            return true;
        }
        catch
        {
            return false;
        }
    }

    protected override void ConstructUI()
    {
        // Get or add RectTransform
        CustomUIObject = UIFactory.CreateUIObject($"MarkPanel_{Name}", CustomPanelParentObject ?? RootObject);
        CustomUIRect = CustomUIObject.GetComponent<RectTransform>();
        CustomUIObject.SetActive(false);

        if (Parameters?.InheritAnchors == true)
        {
            // Inherit anchors from the parent object
            var from = CustomPanelParentObject != null
                ? CustomPanelParentObject.GetComponent<RectTransform>()
                : RootRect;
            CustomUIRect.anchorMin = from.anchorMin;
            CustomUIRect.anchorMax = from.anchorMax;
            CustomUIRect.pivot = from.pivot;
            CustomUIRect.sizeDelta = new Vector2(from.rect.width, from.rect.height);
            CustomUIRect.anchoredPosition = from.anchoredPosition;
        }
        else
        {
            // Set anchors manually using individual values instead of Vector2
            CustomUIRect.anchorMin = Vector2.zero;
            CustomUIRect.anchorMax = Vector2.one;
            CustomUIRect.anchoredPosition = Vector2.zero;
            CustomUIRect.sizeDelta = Vector2.zero;

            //CustomUIRect.offsetMin = Vector2.zero;
            //CustomUIRect.offsetMax = Vector2.zero;
            //CustomUIRect.pivot = new Vector2(0.5f, 0.5f);
        }

        if (PanelManager.ButtonBarFixList.Contains(Name))
        {
           // CustomUIRect.sizeDelta = new Vector2(50f, 50f);
        }

        if (PanelManager.RecipeTrackerFix.Equals(Name))
        {
            CustomUIRect.sizeDelta = new Vector2(50f, 50f);
        }

        ConstructDrag(CustomPanelParentObject ?? CustomUIObject ?? RootObject);


        // Add background image
        var bgImage = CustomUIObject.AddComponent<Image>();
        bgImage.type = Image.Type.Sliced;
        bgImage.color = Theme.PanelBackground;

        CoroutineUtility.StartCoroutine(SafeCreateContent());
    }

    private IEnumerator SafeCreateContent()
    {
        // Wait for a frame to ensure GameObject is fully initialized
        yield return null;

        try
        {
            if (CustomUIObject != null)
            {
                Outline = CustomUIObject.AddComponent<RectOutline>();
                if (Outline != null)
                {
                    Outline.OutlineColor = Theme.ElementOutlineColor;
                    Outline.LineWidth = 2f; // Adjust as needed
                }

                if (!string.IsNullOrEmpty(_shortName))
                {
                    // Create a header container at the top of the panel
                    var headerContainer = UIFactory.CreateUIObject($"HeaderContainer_{Name}", CustomUIObject);
                    if (headerContainer != null)
                    {
                        var headerRect = headerContainer.GetComponent<RectTransform>();
                        if (headerRect != null)
                        {
                            headerRect.anchorMin = new Vector2(0, 1);
                            headerRect.anchorMax = new Vector2(1, 1);
                            headerRect.pivot = new Vector2(0.5f, 1);
                            headerRect.sizeDelta = new Vector2(0, 30); // Fixed height
                            headerRect.anchoredPosition = Vector2.zero;
                        }

                        // Create the label with maximum width
                        var label = UIFactory.CreateLabel(headerContainer, $"NameLabel_{_shortName}", _shortName,
                            alignment: TextAlignmentOptions.Left,
                            fontSize: 12);

                        if (label != null && label.GameObject != null)
                        {
                            // Make the label take most of the width
                            var labelRect = label.GameObject.GetComponent<RectTransform>();
                            if (labelRect != null)
                            {
                                labelRect.anchorMin = new Vector2(0, 0);
                                labelRect.anchorMax = new Vector2(0.9f, 1);
                                labelRect.pivot = new Vector2(0, 0.5f);
                                labelRect.anchoredPosition = new Vector2(10, 0);
                                labelRect.sizeDelta = Vector2.zero;
                            }
                        }
                    }
                }

                // Activate the UI
                if (ConfigManager.IsModVisible)
                    CustomUIObject.SetActive(true);
                
                Outline?.SetActive(false);

                LoadConfigValues();
            }
        }
        catch (Exception ex)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogError($"Error in SafeCreateContent for {Name}: {ex.Message}");
        }
    }

    public void ApplyScale(float value)
    {
        if (OwnerCanvasScaler == null && Transform == null)
            return;

        try
        {
            if (OwnerCanvasScaler != null)
            {

                // Use reflection to set the scaleFactor property
                var scaleFactorField = AccessTools.Field(typeof(CanvasScaler), "m_ScaleFactor");
                if (scaleFactorField != null)
                {
                    scaleFactorField.SetValue(OwnerCanvasScaler, value);

                    // Force a canvas update
                    if (OwnerCanvas != null)
                    {
                        OwnerCanvas.scaleFactor = value;
                        Canvas.ForceUpdateCanvases();
                    }

                    Plugin.Log.LogInfo($"Scale updated to {value} for {Name}");
                }
                else
                {
                    Plugin.Log.LogWarning($"Could not find scaleFactor field for {Name}");
                }
            }
            else
            {
                Transform.localScale = new Vector3(value, value, 1f);
            }
            Save();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Error applying scale to {Name}: {ex.Message}");
        }
    }

    public void ApplyRotation(float value)
    {
        try
        {
            var eulerAngles = Transform.localEulerAngles;
            Transform.localEulerAngles = new Vector3(eulerAngles.x, eulerAngles.y, value);
            Save();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Error applying rotation to {Name}: {ex.Message}");
        }
    }

    public void ResetScale()
    {
        ApplyScale(OriginalScaleFactor);
    }

    public override void ShowPanelOutline(bool select)
    {
        if (Outline == null || !RootObject.activeSelf) return;
        Outline.SetActive(select);
    }

    public override void SetActive(bool value)
    {
        if(!RootObject.activeSelf) return;
        CustomUIObject.SetActive(value);
        if(!value && PanelManager.MainPanel.SelectedElementPanel == this)
            PanelManager.MainPanel.DeselectCurrentPanel();
        
        if (PanelManager.RecipeTrackerFix.Equals(Name))
        {
            CustomUIRect.sizeDelta = new Vector2(50f, 50f);
        }
    }

    public override void SetRootActive(bool value)
    {
        RootObject.SetActive(value);
        if (PanelManager.RecipeTrackerFix.Equals(Name))
        {
            CustomUIRect.sizeDelta = new Vector2(50f, 50f);
        }
        Save();
    }

    public override void Dispose()
    {
        if(CustomUIObject != null)
            Object.Destroy(CustomUIObject);
        Dragger?.Dispose();
    }
}