using System.Collections;
using UnityEngine;

namespace Extensions;

public static class RendererExtension
{
    private static readonly CoroutineHandler coroutineHandler;

    static RendererExtension()
    {
        coroutineHandler = new GameObject("CoroutineHandler").AddComponent<CoroutineHandler>();
    }

    public static void Flash(this Renderer renderer, Color color, Color returnColor, float time = 0.3f)
    {
        coroutineHandler.StartCoroutine(HighlightObject(renderer, color, returnColor, time));
    }

    private static IEnumerator HighlightObject(Renderer obj, Color color, Color returnColor, float time)
    {
        var renderersInChildren = obj.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderersInChildren)
        foreach (var material in renderer.materials)
        {
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 0.7f);
            material.color = color;
        }

        yield return new WaitForSeconds(time);
        foreach (var renderer in renderersInChildren)
        foreach (var material in renderer.materials)
        {
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", returnColor * 0f);
            material.color = returnColor;
        }
    }

    private class CoroutineHandler : MonoBehaviour
    {
    }
}