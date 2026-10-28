using UnityEngine;

/// <summary>
/// Sonido 100% procedural (no necesitas importar audios): zumbido grave constante, golpes lejanos al azar
/// y pasos. Pon este script en un objeto vacio "Ambiente" y arrastra al Jugador en 'player'.
/// </summary>
public class HorrorAmbience : MonoBehaviour
{
    public static HorrorAmbience Instance;

    public Transform player;
    [Range(0f, 1f)] public float droneVolume = 0.35f;
    [Range(0f, 1f)] public float thumpVolume = 0.9f;
    public Vector2 thumpInterval = new Vector2(10f, 28f);

    AudioSource droneSource, stepSource;
    AudioClip droneClip, thumpClip, stepClip;
    float nextThump;

    const int Rate = 44100;

    void Awake()
    {
        Instance = this;
        QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 8);   // Built-in: mas luces por pixel
        droneClip = BuildDrone();
        thumpClip = BuildThump();
        stepClip = BuildStep();

        droneSource = gameObject.AddComponent<AudioSource>();
        droneSource.clip = droneClip;
        droneSource.loop = true;
        droneSource.volume = droneVolume;
        droneSource.spatialBlend = 0f;
        droneSource.Play();

        stepSource = gameObject.AddComponent<AudioSource>();
        stepSource.spatialBlend = 0f;
        stepSource.volume = 0.5f;

        nextThump = Time.time + Random.Range(thumpInterval.x, thumpInterval.y);
    }

    void Update()
    {
        if (player != null && Time.time >= nextThump)
        {
            nextThump = Time.time + Random.Range(thumpInterval.x, thumpInterval.y);
            Vector2 c = Random.insideUnitCircle.normalized * Random.Range(7f, 15f);
            Vector3 pos = player.position + new Vector3(c.x, 1f, c.y);
            AudioSource.PlayClipAtPoint(thumpClip, pos, thumpVolume);
        }
    }

    public void Footstep(bool running)
    {
        stepSource.pitch = Random.Range(0.8f, 1.05f) * (running ? 1.15f : 1f);
        stepSource.PlayOneShot(stepClip, running ? 0.8f : 0.5f);
    }

    // ---- generadores de audio ----
    AudioClip BuildDrone()
    {
        int n = Rate * 4;                       // 4 s en bucle (frecuencias con ciclos enteros => sin click)
        float[] d = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float s = Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.30f
                    + Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.10f
                    + Mathf.Sin(2f * Mathf.PI * 61.25f * t) * 0.18f      // batimiento con 50 Hz
                    + Mathf.Sin(2f * Mathf.PI * 0.25f * t) * 0.05f;
            lp += ((Random.value * 2f - 1f) - lp) * 0.02f;             // ruido filtrado = "aire"
            float w = Mathf.Sin(Mathf.PI * i / n);
            d[i] = s + lp * 0.5f * w * w;
        }
        AudioClip c = AudioClip.Create("drone", n, 1, Rate, false);
        c.SetData(d, 0);
        return c;
    }

    AudioClip BuildThump()
    {
        int n = (int)(Rate * 0.9f);
        float[] d = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            lp += ((Random.value * 2f - 1f) - lp) * 0.05f;
            d[i] = (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.9f + lp * 1.2f) * Mathf.Exp(-5f * t);
        }
        AudioClip c = AudioClip.Create("thump", n, 1, Rate, false);
        c.SetData(d, 0);
        return c;
    }

    AudioClip BuildStep()
    {
        int n = (int)(Rate * 0.14f);
        float[] d = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            lp += ((Random.value * 2f - 1f) - lp) * 0.12f;
            d[i] = (lp * 1.6f + Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.5f) * Mathf.Exp(-28f * t);
        }
        AudioClip c = AudioClip.Create("step", n, 1, Rate, false);
        c.SetData(d, 0);
        return c;
    }
}
