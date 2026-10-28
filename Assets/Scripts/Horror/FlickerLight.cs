using UnityEngine;

/// <summary>
/// Tubo fluorescente de terror. Controla (opcional) una Light y el brillo emisivo del tubo.
/// Modos: Steady (estable con zumbido), Flicker (rafagas de parpadeo), Dying (casi muerta), Dead (apagada).
/// </summary>
public class FlickerLight : MonoBehaviour
{
    public enum Mode { Steady, Flicker, Dying, Dead }

    public Mode mode = Mode.Flicker;
    public Light lightSource;                 // puede ser null (tubo solo emisivo)
    public Renderer tubeRenderer;             // el tubo visible (material con emision)
    public Color emissionColor = new Color(0.75f, 1f, 0.9f);
    public float lightIntensity = 2.2f;
    public float emissionIntensity = 2.2f;

    Material mat;
    bool isOn = true;
    float timer;
    int burstToggles;
    float forcedBurst;     // segundos de parpadeo forzado (lo activa la figura sombra)

    void Awake()
    {
        if (tubeRenderer != null) mat = tubeRenderer.material;   // instancia propia
        timer = Random.Range(0.2f, 4f);
        isOn = mode != Mode.Dead;
        Apply(isOn ? 1f : 0f);
    }

    public void TriggerBurst(float seconds)
    {
        if (mode == Mode.Dead) return;
        forcedBurst = Mathf.Max(forcedBurst, seconds);
    }

    void Update()
    {
        if (mode == Mode.Dead) { Apply(0f); return; }

        if (forcedBurst > 0f)
        {
            forcedBurst -= Time.deltaTime;
            Apply(Random.value < 0.5f ? 0f : Random.Range(0.4f, 1f));
            return;
        }

        switch (mode)
        {
            case Mode.Steady:
                // zumbido: variacion minima de intensidad
                Apply(0.94f + Mathf.PerlinNoise(Time.time * 9f, transform.position.x) * 0.06f);
                break;

            case Mode.Flicker:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    if (burstToggles > 0)
                    {
                        isOn = !isOn;
                        burstToggles--;
                        timer = Random.Range(0.03f, 0.13f);
                        if (burstToggles == 0) { isOn = true; timer = Random.Range(1.5f, 7f); }
                    }
                    else
                    {
                        burstToggles = Random.Range(3, 10);
                        timer = 0.02f;
                    }
                }
                Apply(isOn ? 1f : 0.02f);
                break;

            case Mode.Dying:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    isOn = !isOn;
                    timer = isOn ? Random.Range(0.05f, 0.5f) : Random.Range(0.8f, 5f);
                }
                Apply(isOn ? Random.Range(0.3f, 0.8f) : 0f);
                break;
        }
    }

    void Apply(float k)
    {
        if (lightSource != null)
        {
            lightSource.enabled = k > 0.01f;
            lightSource.intensity = lightIntensity * k;
        }
        if (mat != null) MatUtil.SetEmission(mat, emissionColor * (emissionIntensity * k));
    }
}
