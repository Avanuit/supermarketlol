using UnityEngine;

/// <summary>
/// Una figura alta y negra con ojos rojos aparece al final de un pasillo cuando NO la miras.
/// Si la miras fijamente ~0.6 s, desaparece y todas las luces cercanas parpadean.
/// </summary>
public class ShadowFigure : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject model;                    // hijo con el cuerpo (se activa/desactiva)
    public Transform[] spawnPoints;             // posiciones (extremos de pasillo)
    public FlickerLight[] allLights;            // para provocar parpadeos
    public Vector2 waitRange = new Vector2(18f, 40f);
    public float minDistance = 7f;
    public float maxDistance = 22f;
    public float lookTimeToVanish = 0.6f;
    public float maxVisibleTime = 14f;

    float timer;
    float seenTime;
    float visibleTime;
    bool visible;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        model.SetActive(false);
        timer = Random.Range(waitRange.x, waitRange.y);
    }

    void Update()
    {
        if (!visible)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) TrySpawn();
            return;
        }

        visibleTime += Time.deltaTime;
        // se gira lentamente hacia el jugador
        Vector3 to = playerCamera.transform.position - transform.position; to.y = 0f;
        if (to.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 2f);

        if (IsLookedAt()) seenTime += Time.deltaTime; else seenTime = Mathf.Max(0f, seenTime - Time.deltaTime);

        if (seenTime >= lookTimeToVanish || visibleTime >= maxVisibleTime) Vanish(seenTime >= lookTimeToVanish);
    }

    void TrySpawn()
    {
        Vector3 p = playerCamera.transform.position;
        // elegir un punto lejano que NO este en el campo visual
        for (int tries = 0; tries < 12; tries++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            float dist = Vector3.Distance(sp.position, p);
            if (dist < minDistance || dist > maxDistance) continue;
            transform.position = sp.position;
            model.SetActive(true);
            if (IsLookedAt()) { model.SetActive(false); continue; }   // no aparecer delante de sus ojos
            visible = true;
            seenTime = 0f;
            visibleTime = 0f;
            return;
        }
        timer = 3f;   // reintentar pronto
    }

    bool IsLookedAt()
    {
        Vector3 head = transform.position + Vector3.up * 2f;
        Vector3 vp = playerCamera.WorldToViewportPoint(head);
        if (vp.z < 0f || vp.x < 0.12f || vp.x > 0.88f || vp.y < 0.0f || vp.y > 1f) return false;
        // si hay una repisa/pared en medio, no cuenta
        return !Physics.Linecast(playerCamera.transform.position, head);
    }

    void Vanish(bool provoked)
    {
        visible = false;
        model.SetActive(false);
        timer = Random.Range(waitRange.x, waitRange.y);
        if (provoked && allLights != null)
        {
            Vector3 p = transform.position;
            foreach (FlickerLight l in allLights)
                if (l != null && Vector3.Distance(l.transform.position, p) < 16f) l.TriggerBurst(Random.Range(0.8f, 1.6f));
        }
    }
}
