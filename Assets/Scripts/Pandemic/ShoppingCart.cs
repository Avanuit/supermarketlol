using UnityEngine;

/// <summary>
/// Carro de compras con el PIVOTE en el manubrio (asi, cuando crece, se expande hacia adelante y arriba
/// y no atraviesa al cliente). Cada producto agregado aparece dentro con su etiqueta real.
/// </summary>
public class ShoppingCart : MonoBehaviour
{
    public float maxScale = 2.0f;
    public int itemsForMaxScale = 22;
    public int maxVisibleItems = 48;
    public int items;

    Transform content;
    float current = 1f, punch;
    static Material metal, plastic;

    static void InitMats()
    {
        if (metal != null) return;
        metal = MatUtil.Create(new Color(0.78f, 0.8f, 0.82f), null, 0.75f, 0.85f);
        plastic = MatUtil.Create(new Color(0.85f, 0.12f, 0.12f), null, 0.4f, 0f);
    }

    public static ShoppingCart Create(Transform parent)
    {
        InitMats();
        GameObject root = new GameObject("Carro");
        if (parent != null) root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, 0f, 0.42f);
        ShoppingCart c = root.AddComponent<ShoppingCart>();
        Transform t = root.transform;
        PrimitiveType cu = PrimitiveType.Cube;

        Prim.Make(cu, "base", t, new Vector3(0f, 0.22f, 0.5f), new Vector3(0.50f, 0.03f, 0.80f), metal);
        Prim.Make(cu, "fondo_canasta", t, new Vector3(0f, 0.46f, 0.5f), new Vector3(0.56f, 0.03f, 0.86f), metal);
        Prim.Make(cu, "pared_izq", t, new Vector3(-0.28f, 0.62f, 0.5f), new Vector3(0.025f, 0.30f, 0.86f), metal);
        Prim.Make(cu, "pared_der", t, new Vector3(0.28f, 0.62f, 0.5f), new Vector3(0.025f, 0.30f, 0.86f), metal);
        Prim.Make(cu, "pared_frente", t, new Vector3(0f, 0.62f, 0.93f), new Vector3(0.56f, 0.30f, 0.025f), metal);
        Prim.Make(cu, "pared_atras", t, new Vector3(0f, 0.57f, 0.07f), new Vector3(0.56f, 0.20f, 0.025f), metal);
        Prim.Make(cu, "manubrio", t, new Vector3(0f, 0.95f, 0f), new Vector3(0.58f, 0.035f, 0.035f), plastic);
        Prim.Make(cu, "soporte_izq", t, new Vector3(-0.27f, 0.72f, 0.04f), new Vector3(0.03f, 0.50f, 0.03f), metal, false, new Vector3(-12f, 0f, 0f));
        Prim.Make(cu, "soporte_der", t, new Vector3(0.27f, 0.72f, 0.04f), new Vector3(0.03f, 0.50f, 0.03f), metal, false, new Vector3(-12f, 0f, 0f));
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -0.22f : 0.22f), z = (i < 2 ? 0.15f : 0.85f);
            Prim.Make(PrimitiveType.Cylinder, "rueda", t, new Vector3(x, 0.07f, z), new Vector3(0.12f, 0.02f, 0.12f), plastic, false, new Vector3(0f, 0f, 90f));
        }

        GameObject content = new GameObject("contenido");
        content.transform.SetParent(t, false);
        c.content = content.transform;
        return c;
    }

    /// <summary>Agrega un producto (con su etiqueta) al carro y lo hace "inflarse".</summary>
    public void AddItem(string brandId)
    {
        items++;
        punch = 0.14f;
        if (content.childCount >= maxVisibleItems) return;

        Brand b = ProductCatalog.Find(brandId);
        if (b == null) b = ProductCatalog.RandomAny();
        int k = content.childCount;
        float layer = k / 10;                                  // 10 productos por capa
        Vector3 pos = new Vector3(Random.Range(-0.18f, 0.18f), 0.50f + b.h * 0.45f + layer * 0.13f, Random.Range(0.14f, 0.86f));
        Quaternion rot = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(0f, 360f), Random.Range(-25f, 25f));
        GameObject p = ProductFactory.Create(b, content, pos, rot);
        ProductFactory.SetShadows(p, false);
    }

    void Update()
    {
        float fill = Mathf.Clamp01(items / (float)itemsForMaxScale);
        float target = Mathf.Lerp(1f, maxScale, fill);
        current = Mathf.Lerp(current, target, Time.deltaTime * 5f);
        punch = Mathf.Lerp(punch, 0f, Time.deltaTime * 7f);
        float s = current + punch;
        transform.localScale = new Vector3(s, s, s);
    }
}
