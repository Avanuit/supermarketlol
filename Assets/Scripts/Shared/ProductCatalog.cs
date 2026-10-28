using System.Collections.Generic;
using UnityEngine;

public enum ProductShape { Box, Can, Bottle }

/// <summary>Una marca ficticia. El nombre del PNG en Resources/Labels es igual al id.</summary>
public class Brand
{
    public string id;
    public ProductShape shape;
    public bool essential;          // los clientes en panico prefieren estos (papel, gel, arroz, enlatados...)
    public float w, h, d;           // caja: ancho, alto, profundo | lata/botella: diametro = w, alto = h
    public Color cap = Color.white; // color de la tapa (solo botellas)

    public Brand(string id, ProductShape shape, bool essential, float w, float h, float d, Color? cap = null)
    {
        this.id = id; this.shape = shape; this.essential = essential;
        this.w = w; this.h = h; this.d = d;
        if (cap.HasValue) this.cap = cap.Value;
    }
}

public static class ProductCatalog
{
    public static readonly string[] RowNames = { "ENLATADOS", "CEREALES Y PASTAS", "ASEO Y SALUD", "BEBIDAS", "DESPENSA" };

    // Cada fila de repisas vende estas marcas (la fila 0 es el pasillo 1, etc.)
    public static readonly Brand[][] Rows =
    {
        new[] {
            new Brand("sopa",     ProductShape.Can,    true,  0.080f, 0.110f, 0f),
            new Brand("atun",     ProductShape.Can,    true,  0.085f, 0.085f, 0f),
            new Brand("frijoles", ProductShape.Can,    true,  0.080f, 0.115f, 0f),
        },
        new[] {
            new Brand("solaris",  ProductShape.Box,    false, 0.250f, 0.340f, 0.080f),
            new Brand("sonrisas", ProductShape.Box,    false, 0.230f, 0.320f, 0.080f),
            new Brand("pasta",    ProductShape.Box,    true,  0.150f, 0.280f, 0.060f),
            new Brand("galletas", ProductShape.Box,    false, 0.220f, 0.200f, 0.070f),
        },
        new[] {
            new Brand("papel",    ProductShape.Box,    true,  0.300f, 0.260f, 0.220f),
            new Brand("gel",      ProductShape.Bottle, true,  0.070f, 0.200f, 0f, new Color(0.9f, 0.9f, 0.9f)),
            new Brand("blancol",  ProductShape.Box,    false, 0.240f, 0.300f, 0.100f),
        },
        new[] {
            new Brand("fizz",     ProductShape.Bottle, false, 0.090f, 0.300f, 0f, new Color(0.8f, 0.05f, 0.1f)),
            new Brand("naranja",  ProductShape.Bottle, false, 0.085f, 0.270f, 0f, new Color(1f, 0.55f, 0f)),
            new Brand("leche",    ProductShape.Box,    true,  0.100f, 0.260f, 0.100f),
        },
        new[] {
            new Brand("arroz",    ProductShape.Box,    true,  0.150f, 0.250f, 0.080f),
            new Brand("cafe",     ProductShape.Box,    false, 0.130f, 0.200f, 0.070f),
            new Brand("aceite",   ProductShape.Bottle, true,  0.090f, 0.300f, 0f, new Color(0.9f, 0.8f, 0.1f)),
            new Brand("pan",      ProductShape.Box,    false, 0.300f, 0.200f, 0.110f),
        },
    };

    static Dictionary<string, Brand> _byId;
    public static Brand Find(string id)
    {
        if (_byId == null)
        {
            _byId = new Dictionary<string, Brand>();
            foreach (Brand[] row in Rows) foreach (Brand b in row) _byId[b.id] = b;
        }
        Brand r;
        return _byId.TryGetValue(id, out r) ? r : null;
    }

    public static Brand RandomAny()
    {
        Brand[] row = Rows[Random.Range(0, Rows.Length)];
        return row[Random.Range(0, row.Length)];
    }
}
