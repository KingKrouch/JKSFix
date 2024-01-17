using System;
using App;
using App.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace JKSFix;

public static partial class Patches
{
    // TODO: Fix AspectRatioFitter patch not working. This should be working, but it doesn't for some reason.
    // Definitely need to find a way to update the aspect ratio fitter whenever the screen resolution changes.
    [HarmonyPatch(typeof(UXBInGameUIController), nameof(UXBInGameUIController.Create), new Type[] { typeof(GameManager), typeof(WorldObjects) }), HarmonyPostfix]
    public static void AdjustUIAspectRatioScaling(UXBInGameUIController __instance)
    {
        var aspectRatioFitter = __instance.gameObject.GetComponent<AspectRatioFitter>();
        
        if (aspectRatioFitter == null)
            Debug.Log("Adding AspectRatioFitter to " + __instance.gameObject.name);
            aspectRatioFitter = __instance.gameObject.AddComponent<AspectRatioFitter>();
        
        AdjustAspectRatioFitterProperties(aspectRatioFitter);
    }

    public static void AdjustAspectRatioFitterProperties(AspectRatioFitter arf)
    {
        // Adjust the AspectMode just in case the component didn't exist before. Doesn't hurt to try.
        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        // Check if the display aspect ratio is less than 16:9, and if so, disable the AspectRatioFitter and use the old transforms.
        if (Screen.currentResolution.m_Width / Screen.currentResolution.m_Height >= 1920.0f / 1080.0f)
            arf.aspectRatio = 1920.0f / 1080.0f;
        else
            arf.aspectRatio = Screen.currentResolution.m_Width / (float)Screen.currentResolution.m_Height;
    }

    [HarmonyPatch(typeof(InGameCamera), nameof(InGameCamera._initialize)), HarmonyPostfix]
    public static void SwitchToMajorAxisFOVScaling(InGameCamera __instance)
    {
        var camera = __instance.gameObject.GetComponent<Camera>();
        if (camera == null) return;
        var oldFov = camera.fieldOfView;
        camera.usePhysicalProperties = true;
        camera.gateFit = Camera.GateFitMode.Overscan;
        camera.sensorSize = new Vector2(16, 9);
        camera.fieldOfView = oldFov;
    }
}