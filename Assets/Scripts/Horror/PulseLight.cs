using UnityEngine;

/// <summary>Luz de emergencia roja que late (para el letrero de SALIDA).</summary>
public class PulseLight : MonoBehaviour
{
    public Light target;
    public float minIntensity = 0.2f;
    public float maxIntensity = 2.5f;
    public float speed = 1.6f;

    void Update()
    {
        if (target == null) return;
        float k = (Mathf.Sin(Time.time * speed * Mathf.PI) + 1f) * 0.5f;
        target.intensity = Mathf.Lerp(minIntensity, maxIntensity, k * k);
    }
}
