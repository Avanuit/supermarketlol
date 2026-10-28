using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class PandemicPhase
{
    public string name;
    public float duration = 30f;       // segundos (a velocidad x1)
    public float spawnPerSecond = 1f;  // clientes que llegan por segundo
    public bool panic;                 // corren, agarran de a varios, prefieren papel/gel/arroz/enlatados
    public float speedMin = 1.5f, speedMax = 2.0f;
    public int itemsMin = 3, itemsMax = 6;
    public bool alertBanner;           // muestra la noticia al iniciar la fase

    public PandemicPhase() { }
    public PandemicPhase(string name, float duration, float spawn, bool panic, float sMin, float sMax, int iMin, int iMax, bool banner)
    {
        this.name = name; this.duration = duration; spawnPerSecond = spawn; this.panic = panic;
        speedMin = sMin; speedMax = sMax; itemsMin = iMin; itemsMax = iMax; alertBanner = banner;
    }
}

/// <summary>
/// Director de la simulacion: fases, llegada de clientes, estadisticas, reloj simulado, HUD y controles.
/// Teclas: 1-5 velocidad (x1,x2,x4,x8,x16) | Espacio pausa | C cambia de camara | H oculta HUD | R reinicia.
/// </summary>
public class PandemicDirector : MonoBehaviour
{
    [Header("Referencias (las llena el generador)")]
    public ShelfUnit[] shelves;
    public CheckoutLane[] lanes;
    public Transform customersRoot;
    public Texture2D alertTexture;

    [Header("Carros")]
    public float cartMaxScale = 2.0f;
    public int itemsForMaxScale = 22;

    [Header("Simulacion")]
    public int maxCustomers = 140;
    public float clockMinutesPerSecond = 4f;
    public PandemicPhase[] phases = DefaultPhases();

    // ---- estadisticas ----
    int phaseIndex;
    float phaseTime;
    float spawnAcc;
    float simMinutes = 8f * 60f;
    int customersInside, served, emptyHanded, itemsTaken, itemsSold;
    int totalStock, remainingStock;
    float statTimer;
    bool paused, hideHud;
    float prevScale = 1f;
    GUIStyle big, mid, small, boxStyle;

    public static PandemicPhase[] DefaultPhases()
    {
        return new PandemicPhase[]
        {
            new PandemicPhase("Dia normal",                          25f, 0.35f, false, 1.5f, 2.0f,  3,  6, false),
            new PandemicPhase("ALERTA: se anuncia una pandemia",     12f, 1.40f, false, 2.5f, 4.0f,  6, 10, true),
            new PandemicPhase("PANICO EN EL SUPERMERCADO",           70f, 3.50f, true,  4.5f, 6.5f, 12, 22, false),
            new PandemicPhase("Estantes vacios",                     40f, 0.60f, true,  3.0f, 5.0f,  8, 15, false),
            new PandemicPhase("Fin de la simulacion",                 9999f, 0f, false, 2.0f, 3.0f,  1,  1, false),
        };
    }

    void Start()
    {
        foreach (ShelfUnit s in shelves) { totalStock += s.total; }
        remainingStock = totalStock;
        foreach (CheckoutLane l in lanes) l.director = this;
    }

    PandemicPhase Phase { get { return phases[Mathf.Min(phaseIndex, phases.Length - 1)]; } }

    void Update()
    {
        HandleKeys();
        float dt = Time.deltaTime;

        // fases
        phaseTime += dt;
        if (phaseTime >= Phase.duration && phaseIndex < phases.Length - 1) { phaseIndex++; phaseTime = 0f; spawnAcc = 0f; }

        // llegada de clientes
        spawnAcc += Phase.spawnPerSecond * dt;
        while (spawnAcc >= 1f)
        {
            spawnAcc -= 1f;
            if (customersInside < maxCustomers) { Customer.Create(this, Phase); customersInside++; }
        }

        simMinutes += dt * clockMinutesPerSecond;

        statTimer -= Time.unscaledDeltaTime;
        if (statTimer <= 0f)
        {
            statTimer = 0.4f;
            int r = 0;
            foreach (ShelfUnit s in shelves) r += s.remaining;
            remainingStock = r;
        }
    }

    void HandleKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetSpeed(1f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetSpeed(2f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetSpeed(4f);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetSpeed(8f);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetSpeed(16f);
        if (Input.GetKeyDown(KeyCode.H)) hideHud = !hideHud;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            paused = !paused;
            if (paused) { prevScale = Time.timeScale; Time.timeScale = 0f; } else Time.timeScale = prevScale;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void SetSpeed(float s) { paused = false; Time.timeScale = s; prevScale = s; }

    // ---------------------------------------------------------------- API para Customer / CheckoutLane
    public ShelfUnit PickShelf(bool panic)
    {
        for (int i = 0; i < 24; i++)
        {
            ShelfUnit s = shelves[Random.Range(0, shelves.Length)];
            if (s.remaining <= 0) continue;
            if (panic && !s.essential && Random.value < 0.6f) continue;
            return s;
        }
        int start = Random.Range(0, shelves.Length);
        for (int i = 0; i < shelves.Length; i++)
        {
            ShelfUnit s = shelves[(start + i) % shelves.Length];
            if (s.remaining > 0) return s;
        }
        return null;
    }

    public CheckoutLane ChooseLane()
    {
        CheckoutLane best = null;
        int bestCount = int.MaxValue;
        foreach (CheckoutLane l in lanes)
        {
            if (l.IsFull) continue;
            int c = l.queue.Count;
            if (c < bestCount || (c == bestCount && Random.value < 0.5f)) { best = l; bestCount = c; }
        }
        return best;
    }

    public void OnItemTaken() { itemsTaken++; }
    public void OnPaid(int items) { served++; itemsSold += items; }
    public void OnLeftEmptyHanded() { emptyHanded++; }
    public void OnCustomerLeft(Customer c) { customersInside--; }

    // ---------------------------------------------------------------- HUD
    void MakeStyles()
    {
        if (big != null) return;
        float k = Screen.height / 1080f;
        big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(44 * k), fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
        mid = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(24 * k), fontStyle = FontStyle.Bold };
        small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(18 * k) };
        boxStyle = new GUIStyle(GUI.skin.box);
    }

    void OnGUI()
    {
        if (hideHud) return;
        MakeStyles();
        float k = Screen.height / 1080f;
        PandemicPhase ph = Phase;

        // titulo de fase
        Color old = GUI.color;
        GUI.color = ph.panic ? new Color(1f, 0.35f, 0.3f) : (ph.alertBanner ? new Color(1f, 0.85f, 0.2f) : Color.white);
        big.normal.textColor = GUI.color;
        GUI.Label(new Rect(0, 14 * k, Screen.width, 70 * k), ph.name, big);
        GUI.color = old;

        // panel de estadisticas
        int h = Mathf.FloorToInt(simMinutes / 60f) % 24, m = Mathf.FloorToInt(simMinutes) % 60;
        int day = 1 + Mathf.FloorToInt(simMinutes / 1440f);
        int queued = 0;
        foreach (CheckoutLane l in lanes) queued += l.queue.Count;
        float stockPct = totalStock > 0 ? 100f * remainingStock / totalStock : 0f;

        Rect panel = new Rect(16 * k, 16 * k, 360 * k, 330 * k);
        GUI.Box(panel, GUIContent.none, boxStyle);
        GUILayout.BeginArea(new Rect(panel.x + 14 * k, panel.y + 10 * k, panel.width - 28 * k, panel.height - 20 * k));
        GUILayout.Label(string.Format("Dia {0}  {1:00}:{2:00}", day, h, m), mid);
        GUILayout.Label(string.Format("Clientes dentro: {0}", customersInside), small);
        GUILayout.Label(string.Format("En fila de cajas: {0}", queued), small);
        GUILayout.Label(string.Format("Productos tomados: {0}", itemsTaken), small);
        GUILayout.Label(string.Format("Compras pagadas: {0} ({1} items)", served, itemsSold), small);
        GUILayout.Label(string.Format("Se fueron sin nada: {0}", emptyHanded), small);
        GUILayout.Label(string.Format("Inventario en estantes: {0:0}%", stockPct), mid);
        GUILayout.Label(string.Format("Velocidad: x{0}{1}", paused ? 0 : Time.timeScale, paused ? "  (PAUSA)" : ""), small);
        GUILayout.EndArea();

        GUI.Label(new Rect(16 * k, Screen.height - 40 * k, Screen.width, 30 * k),
                  "1-5 velocidad · Espacio pausa · C camara · H ocultar HUD · R reiniciar", small);

        // noticia de alerta (primeros 7 s de la fase con banner)
        if (ph.alertBanner && alertTexture != null && phaseTime < 7f)
        {
            float w = 760 * k, hh = w * alertTexture.height / alertTexture.width;
            Rect r = new Rect(Screen.width - w - 20 * k, 20 * k, w, hh);
            float flash = (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.5f;
            GUI.color = new Color(1f, 0.3f + flash * 0.4f, 0.3f);
            GUI.DrawTexture(new Rect(r.x - 6 * k, r.y - 6 * k, r.width + 12 * k, r.height + 12 * k), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(r, alertTexture);
        }
        GUI.color = old;
    }
}
