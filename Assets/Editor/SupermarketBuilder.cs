using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Genera el supermercado completo con un clic:  menu  Supermercado > 1) ESCENA 1 (terror)  /  2) ESCENA 2 (pandemia).
/// Crea estructura, repisas con productos etiquetados, cajas, luces, carteles, y los objetos jugables / de simulacion.
/// Todo queda en la jerarquia como objetos normales, para que lo puedas editar a mano.
/// </summary>
public static class SupermarketBuilder
{
    static readonly float[] BoardY = { 0.10f, 0.50f, 0.90f, 1.30f, 1.70f, 1.98f };
    const PrimitiveType Cube = PrimitiveType.Cube;

    [MenuItem("Supermercado/1) Construir ESCENA 1 - Terror nocturno")]
    static void MenuHorror() { Build(true); }

    [MenuItem("Supermercado/2) Construir ESCENA 2 - Pandemia timelapse")]
    static void MenuPandemic() { Build(false); }

    // =============================================================================================
    static void Build(bool horror)
    {
        if (Resources.Load<Texture2D>("Labels/sopa") == null)
        {
            EditorUtility.DisplayDialog("Faltan las etiquetas",
                "No encuentro Assets/Resources/Labels/sopa.png.\nCopia la carpeta Assets/Resources/Labels del paquete a tu proyecto y vuelve a intentar.", "OK");
            return;
        }

        string sceneTag = horror ? "Escena1_Terror" : "Escena2_Pandemia";
        GameObject old = GameObject.Find("Supermercado");
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Ya existe un 'Supermercado'", "Se borrara y se reconstruira. ?Continuar?", "Si", "Cancelar")) return;
            Object.DestroyImmediate(old);
        }
        string genFolder = "Assets/Generated/" + sceneTag;
        if (AssetDatabase.IsValidFolder(genFolder)) AssetDatabase.DeleteAsset(genFolder);
        ProductFactory.ResetCache();
        Random.InitState(horror ? 1313 : 2020);

        GameObject rootGo = new GameObject("Supermercado");
        Transform root = rootGo.transform;

        BuildEnvironment(horror);
        BuildShell(root, horror);
        List<ShelfUnit> shelves = BuildShelves(root, horror);
        List<CheckoutLane> lanes = BuildCheckouts(root, !horror);
        List<FlickerLight> flickers = new List<FlickerLight>();
        BuildFridges(root, horror, flickers);
        BuildSigns(root, horror);

        if (horror)
        {
            BuildCeilingLights(root, flickers);
            BuildHorrorDressing(root);
            BuildHorrorPlayer(root, flickers);
        }
        else
        {
            BuildPandemicRuntime(root, shelves, lanes);
        }

        // guardar materiales y mallas generados como assets (si no, se pierden al cerrar Unity)
        PersistAssets(rootGo, genFolder);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = rootGo;
        Debug.Log("[Supermercado] " + sceneTag + " construida. Guarda la escena con Ctrl+S.");
    }

    // =============================================================================================
    // Helpers
    // =============================================================================================
    static Transform Group(string name, Transform parent)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    static Material Mat(string name, Color c, string tex = null, float tx = 1f, float ty = 1f, float smooth = 0.3f, float metal = 0f)
    {
        Texture2D t = tex != null ? Resources.Load<Texture2D>("Labels/" + tex) : null;
        Material m = MatUtil.Create(c, t, smooth, metal);
        m.name = name;
        if (t != null) m.mainTextureScale = new Vector2(tx, ty);
        return m;
    }

    static Material Emissive(string name, Color c, Color em, string tex = null)
    {
        Texture2D t = tex != null ? Resources.Load<Texture2D>("Labels/" + tex) : null;
        Material m = MatUtil.CreateEmissive(c, em, t);
        m.name = name;
        return m;
    }

    static Light AddLight(Transform parent, string name, LightType type, Vector3 localPos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        Light l = go.AddComponent<Light>();
        l.type = type;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        return l;
    }

    // =============================================================================================
    // Ambiente y luz global
    // =============================================================================================
    static void BuildEnvironment(bool horror)
    {
        Light[] all = Object.FindObjectsOfType<Light>();
        if (horror)
        {
            foreach (Light l in all) if (l.type == LightType.Directional) l.gameObject.SetActive(false);   // sin sol: es de noche
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.045f, 0.06f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.035f);
            RenderSettings.fogDensity = 0.04f;
            RenderSettings.reflectionIntensity = 0f;        // sin reflejos del cielo del dia
        }
        else
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.68f);
            RenderSettings.fog = false;
            Light sun = null;
            foreach (Light l in all) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null)
            {
                GameObject g = new GameObject("Sol");
                sun = g.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.gameObject.SetActive(true);
            sun.color = new Color(1f, 0.97f, 0.9f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        }
    }

    // =============================================================================================
    // Estructura: piso, techo, paredes, puerta, calle
    // =============================================================================================
    static void BuildShell(Transform root, bool horror)
    {
        Transform t = Group("Estructura", root);
        float W = StoreLayout.HalfWidth * 2f;
        float L = StoreLayout.BackZ - StoreLayout.FrontZ;
        float cz = (StoreLayout.BackZ + StoreLayout.FrontZ) * 0.5f;
        float H = StoreLayout.Height;

        Material floor = Mat("piso", Color.white, "floor_tile", W / 2f, L / 2f, horror ? 0.6f : 0.45f);
        Prim.Make(Cube, "Piso", t, new Vector3(0f, -0.05f, cz), new Vector3(W, 0.1f, L), floor, true);

        Material wallSide = Mat("pared_lado", Color.white, "wall", L / 2f, 2f, 0.1f);
        Material wallFront = Mat("pared_frente", Color.white, "wall", W / 2f, 2f, 0.1f);
        Prim.Make(Cube, "Pared_Izq", t, new Vector3(-StoreLayout.HalfWidth - 0.1f, H / 2f, cz), new Vector3(0.2f, H, L), wallSide, true);
        Prim.Make(Cube, "Pared_Der", t, new Vector3(StoreLayout.HalfWidth + 0.1f, H / 2f, cz), new Vector3(0.2f, H, L), wallSide, true);
        Prim.Make(Cube, "Pared_Fondo", t, new Vector3(0f, H / 2f, StoreLayout.BackZ + 0.1f), new Vector3(W + 0.4f, H, 0.2f), wallFront, true);

        float segW = StoreLayout.HalfWidth - StoreLayout.DoorHalfWidth;
        float segX = StoreLayout.DoorHalfWidth + segW / 2f;
        Prim.Make(Cube, "Pared_Frente_Izq", t, new Vector3(-segX, H / 2f, StoreLayout.FrontZ - 0.1f), new Vector3(segW, H, 0.2f), wallFront, true);
        Prim.Make(Cube, "Pared_Frente_Der", t, new Vector3(segX, H / 2f, StoreLayout.FrontZ - 0.1f), new Vector3(segW, H, 0.2f), wallFront, true);
        Prim.Make(Cube, "Dintel_Puerta", t, new Vector3(0f, (3f + H) / 2f, StoreLayout.FrontZ - 0.1f), new Vector3(StoreLayout.DoorHalfWidth * 2f, H - 3f, 0.2f), wallFront, true);

        // exterior: asfalto, vereda y rayas de parqueadero
        Material asphalt = Mat("asfalto", new Color(0.75f, 0.75f, 0.75f), "asphalt", 80f, 80f, 0.15f);
        Prim.Make(Cube, "Asfalto", t, new Vector3(0f, -0.07f, 0f), new Vector3(160f, 0.1f, 160f), asphalt, false);
        Material side = Mat("vereda", new Color(0.55f, 0.55f, 0.55f), null, 1, 1, 0.2f);
        Prim.Make(Cube, "Vereda", t, new Vector3(0f, -0.06f, StoreLayout.FrontZ - 2.3f), new Vector3(40f, 0.12f, 4.6f), side, false);
        Material white = Mat("raya", new Color(0.9f, 0.9f, 0.9f), null, 1, 1, 0.1f);
        for (int i = -5; i <= 5; i++)
            Prim.Make(Cube, "Raya_" + i, t, new Vector3(i * 2.8f, -0.015f, -30f), new Vector3(0.12f, 0.01f, 4.5f), white, false);

        if (horror)
        {
            Material ceil = Mat("techo", Color.white, "ceiling_tile", W / 1.2f, L / 1.2f, 0.1f);
            Prim.Make(Cube, "Techo", t, new Vector3(0f, H + 0.05f, cz), new Vector3(W, 0.1f, L), ceil, false);

            // puerta de vidrio oscuro (cerrada, no se puede salir)
            Material glass = Mat("vidrio_oscuro", new Color(0.02f, 0.03f, 0.04f), null, 1, 1, 0.92f, 0.2f);
            Material frame = Mat("marco_puerta", new Color(0.35f, 0.37f, 0.4f), null, 1, 1, 0.5f, 0.8f);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim.Make(Cube, "Vidrio_" + s, t, new Vector3(s * 1.0f, 1.5f, StoreLayout.FrontZ), new Vector3(1.9f, 3f, 0.05f), glass, true);
                Prim.Make(Cube, "Manija_" + s, t, new Vector3(s * 0.15f, 1.1f, StoreLayout.FrontZ + 0.08f), new Vector3(0.04f, 0.5f, 0.04f), frame, false);
            }
            Prim.Make(Cube, "Marco_Centro", t, new Vector3(0f, 1.5f, StoreLayout.FrontZ), new Vector3(0.1f, 3f, 0.08f), frame, false);
            Prim.Make(Cube, "Marco_Izq", t, new Vector3(-2f, 1.5f, StoreLayout.FrontZ), new Vector3(0.1f, 3f, 0.1f), frame, false);
            Prim.Make(Cube, "Marco_Der", t, new Vector3(2f, 1.5f, StoreLayout.FrontZ), new Vector3(0.1f, 3f, 0.1f), frame, false);

            // luz de luna fria entrando por la puerta
            GameObject moon = new GameObject("Luz_Luna_Exterior");
            moon.transform.SetParent(t, false);
            moon.transform.position = new Vector3(0f, 3.2f, -26f);
            moon.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            Light ml = moon.AddComponent<Light>();
            ml.type = LightType.Spot;
            ml.color = new Color(0.45f, 0.6f, 1f);
            ml.intensity = 2.5f;
            ml.range = 24f;
            ml.spotAngle = 45f;
            ml.shadows = LightShadows.None;
        }
    }

    // =============================================================================================
    // Repisas con productos
    // =============================================================================================
    static List<ShelfUnit> BuildShelves(Transform root, bool horror)
    {
        List<ShelfUnit> list = new List<ShelfUnit>();
        Transform shelvesRoot = Group("Repisas", root);

        Material frameMat = Mat("repisa_marco", new Color(0.45f, 0.5f, 0.56f), null, 1, 1, 0.5f, 0.7f);
        Material boardMat = Mat("repisa_tabla", new Color(0.86f, 0.88f, 0.9f), null, 1, 1, 0.4f, 0.2f);
        Material backMat = Mat("repisa_fondo", new Color(0.9f, 0.88f, 0.8f), null, 1, 1, 0.2f, 0f);
        Material priceMat = Mat("repisa_precios", Color.white, "price_strip", 1f, 1f, 0.2f);

        for (int row = 0; row < StoreLayout.RowX.Length; row++)
        {
            Transform rowT = Group("Fila_" + (row + 1) + "_" + ProductCatalog.RowNames[row], shelvesRoot);
            Brand[] brands = ProductCatalog.Rows[row];
            bool essentialRow = row == 0 || row == 2 || row == 4;

            for (int u = 0; u < StoreLayout.UnitsPerRow; u++)
            {
                GameObject unit = new GameObject("Modulo_" + (u + 1));
                unit.transform.SetParent(rowT, false);
                unit.transform.position = new Vector3(StoreLayout.RowX[row], 0f, StoreLayout.UnitZ(u));
                BoxCollider bc = unit.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 1f, 0f);
                bc.size = new Vector3(1.0f, 2.0f, 2.0f);
                Transform ut = unit.transform;

                Prim.Make(Cube, "fondo", ut, new Vector3(0f, 1f, 0f), new Vector3(0.05f, 2.0f, 1.98f), backMat);
                Prim.Make(Cube, "poste_a", ut, new Vector3(0f, 1f, -0.99f), new Vector3(0.98f, 2.0f, 0.04f), frameMat);
                Prim.Make(Cube, "poste_b", ut, new Vector3(0f, 1f, 0.99f), new Vector3(0.98f, 2.0f, 0.04f), frameMat);
                for (int l = 0; l < BoardY.Length; l++)
                    Prim.Make(Cube, "tabla_" + l, ut, new Vector3(0f, BoardY[l], 0f), new Vector3(0.96f, 0.04f, 1.96f), boardMat);

                for (int s = -1; s <= 1; s += 2)
                {
                    // tiras de precios en el borde frontal de cada tabla
                    for (int l = 0; l < 5; l++)
                        Prim.Make(PrimitiveType.Quad, "precios", ut, new Vector3(s * 0.482f, BoardY[l], 0f), new Vector3(1.9f, 0.035f, 1f), priceMat, false,
                                  new Vector3(0f, s > 0 ? -90f : 90f, 0f));

                    GameObject face = new GameObject(s > 0 ? "cara_der" : "cara_izq");
                    face.transform.SetParent(ut, false);
                    ShelfUnit su = face.AddComponent<ShelfUnit>();
                    su.rowIndex = row;
                    su.side = s;
                    su.aisleX = StoreLayout.RowX[row] + s * 2.25f;
                    su.zCenter = StoreLayout.UnitZ(u);
                    su.essential = essentialRow;
                    FillSide(face.transform, su, brands, s, horror);
                    su.Finish();
                    list.Add(su);
                }

                if (horror) MarkStatic(unit);
            }
        }
        return list;
    }

    static void FillSide(Transform sideT, ShelfUnit su, Brand[] brands, int side, bool gaps)
    {
        for (int l = 0; l < 5; l++)
        {
            Brand a = brands[Random.Range(0, brands.Length)];
            Brand b = brands[Random.Range(0, brands.Length)];
            float split = Random.Range(-0.3f, 0.5f);
            float z = -0.93f;
            while (true)
            {
                Brand br = z < split ? a : b;
                float w = br.w;
                if (z + w > 0.95f) break;
                if (!gaps || Random.value > 0.12f)
                {
                    float depth = br.shape == ProductShape.Box ? br.d : br.w;
                    float x = side * (0.48f - depth * 0.5f - 0.03f);
                    float y = BoardY[l] + 0.02f + br.h * 0.5f;
                    Quaternion rot = br.shape == ProductShape.Box
                        ? Quaternion.Euler(0f, Random.Range(-4f, 4f), 0f)
                        : Quaternion.Euler(0f, (side > 0 ? 90f : -90f) + Random.Range(-8f, 8f), 0f);
                    GameObject p = ProductFactory.Create(br, sideT, new Vector3(x, y, z + w * 0.5f), rot);
                    ProductFactory.SetShadows(p, false);
                    su.items.Add(p);
                }
                z += w + 0.012f;
            }
        }
    }

    static void MarkStatic(GameObject go)
    {
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
    }

    // =============================================================================================
    // Cajas registradoras
    // =============================================================================================
    static List<CheckoutLane> BuildCheckouts(Transform root, bool withLanes)
    {
        List<CheckoutLane> lanes = new List<CheckoutLane>();
        Transform t = Group("Cajas", root);
        Material body = Mat("caja_cuerpo", new Color(0.85f, 0.87f, 0.9f), null, 1, 1, 0.4f, 0.1f);
        Material top = Mat("caja_tope", new Color(0.12f, 0.13f, 0.15f), null, 1, 1, 0.5f, 0.2f);
        Material belt = Mat("caja_cinta", new Color(0.06f, 0.06f, 0.07f), null, 1, 1, 0.2f, 0f);
        Material reg = Mat("caja_registradora", new Color(0.18f, 0.19f, 0.22f), null, 1, 1, 0.5f, 0.3f);
        Material screen = Emissive("caja_pantalla", new Color(0.05f, 0.2f, 0.1f), new Color(0.2f, 1f, 0.5f) * 0.8f);
        Material tape = Mat("cinta_amarilla", new Color(0.95f, 0.8f, 0.1f), null, 1, 1, 0.2f, 0f);
        Material post = Mat("poste_cartel", new Color(0.25f, 0.27f, 0.3f), null, 1, 1, 0.4f, 0.5f);

        for (int i = 0; i < StoreLayout.LaneX.Length; i++)
        {
            float x = StoreLayout.LaneX[i];
            float z = StoreLayout.CheckoutZ;
            GameObject lane = new GameObject("Caja_" + (i + 1));
            lane.transform.SetParent(t, false);
            lane.transform.position = new Vector3(x, 0f, z);
            Transform lt = lane.transform;

            Prim.Make(Cube, "mostrador", lt, new Vector3(0f, 0.47f, 0f), new Vector3(1.5f, 0.94f, 0.75f), body, true);
            Prim.Make(Cube, "tope", lt, new Vector3(0f, 0.96f, 0f), new Vector3(1.56f, 0.04f, 0.8f), top);
            Prim.Make(Cube, "cinta_transportadora", lt, new Vector3(-0.25f, 0.985f, 0.62f), new Vector3(0.7f, 0.03f, 1.25f), belt);
            Prim.Make(Cube, "registradora", lt, new Vector3(0.45f, 1.1f, -0.05f), new Vector3(0.38f, 0.24f, 0.32f), reg);
            Prim.Make(Cube, "pantalla", lt, new Vector3(0.45f, 1.28f, -0.1f), new Vector3(0.3f, 0.2f, 0.03f), screen, false, new Vector3(-20f, 0f, 0f));
            Prim.Make(Cube, "marca_suelo", lt, new Vector3(0f, 0.004f, StoreLayout.QueueStartZ - z + 0.5f), new Vector3(0.9f, 0.006f, 0.07f), tape);
            Prim.Make(Cube, "poste_izq", lt, new Vector3(-0.55f, 1.3f, -0.45f), new Vector3(0.04f, 2.6f, 0.04f), post);
            Prim.Make(Cube, "poste_der", lt, new Vector3(0.55f, 1.3f, -0.45f), new Vector3(0.04f, 2.6f, 0.04f), post);
            GameObject sign = Prim.Make(Cube, "cartel_caja", lt, new Vector3(0f, 2.6f, -0.45f), new Vector3(1.2f, 0.6f, 0.06f),
                                        Mat("cartel_caja_" + (i + 1), Color.white, "sign_caja_" + (i + 1), 1, 1, 0.2f), false);
            sign.transform.rotation = Quaternion.identity;

            if (withLanes)
            {
                CheckoutLane cl = lane.AddComponent<CheckoutLane>();
                cl.laneX = x;
                lanes.Add(cl);
            }
        }
        return lanes;
    }

    // =============================================================================================
    // Refrigeradores del fondo
    // =============================================================================================
    static void BuildFridges(Transform root, bool horror, List<FlickerLight> flickers)
    {
        Transform t = Group("Refrigeradores", root);
        Material body = Mat("fri_cuerpo", new Color(0.82f, 0.85f, 0.88f), null, 1, 1, 0.5f, 0.3f);
        Material trim = Mat("fri_marco", new Color(0.2f, 0.22f, 0.25f), null, 1, 1, 0.4f, 0.4f);
        for (int i = 0; i < 11; i++)
        {
            float x = -11f + i * 2.2f;
            Transform ft = Group("Refri_" + (i + 1), t);
            ft.position = new Vector3(x, 0f, StoreLayout.BackZ - 0.45f);
            Prim.Make(Cube, "cuerpo", ft, new Vector3(0f, 1.1f, 0f), new Vector3(2.1f, 2.2f, 0.8f), body, true);
            Prim.Make(Cube, "marco", ft, new Vector3(0f, 1.1f, -0.405f), new Vector3(2.0f, 2.1f, 0.02f), trim);
            GameObject glass = Prim.Make(PrimitiveType.Quad, "vidrio_luz", ft, new Vector3(0f, 1.1f, -0.42f), new Vector3(1.9f, 2.0f, 1f),
                                         Emissive("fri_panel_" + i, Color.white, new Color(0.9f, 0.95f, 1f) * 0.9f, "fridge_front"));
            Prim.Make(Cube, "manija", ft, new Vector3(0.85f, 1.1f, -0.44f), new Vector3(0.04f, 0.8f, 0.04f), trim);

            if (horror)
            {
                FlickerLight fl = glass.AddComponent<FlickerLight>();
                fl.tubeRenderer = glass.GetComponent<Renderer>();
                fl.emissionColor = new Color(0.8f, 0.92f, 1f);
                fl.emissionIntensity = 0.9f;
                fl.lightSource = null;
                float r = Random.value;
                fl.mode = r < 0.15f ? FlickerLight.Mode.Dead : (r < 0.55f ? FlickerLight.Mode.Flicker : FlickerLight.Mode.Steady);
                flickers.Add(fl);
            }
        }
    }

    // =============================================================================================
    // Carteles
    // =============================================================================================
    static GameObject SignBox(Transform parent, string name, string tex, Vector3 pos, Vector3 scale, float yaw, bool emissive, float em = 1f)
    {
        Material m = emissive ? Emissive("cartel_" + name, Color.white, Color.white * em, tex) : Mat("cartel_" + name, Color.white, tex, 1, 1, 0.2f);
        return Prim.Make(Cube, name, parent, pos, scale, m, false, new Vector3(0f, yaw, 0f));
    }

    static void BuildSigns(Transform root, bool horror)
    {
        Transform t = Group("Carteles", root);
        Material post = Mat("poste_pasillo", new Color(0.25f, 0.27f, 0.3f), null, 1, 1, 0.4f, 0.5f);

        // letrero exterior
        SignBox(t, "letrero_exterior", "sign_supermercado", new Vector3(0f, 3.5f, StoreLayout.FrontZ - 0.25f), new Vector3(6f, 1.5f, 0.1f), 0f, true, horror ? 0.35f : 0.9f);
        // letreros de pasillo (sobre postes, miran hacia la entrada)
        for (int r = 0; r < StoreLayout.RowX.Length; r++)
        {
            float x = StoreLayout.RowX[r];
            SignBox(t, "pasillo_" + (r + 1), "sign_pasillo_" + (r + 1), new Vector3(x, 3.1f, 1.1f), new Vector3(2.4f, 0.6f, 0.05f), 0f, false);
            Prim.Make(Cube, "poste_a_" + r, t, new Vector3(x - 1.15f, 1.55f, 1.1f), new Vector3(0.06f, 3.1f, 0.06f), post);
            Prim.Make(Cube, "poste_b_" + r, t, new Vector3(x + 1.15f, 1.55f, 1.1f), new Vector3(0.06f, 3.1f, 0.06f), post);
        }
        // salida
        SignBox(t, "salida", "sign_salida", new Vector3(0f, 3.4f, StoreLayout.FrontZ + 0.2f), new Vector3(1.4f, 0.47f, 0.05f), 0f, true, 1.3f);
        // ofertas
        SignBox(t, "ofertas", "sign_ofertas", new Vector3(-8.5f, 2.3f, StoreLayout.FrontZ + 0.15f), new Vector3(2.6f, 1.3f, 0.05f), 0f, false);

        if (horror)
        {
            SignBox(t, "cerrado", "sign_cerrado", new Vector3(1.0f, 1.7f, StoreLayout.FrontZ + 0.05f), new Vector3(0.7f, 0.35f, 0.02f), 0f, false);
            Light red = AddLight(t, "Luz_Emergencia_Salida", LightType.Point, new Vector3(0f, 3.0f, StoreLayout.FrontZ + 1.2f), new Color(1f, 0.1f, 0.05f), 2f, 10f);
            PulseLight pl = red.gameObject.AddComponent<PulseLight>();
            pl.target = red;
        }
    }

    // =============================================================================================
    // Terror: fluorescentes
    // =============================================================================================
    static void BuildCeilingLights(Transform root, List<FlickerLight> flickers)
    {
        Transform t = Group("Fluorescentes", root);
        Material housing = Mat("fluo_carcasa", new Color(0.2f, 0.21f, 0.23f), null, 1, 1, 0.3f, 0.5f);
        Material tube = Emissive("fluo_tubo", new Color(0.9f, 0.95f, 0.95f), new Color(0.75f, 1f, 0.9f) * 2.2f);
        float H = StoreLayout.Height;
        int count = 0;
        for (int ai = 0; ai < StoreLayout.AisleX.Length; ai++)
        {
            for (int zi = 0; zi < 10; zi++)
            {
                float x = StoreLayout.AisleX[ai];
                float z = -16f + zi * 4f;
                Transform ft = Group("Fluo_" + ai + "_" + zi, t);
                ft.position = new Vector3(x, 0f, z);
                Prim.Make(Cube, "carcasa", ft, new Vector3(0f, H - 0.05f, 0f), new Vector3(0.22f, 0.05f, 1.5f), housing);
                GameObject tubeGo = Prim.Make(Cube, "tubo", ft, new Vector3(0f, H - 0.095f, 0f), new Vector3(0.14f, 0.03f, 1.4f), tube);

                FlickerLight fl = ft.gameObject.AddComponent<FlickerLight>();
                fl.tubeRenderer = tubeGo.GetComponent<Renderer>();
                float r = Random.value;
                fl.mode = r < 0.40f ? FlickerLight.Mode.Dead : (r < 0.65f ? FlickerLight.Mode.Steady : (r < 0.85f ? FlickerLight.Mode.Flicker : FlickerLight.Mode.Dying));

                if (fl.mode != FlickerLight.Mode.Dead && Random.value < 0.75f)
                {
                    Light l = AddLight(ft, "luz", LightType.Point, new Vector3(0f, H - 0.4f, 0f), new Color(0.78f, 0.95f, 0.88f), 2.4f, 7.5f);
                    fl.lightSource = l;
                    fl.lightIntensity = 2.4f;
                    count++;
                }
                flickers.Add(fl);
            }
        }
        Debug.Log("[Supermercado] Fluorescentes con luz real: " + count);
    }

    // =============================================================================================
    // Terror: suciedad, afiches, productos tirados, carros volcados
    // =============================================================================================
    static void BuildHorrorDressing(Transform root)
    {
        Transform t = Group("Ambientacion_Terror", root);

        // manchas en el piso
        Material[] stain = {
            Cutout("mancha_1", "stain_1"), Cutout("mancha_2", "stain_2"), Cutout("mancha_3", "stain_3") };
        for (int i = 0; i < 16; i++)
        {
            float x = StoreLayout.AisleX[Random.Range(0, StoreLayout.AisleX.Length)] + Random.Range(-1f, 1f);
            float z = Random.Range(-11f, 19f);
            float s = Random.Range(0.9f, 2.3f);
            Prim.Make(PrimitiveType.Quad, "mancha", t, new Vector3(x, 0.012f + i * 0.0005f, z), new Vector3(s, s, 1f), stain[i % 3], false,
                      new Vector3(90f, Random.Range(0f, 360f), 0f));
        }

        // manos ensangrentadas en paredes y refrigeradores
        Material hand = Cutout("mano", "handprint");
        for (int i = 0; i < 9; i++)
        {
            float s = Random.Range(0.3f, 0.45f);
            Prim.Make(PrimitiveType.Quad, "mano_" + i, t, new Vector3(-10.5f + i * 2.6f + Random.Range(-0.4f, 0.4f), Random.Range(0.9f, 1.8f), StoreLayout.BackZ - 0.9f - 0.012f),
                      new Vector3(s, s, 1f), hand, false, new Vector3(0f, 0f, Random.Range(-25f, 25f)));
        }
        for (int i = 0; i < 4; i++)
        {
            float z = Random.Range(-6f, 14f);
            float s = Random.Range(0.3f, 0.45f);
            Prim.Make(PrimitiveType.Quad, "mano_izq_" + i, t, new Vector3(-StoreLayout.HalfWidth + 0.01f, Random.Range(1.0f, 1.9f), z), new Vector3(s, s, 1f), hand, false, new Vector3(0f, -90f, Random.Range(-20f, 20f)));
            Prim.Make(PrimitiveType.Quad, "mano_der_" + i, t, new Vector3(StoreLayout.HalfWidth - 0.01f, Random.Range(1.0f, 1.9f), z + 1.5f), new Vector3(s, s, 1f), hand, false, new Vector3(0f, 90f, Random.Range(-20f, 20f)));
        }

        // afiches de "desaparecido"
        Material poster = Mat("afiche_desaparecido", Color.white, "poster_desaparecido", 1, 1, 0.1f);
        Prim.Make(PrimitiveType.Quad, "afiche_1", t, new Vector3(-StoreLayout.HalfWidth + 0.02f, 1.6f, -8f), new Vector3(0.6f, 0.9f, 1f), poster, false, new Vector3(0f, -90f, 2f));
        Prim.Make(PrimitiveType.Quad, "afiche_2", t, new Vector3(StoreLayout.HalfWidth - 0.02f, 1.5f, 3f), new Vector3(0.6f, 0.9f, 1f), poster, false, new Vector3(0f, 90f, -3f));
        Prim.Make(PrimitiveType.Quad, "afiche_3", t, new Vector3(-5.5f, 1.5f, StoreLayout.FrontZ + 0.02f), new Vector3(0.6f, 0.9f, 1f), poster, false, new Vector3(0f, 180f, 1f));
        Prim.Make(PrimitiveType.Quad, "afiche_4", t, new Vector3(5.5f, 1.6f, StoreLayout.FrontZ + 0.02f), new Vector3(0.6f, 0.9f, 1f), poster, false, new Vector3(0f, 180f, -2f));
        Prim.Make(PrimitiveType.Quad, "afiche_5", t, new Vector3(0f, 1.4f, StoreLayout.BackZ - 0.02f - 0.9f), new Vector3(0.6f, 0.9f, 1f), poster, false);

        // productos tirados
        Transform spill = Group("Productos_Tirados", t);
        for (int i = 0; i < 70; i++)
        {
            Brand b = ProductCatalog.RandomAny();
            float x = StoreLayout.AisleX[Random.Range(0, StoreLayout.AisleX.Length)] + Random.Range(-1.3f, 1.3f);
            float z = Random.Range(0.5f, 19.5f);
            Quaternion rot = Quaternion.Euler(Random.value < 0.5f ? 90f : 0f, Random.Range(0f, 360f), Random.value < 0.5f ? 90f : 0f);
            GameObject p = ProductFactory.Create(b, spill, new Vector3(x, Mathf.Max(0.05f, b.w * 0.5f), z), rot);
            ProductFactory.SetShadows(p, false);
        }

        // carros abandonados / volcados
        Vector3[] cartPos = { new Vector3(-2.25f, 0.34f, 6f), new Vector3(6.75f, 0.30f, 11f), new Vector3(2.25f, 0f, -6f), new Vector3(-6.75f, 0.34f, 15f) };
        float[] cartRoll = { 85f, -80f, 0f, 90f };
        for (int i = 0; i < cartPos.Length; i++)
        {
            ShoppingCart c = ShoppingCart.Create(t);
            c.gameObject.name = "Carro_Abandonado_" + i;
            c.transform.localPosition = cartPos[i];
            c.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), cartRoll[i]);
            Object.DestroyImmediate(c);      // es utileria: no necesita el script
        }
    }

    static Material Cutout(string name, string tex)
    {
        Material m = Mat(name, Color.white, tex, 1, 1, 0.35f);
        MatUtil.MakeCutout(m);
        return m;
    }

    // =============================================================================================
    // Terror: jugador, audio, figura, polvo
    // =============================================================================================
    static void BuildHorrorPlayer(Transform root, List<FlickerLight> flickers)
    {
        GameObject player = new GameObject("Jugador");
        player.transform.SetParent(root, false);
        player.transform.position = new Vector3(0f, 0.05f, -17.2f);
        player.transform.rotation = Quaternion.identity;
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f); cc.stepOffset = 0.3f;

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject g = new GameObject("Main Camera");
            g.tag = "MainCamera";
            cam = g.AddComponent<Camera>();
            g.AddComponent<AudioListener>();
        }
        cam.transform.SetParent(player.transform, false);
        cam.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        cam.transform.localRotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.015f, 0.02f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 80f;
        cam.fieldOfView = 70f;

        // linterna
        GameObject fl = new GameObject("Linterna");
        fl.transform.SetParent(cam.transform, false);
        fl.transform.localPosition = new Vector3(0.12f, -0.1f, 0.15f);
        Light spot = fl.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(1f, 0.96f, 0.85f);
        spot.intensity = 3.2f;
        spot.range = 24f;
        spot.spotAngle = 52f;
        spot.innerSpotAngle = 24f;
        spot.shadows = LightShadows.Soft;
        spot.shadowStrength = 0.9f;

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.cameraTransform = cam.transform;
        pc.flashlight = spot;

        // audio procedural
        GameObject amb = new GameObject("Ambiente_Sonido");
        amb.transform.SetParent(root, false);
        HorrorAmbience ha = amb.AddComponent<HorrorAmbience>();
        ha.player = player.transform;

        // figura sombra
        GameObject fig = new GameObject("Figura_Sombra");
        fig.transform.SetParent(root, false);
        GameObject model = new GameObject("Modelo");
        model.transform.SetParent(fig.transform, false);
        Material black = Mat("sombra", new Color(0.01f, 0.01f, 0.01f), null, 1, 1, 0f);
        Material eye = Emissive("sombra_ojos", Color.black, new Color(1f, 0.05f, 0.02f) * 4f);
        Prim.Make(PrimitiveType.Capsule, "cuerpo", model.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.45f, 1.1f, 0.3f), black);
        Prim.Make(PrimitiveType.Sphere, "cabeza", model.transform, new Vector3(0f, 2.35f, 0f), new Vector3(0.3f, 0.36f, 0.3f), black);
        Prim.Make(PrimitiveType.Capsule, "brazo_izq", model.transform, new Vector3(-0.34f, 1.05f, 0f), new Vector3(0.09f, 0.75f, 0.09f), black, false, new Vector3(0f, 0f, 4f));
        Prim.Make(PrimitiveType.Capsule, "brazo_der", model.transform, new Vector3(0.34f, 1.05f, 0f), new Vector3(0.09f, 0.75f, 0.09f), black, false, new Vector3(0f, 0f, -4f));
        Prim.Make(PrimitiveType.Sphere, "ojo_izq", model.transform, new Vector3(-0.07f, 2.38f, 0.13f), new Vector3(0.06f, 0.035f, 0.04f), eye);
        Prim.Make(PrimitiveType.Sphere, "ojo_der", model.transform, new Vector3(0.07f, 2.38f, 0.13f), new Vector3(0.06f, 0.035f, 0.04f), eye);

        Transform spRoot = Group("Puntos_Aparicion", root);
        List<Transform> sps = new List<Transform>();
        foreach (float ax in StoreLayout.AisleX)
        {
            foreach (float az in new float[] { 3f, 18.5f })
            {
                Transform sp = Group("sp", spRoot);
                sp.position = new Vector3(ax, 0f, az);
                sps.Add(sp);
            }
        }
        ShadowFigure sf = fig.AddComponent<ShadowFigure>();
        sf.playerCamera = cam;
        sf.model = model;
        sf.spawnPoints = sps.ToArray();
        sf.allLights = flickers.ToArray();

        // polvo flotando
        GameObject dust = new GameObject("Polvo");
        dust.transform.SetParent(player.transform, false);
        dust.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        ParticleSystem ps = dust.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 8f;
        main.startSpeed = 0.03f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = new Color(0.8f, 0.88f, 0.9f, 0.3f);
        main.maxParticles = 500;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 60f;
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(14f, 4f, 14f);
        ParticleSystem.NoiseModule nz = ps.noise;
        nz.enabled = true;
        nz.strength = 0.15f;
        ParticleSystemRenderer pr = dust.GetComponent<ParticleSystemRenderer>();
        Material dm = new Material(Shader.Find("Sprites/Default"));
        dm.name = "polvo";
        dm.mainTexture = Resources.Load<Texture2D>("Labels/dust");
        pr.sharedMaterial = dm;
    }

    // =============================================================================================
    // Pandemia: cajeros, TV, director, camara
    // =============================================================================================
    static void BuildPandemicRuntime(Transform root, List<ShelfUnit> shelves, List<CheckoutLane> lanes)
    {
        Transform t = Group("Pandemia", root);

        // cajeros detras de cada mostrador
        for (int i = 0; i < StoreLayout.LaneX.Length; i++)
        {
            PersonRig cashier = PersonFactory.Create("Cajero_" + (i + 1), t, new Vector3(StoreLayout.LaneX[i], 0f, StoreLayout.CheckoutZ - 0.95f));
            cashier.restArm = -30f;
        }

        // TV con la noticia (pared de la entrada)
        Prim.Make(PrimitiveType.Quad, "TV_Noticia", t, new Vector3(5.5f, 2.5f, StoreLayout.FrontZ + 0.03f), new Vector3(3.2f, 1.8f, 1f),
                  Emissive("tv_noticia", Color.white, Color.white * 0.9f, "poster_pandemia"), false, new Vector3(0f, 180f, 0f));
        Prim.Make(PrimitiveType.Quad, "TV_Noticia_2", t, new Vector3(-12f, 2.5f, -9f), new Vector3(3.2f, 1.8f, 1f),
                  Emissive("tv_noticia_2", Color.white, Color.white * 0.9f, "poster_pandemia"), false, new Vector3(0f, -90f, 0f));
        t.gameObject.name = "Pandemia";

        GameObject clients = new GameObject("Clientes");
        clients.transform.SetParent(root, false);

        GameObject dirGo = new GameObject("Director_Pandemia");
        dirGo.transform.SetParent(root, false);
        PandemicDirector dir = dirGo.AddComponent<PandemicDirector>();
        dir.shelves = shelves.ToArray();
        dir.lanes = lanes.ToArray();
        dir.customersRoot = clients.transform;
        dir.alertTexture = Resources.Load<Texture2D>("Labels/poster_pandemia");

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject g = new GameObject("Main Camera");
            g.tag = "MainCamera";
            cam = g.AddComponent<Camera>();
            g.AddComponent<AudioListener>();
        }
        cam.fieldOfView = 50f;
        cam.farClipPlane = 300f;
        if (cam.GetComponent<TimelapseCamera>() == null) cam.gameObject.AddComponent<TimelapseCamera>();
    }

    // =============================================================================================
    // Guardar materiales y mallas como assets
    // =============================================================================================
    static void PersistAssets(GameObject root, string folder)
    {
        EnsureFolder("Assets/Generated");
        EnsureFolder(folder);
        EnsureFolder(folder + "/Materials");
        EnsureFolder(folder + "/Meshes");

        HashSet<Material> mats = new HashSet<Material>();
        HashSet<Mesh> meshes = new HashSet<Mesh>();
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            foreach (Material m in r.sharedMaterials)
                if (m != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))) mats.Add(m);
        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mf.sharedMesh))) meshes.Add(mf.sharedMesh);

        int i = 0;
        foreach (Material m in mats)
            AssetDatabase.CreateAsset(m, folder + "/Materials/" + Safe(m.name) + "_" + (i++) + ".mat");
        i = 0;
        foreach (Mesh m in meshes)
            AssetDatabase.CreateAsset(m, folder + "/Meshes/" + Safe(m.name) + "_" + (i++) + ".asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string name = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }

    static string Safe(string n)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
        return string.IsNullOrEmpty(n) ? "asset" : n;
    }
}
