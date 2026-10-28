using UnityEngine;

/// <summary>
/// Camara de timelapse con 4 vistas (tecla C): 0 orbita aerea, 1 cenital, 2 desde la puerta hacia los pasillos,
/// 3 sobre las cajas. Usa tiempo SIN escalar para que se mueva suave aunque aceleres la simulacion.
/// </summary>
public class TimelapseCamera : MonoBehaviour
{
    public int mode = 0;
    public Vector3 center = new Vector3(0f, 0f, 1f);
    public float orbitRadius = 26f;
    public float orbitHeight = 21f;
    public float orbitSpeed = 5f;      // grados por segundo
    public float smooth = 3f;

    float angle = 200f;
    bool snapped;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) mode = (mode + 1) % 4;
        float dt = Time.unscaledDeltaTime;

        Vector3 pos;
        Quaternion rot;
        switch (mode)
        {
            case 1:
                pos = new Vector3(0f, 42f, 1f);
                rot = Quaternion.Euler(90f, 0f, 0f);
                break;
            case 2:
                pos = new Vector3(0f, 3.4f, -17.6f);
                rot = Quaternion.LookRotation(new Vector3(0f, 1.0f, 12f) - pos);
                break;
            case 3:
                pos = new Vector3(0f, 7.5f, -2.5f);
                rot = Quaternion.LookRotation(new Vector3(0f, 0.3f, -14f) - pos);
                break;
            default:
                angle += orbitSpeed * dt;
                float a = angle * Mathf.Deg2Rad;
                pos = center + new Vector3(Mathf.Sin(a) * orbitRadius, orbitHeight, Mathf.Cos(a) * orbitRadius);
                rot = Quaternion.LookRotation(center - pos);
                break;
        }

        if (!snapped) { transform.position = pos; transform.rotation = rot; snapped = true; return; }
        float t = 1f - Mathf.Exp(-smooth * dt);
        transform.position = Vector3.Lerp(transform.position, pos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, t);
    }
}
