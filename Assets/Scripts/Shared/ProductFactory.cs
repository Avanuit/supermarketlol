using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fabrica productos con su etiqueta. Cajas = cubo. Latas y botellas = cilindro propio cuyo UV esta hecho
/// para que el CENTRO de la etiqueta quede mirando hacia +Z local (asi la marca siempre se ve de frente).
/// </summary>
public static class ProductFactory
{
    static Mesh _box, _cyl;
    static readonly Dictionary<string, Material> _brandMats = new Dictionary<string, Material>();
    static readonly Dictionary<int, Material> _capMats = new Dictionary<int, Material>();

    /// <summary>Limpia caches estaticos (lo llama el generador antes de reconstruir).</summary>
    public static void ResetCache()
    {
        _cyl = null;
        _brandMats.Clear();
        _capMats.Clear();
    }

    public static Mesh BoxMesh()
    {
        if (_box == null)
        {
            GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _box = t.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Object.Destroy(t); else Object.DestroyImmediate(t);
        }
        return _box;
    }

    public static Mesh CylinderMesh()
    {
        if (_cyl != null) return _cyl;
        const int seg = 24;
        List<Vector3> v = new List<Vector3>();
        List<Vector3> n = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> tris = new List<int>();

        // pared lateral: dos anillos (abajo y arriba), u de 0 a 1; u = 0.5 queda en +Z (frente)
        for (int i = 0; i <= seg; i++)
        {
            float u = (float)i / seg;
            float phi = (u - 0.5f) * 2f * Mathf.PI;
            float x = -Mathf.Sin(phi) * 0.5f;
            float z = Mathf.Cos(phi) * 0.5f;
            Vector3 nor = new Vector3(-Mathf.Sin(phi), 0f, Mathf.Cos(phi));
            v.Add(new Vector3(x, -0.5f, z)); n.Add(nor); uv.Add(new Vector2(u, 0.05f));
            v.Add(new Vector3(x, 0.5f, z)); n.Add(nor); uv.Add(new Vector2(u, 0.95f));
        }
        for (int i = 0; i < seg; i++)
        {
            int b0 = i * 2, t0 = i * 2 + 1, b1 = (i + 1) * 2, t1 = (i + 1) * 2 + 1;
            tris.Add(b0); tris.Add(t0); tris.Add(t1);
            tris.Add(b0); tris.Add(t1); tris.Add(b1);
        }
        // tapas (usan una franja lisa del PNG: arriba v=0.99, abajo v=0.01)
        for (int cap = 0; cap < 2; cap++)
        {
            float y = cap == 0 ? 0.5f : -0.5f;
            float vv = cap == 0 ? 0.99f : 0.01f;
            Vector3 nor = new Vector3(0f, cap == 0 ? 1f : -1f, 0f);
            int center = v.Count;
            v.Add(new Vector3(0f, y, 0f)); n.Add(nor); uv.Add(new Vector2(0.5f, vv));
            for (int i = 0; i <= seg; i++)
            {
                float phi = (float)i / seg * 2f * Mathf.PI;
                v.Add(new Vector3(-Mathf.Sin(phi) * 0.5f, y, Mathf.Cos(phi) * 0.5f)); n.Add(nor); uv.Add(new Vector2(0.5f, vv));
            }
            for (int i = 0; i < seg; i++)
            {
                if (cap == 0) { tris.Add(center); tris.Add(center + 2 + i); tris.Add(center + 1 + i); }
                else { tris.Add(center); tris.Add(center + 1 + i); tris.Add(center + 2 + i); }
            }
        }
        _cyl = new Mesh();
        _cyl.name = "ProductCylinder";
        _cyl.SetVertices(v);
        _cyl.SetNormals(n);
        _cyl.SetUVs(0, uv);
        _cyl.SetTriangles(tris, 0);
        _cyl.RecalculateBounds();
        return _cyl;
    }

    public static Material BrandMaterial(Brand b)
    {
        Material m;
        if (_brandMats.TryGetValue(b.id, out m) && m != null) return m;
        Texture2D tex = Resources.Load<Texture2D>("Labels/" + b.id);
        if (tex == null) Debug.LogWarning("Falta la etiqueta Resources/Labels/" + b.id + ".png");
        m = MatUtil.Create(Color.white, tex, 0.35f, 0f);
        m.name = "mat_" + b.id;
        m.enableInstancing = true;
        _brandMats[b.id] = m;
        return m;
    }

    static Material CapMaterial(Color c)
    {
        int key = c.GetHashCode();
        Material m;
        if (_capMats.TryGetValue(key, out m) && m != null) return m;
        m = MatUtil.Create(c, null, 0.6f, 0f);
        m.name = "mat_cap_" + ColorUtility.ToHtmlStringRGB(c);
        _capMats[key] = m;
        return m;
    }

    /// <summary>
    /// Crea un producto. localPos = CENTRO del producto. yawDeg gira el producto; localRot permite rotaciones libres
    /// (productos tirados en el piso o dentro de un carro).
    /// </summary>
    public static GameObject Create(Brand b, Transform parent, Vector3 localPos, Quaternion localRot)
    {
        GameObject root = new GameObject(b.id);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPos;
        root.transform.localRotation = localRot;

        if (b.shape == ProductShape.Box)
        {
            AddMesh(root, BoxMesh(), BrandMaterial(b));
            // el cubo mira hacia +X: la cara grande (ancho x alto) queda en el eje Z local
            root.transform.localScale = new Vector3(b.d, b.h, b.w);
        }
        else if (b.shape == ProductShape.Can)
        {
            AddMesh(root, CylinderMesh(), BrandMaterial(b));
            root.transform.localScale = new Vector3(b.w, b.h, b.w);
        }
        else // botella: cuerpo + tapa (hermanos para que el escalado no deforme la tapa)
        {
            GameObject body = new GameObject("cuerpo");
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(b.w, b.h, b.w);
            AddMesh(body, CylinderMesh(), BrandMaterial(b));

            GameObject cap = new GameObject("tapa");
            cap.transform.SetParent(root.transform, false);
            cap.transform.localPosition = new Vector3(0f, b.h * 0.5f + 0.02f, 0f);
            cap.transform.localScale = new Vector3(b.w * 0.45f, 0.04f, b.w * 0.45f);
            AddMesh(cap, CylinderMesh(), CapMaterial(b.cap));
        }
        return root;
    }

    static void AddMesh(GameObject go, Mesh mesh, Material mat)
    {
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
    }

    public static void SetShadows(GameObject root, bool cast)
    {
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
        {
            r.shadowCastingMode = cast ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
