using App;
using Cpp2IL.Core.Analysis.ResultModels;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.InputSystem.Switch;

namespace JKSFix;

public static partial class Patches
{
    // TODO:
    // 1. Figure out why characters on the main menu are rotated in the incorrect way.
    // 2. Figure out the jittering camera rotation.

    [HarmonyPatch(typeof(App.ActorCartridge.ActorController), nameof(App.ActorCartridge.ActorController._initialize)), HarmonyPrefix]
    public static void AddInterpolationActor(App.ActorCartridge.ActorController __instance)
    {
        Debug.Log("ActorController found under: " + __instance.gameObject.name);
        // We need to explicitly check if it exists first, as for some reason, the component can be added twice and cause slow movement.
        var transformInterpolator = __instance.gameObject.GetComponent<TransformInterpolator>();
        if (transformInterpolator != null) return;
        transformInterpolator = __instance.gameObject.AddComponent<TransformInterpolator>();
        Debug.Log("Adding Transform Interpolator to " + __instance.gameObject.name);
    }

    [HarmonyPatch(typeof(InGameCamera), nameof(InGameCamera._initialize)), HarmonyPostfix]
    public static void FixInGameCamera(App.InGameCamera __instance)
    {
        // Run some checks to see if this is a CustomInGameCamera instance first before adding our modified component.
        if (__instance.GetType() == typeof(CustomInGameCamera)) return;
        var cig = __instance.gameObject.GetComponent<CustomInGameCamera>();
        if (cig != null) return;
        cig = __instance.gameObject.AddComponent<CustomInGameCamera>();
        // Copy all of the properties to our new camera.
        cig.m_activeCamera = __instance.m_activeCamera;
        cig.at = __instance.at;
        cig.m_cameraShake = __instance.m_cameraShake;
        cig.m_cameraSwitchAt = __instance.m_cameraSwitchAt;
        cig.m_cameraSwitchDuration = __instance.m_cameraSwitchDuration;
        cig.m_cameraSwitchPosition = __instance.m_cameraSwitchPosition;
        cig.m_cameraSwitchTimer = __instance.m_cameraSwitchTimer;
        cig.m_cameraSwitchUp = __instance.m_cameraSwitchUp;
        cig.m_cameraWallCorrection = __instance.m_cameraWallCorrection;
        cig.m_cameraZoom = __instance.m_cameraZoom;
        cig.m_controllers = __instance.m_controllers;
        cig.m_entryFuncDiposer = __instance.m_entryFuncDiposer;
        cig.m_lerpLockTimer = __instance.m_lerpLockTimer;
        cig.m_nextCamera = __instance.m_nextCamera;
        cig.m_position = __instance.m_position;
        cig.m_preCamera = __instance.m_preCamera;
        cig.m_up = __instance.m_up;

        // Disable the old instance.
        __instance.enabled = false;

        //// The old camera interpolation logic, for those interested.
        //Debug.Log("InGameCamera found under: " + __instance.gameObject.name);
        //// We need to explicitly check if it exists first, as for some reason, the component can be added twice and cause slow movement.
        //var transformInterpolator = __instance.gameObject.GetComponent<TransformInterpolator>();
        //if (transformInterpolator != null) return;
        //transformInterpolator = __instance.gameObject.AddComponent<TransformInterpolator>();
        //Debug.Log("Adding Transform Interpolator to " + __instance.gameObject.name);
    }

}

public class CustomInGameCamera : InGameCamera
{
    void Update()
    {
        _update(Time.deltaTime);
        _updateNormal(Time.deltaTime);
        _updateFixed(Time.deltaTime);
        return;
    }

    void FixedUpdate()
    {
        return;
    }
}

// This is a modified version of the interpolation controller as shown here: https://forum.unity.com/threads/motion-interpolation-solution-to-eliminate-fixedupdate-stutter.1325943/
/// How to use TransformInterpolator properly:
/// 1. Make sure the GameObject executes its mechanics (transform-manipulations) in FixedUpdate().
/// 2. Make sure VSYNC is enabled.
/// 3. Set the execution order for this script BEFORE all the other scripts that execute mechanics.
/// 4. Attach (and enable) this component to every GameObject that you want to interpolate.
/// (including the camera).
//[DefaultExecutionOrder(-2000)]
public class TransformInterpolator : MonoBehaviour
{
    private struct TransformData
    {
        public Vector3 position;
        public Vector3 scale;
        public Quaternion rotation;
    }

    private TransformData transformData;
    private TransformData prevTransformData;
    private bool isTransformInterpolated;

    void OnEnable()
    {
        prevTransformData.position = transform.localPosition;
        prevTransformData.rotation = transform.localRotation;
        prevTransformData.scale = transform.localScale;
        isTransformInterpolated = false;
    }

    void FixedUpdate()
    {
        if (isTransformInterpolated)
        {
            transform.localPosition = transformData.position;
            transform.localScale = transformData.scale;
            isTransformInterpolated = false;
        }
        
        prevTransformData.position = transform.localPosition;
        prevTransformData.rotation = transform.localRotation;
        prevTransformData.scale = transform.localScale;
    }

    void Update()
    {
        if (!isTransformInterpolated)
        {
            transformData.position = transform.localPosition;
            transformData.rotation = transform.localRotation;
            transformData.scale = transform.localScale;
            isTransformInterpolated = true;
        }
        
        var interpolationAlpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);

        // Interpolate transform:
        transform.localPosition = Vector3.Lerp(prevTransformData.position, transformData.position, interpolationAlpha);
        transform.localRotation = Quaternion.Lerp(prevTransformData.rotation, transformData.rotation, interpolationAlpha);
        transform.localScale = Vector3.Lerp(prevTransformData.scale, transformData.scale, interpolationAlpha);
    }
}