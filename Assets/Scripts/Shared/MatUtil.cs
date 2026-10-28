using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Crea materiales que funcionan igual en URP y en el pipeline Built-in.</summary>
public static class MatUtil
{
    static Shader _lit;

    public static Shader LitShader()
    {
        if (_lit != null) return _lit;
        if (GraphicsSettings.currentRenderPipeline != null)
            _lit = Shader.Find("Universal Render Pipeline/Lit");
        if (_lit == null) _lit = Shader.Find("Standard");
        return _lit;
    }

    public static Material Create(Color color, Texture tex = null, float smooth = 0.25f, float metallic = 0f)
    {
        Material m = new Material(LitShader());
        m.color = color;
        if (tex != null) m.mainTexture = tex;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);   // URP
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);   // Built-in
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        return m;
    }

    public static Material CreateEmissive(Color color, Color emission, Texture emissionMap = null)
    {
        Material m = Create(color, null, 0.4f, 0f);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetColor("_EmissionColor", emission);
        if (emissionMap != null)
        {
            m.SetTexture("_EmissionMap", emissionMap);
            m.mainTexture = emissionMap;
        }
        return m;
    }

    public static void SetEmission(Material m, Color c)
    {
        m.SetColor("_EmissionColor", c);
    }

    /// <summary>Recorte duro por alfa (para manchas y huellas con fondo transparente).</summary>
    public static void MakeCutout(Material m)
    {
        if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
        if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 1f);
        m.SetOverrideTag("RenderType", "TransparentCutout");
        m.EnableKeyword("_ALPHATEST_ON");
        m.renderQueue = 2450;
    }
}

/// <summary>Atajo para crear primitivas sin colisionador (a menos que lo pidas).</summary>
public static class Prim
{
    public static GameObject Make(PrimitiveType type, string name, Transform parent, Vector3 localPos,
                                  Vector3 localScale, Material mat, bool collider = false, Vector3? euler = null)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c);
            }
        }
        return go;
    }
}
