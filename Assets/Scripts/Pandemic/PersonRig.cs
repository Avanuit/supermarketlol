using UnityEngine;

/// <summary>Persona hecha con primitivas (piernas, torso, cabeza, brazos, ojos, pelo) y animacion procedural.</summary>
public class PersonRig : MonoBehaviour
{
    public Transform model, legL, legR, armL, armR;
    public float walkSpeed;        // m/s actuales (0 = quieto)
    public bool pushing;           // true = brazos al frente agarrando un carro
    public float reach;            // -1 brazo izquierdo extendido, +1 derecho, 0 ninguno (para tomar productos)
    public float restArm = 0f;     // angulo de brazos en reposo (cajeros)

    float phase;

    void Update()
    {
        float amp = Mathf.Clamp(walkSpeed * 9f, 0f, 48f);
        phase += Time.deltaTime * walkSpeed * 2.4f;
        float s = Mathf.Sin(phase);

        legL.localRotation = Quaternion.Euler(s * amp, 0f, 0f);
        legR.localRotation = Quaternion.Euler(-s * amp, 0f, 0f);

        float lean = Mathf.Clamp((walkSpeed - 3f) * 4f, 0f, 14f);          // se inclinan al correr
        model.localRotation = Quaternion.Euler(lean, 0f, 0f);
        Vector3 mp = model.localPosition;
        mp.y = walkSpeed > 0.05f ? Mathf.Abs(s) * 0.035f : 0f;
        model.localPosition = mp;

        if (pushing)
        {
            float l = reach < -0.5f ? 1f : 0f, r = reach > 0.5f ? 1f : 0f;
            armL.localRotation = Quaternion.Euler(-50f + l * 25f, 0f, -l * 80f);
            armR.localRotation = Quaternion.Euler(-50f + r * 25f, 0f, r * 80f);
        }
        else
        {
            armL.localRotation = Quaternion.Euler(restArm - s * amp * 0.8f, 0f, 0f);
            armR.localRotation = Quaternion.Euler(restArm + s * amp * 0.8f, 0f, 0f);
        }
    }
}

public static class PersonFactory
{
    static Material[] shirts, pants, skins, hairs;
    static Material eyeMat;

    static void Init()
    {
        if (shirts != null && shirts[0] != null) return;
        Color[] sc = {
            new Color(0.85f,0.2f,0.2f), new Color(0.2f,0.45f,0.85f), new Color(0.25f,0.7f,0.35f), new Color(0.95f,0.8f,0.2f),
            new Color(0.6f,0.3f,0.75f), new Color(0.95f,0.5f,0.15f), new Color(0.9f,0.9f,0.9f), new Color(0.15f,0.15f,0.2f),
            new Color(0.2f,0.75f,0.75f), new Color(0.9f,0.45f,0.6f), new Color(0.5f,0.5f,0.55f), new Color(0.55f,0.35f,0.2f) };
        Color[] pc = { new Color(0.15f,0.2f,0.4f), new Color(0.2f,0.2f,0.22f), new Color(0.45f,0.35f,0.25f), new Color(0.3f,0.4f,0.55f) };
        Color[] kc = { new Color(0.96f,0.8f,0.68f), new Color(0.85f,0.65f,0.5f), new Color(0.65f,0.45f,0.32f), new Color(0.45f,0.3f,0.2f), new Color(0.3f,0.2f,0.14f) };
        Color[] hc = { new Color(0.08f,0.06f,0.05f), new Color(0.3f,0.2f,0.1f), new Color(0.85f,0.7f,0.35f), new Color(0.6f,0.25f,0.1f), new Color(0.6f,0.6f,0.62f) };
        shirts = Build(sc, 0.3f); pants = Build(pc, 0.2f); skins = Build(kc, 0.3f); hairs = Build(hc, 0.4f);
        eyeMat = MatUtil.Create(new Color(0.05f, 0.05f, 0.05f), null, 0.6f);
    }

    static Material[] Build(Color[] cols, float smooth)
    {
        Material[] m = new Material[cols.Length];
        for (int i = 0; i < cols.Length; i++) { m[i] = MatUtil.Create(cols[i], null, smooth); m[i].enableInstancing = true; }
        return m;
    }

    static Material Pick(Material[] a) { return a[Random.Range(0, a.Length)]; }

    public static PersonRig Create(string name, Transform parent, Vector3 pos)
    {
        Init();
        GameObject root = new GameObject(name);
        if (parent != null) root.transform.SetParent(parent, false);
        root.transform.position = pos;
        PersonRig rig = root.AddComponent<PersonRig>();

        Material shirt = Pick(shirts), pant = Pick(pants), skin = Pick(skins), hair = Pick(hairs);

        GameObject model = new GameObject("modelo");
        model.transform.SetParent(root.transform, false);
        float h = Random.Range(0.93f, 1.08f);
        model.transform.localScale = new Vector3(h, h, h);
        rig.model = model.transform;

        rig.legL = Limb("pierna_izq", model.transform, new Vector3(-0.1f, 0.85f, 0f), new Vector3(0.17f, 0.43f, 0.17f), -0.42f, pant);
        rig.legR = Limb("pierna_der", model.transform, new Vector3(0.1f, 0.85f, 0f), new Vector3(0.17f, 0.43f, 0.17f), -0.42f, pant);
        rig.armL = Limb("brazo_izq", model.transform, new Vector3(-0.26f, 1.38f, 0f), new Vector3(0.09f, 0.27f, 0.09f), -0.26f, shirt);
        rig.armR = Limb("brazo_der", model.transform, new Vector3(0.26f, 1.38f, 0f), new Vector3(0.09f, 0.27f, 0.09f), -0.26f, shirt);

        Prim.Make(PrimitiveType.Capsule, "torso", model.transform, new Vector3(0f, 1.12f, 0f), new Vector3(0.40f, 0.30f, 0.24f), shirt);
        Prim.Make(PrimitiveType.Sphere, "cabeza", model.transform, new Vector3(0f, 1.58f, 0f), new Vector3(0.24f, 0.26f, 0.24f), skin);
        Prim.Make(PrimitiveType.Sphere, "pelo", model.transform, new Vector3(0f, 1.67f, -0.012f), new Vector3(0.255f, 0.14f, 0.26f), hair);
        Prim.Make(PrimitiveType.Sphere, "ojo_izq", model.transform, new Vector3(-0.05f, 1.60f, 0.105f), new Vector3(0.035f, 0.035f, 0.035f), eyeMat);
        Prim.Make(PrimitiveType.Sphere, "ojo_der", model.transform, new Vector3(0.05f, 1.60f, 0.105f), new Vector3(0.035f, 0.035f, 0.035f), eyeMat);
        return rig;
    }

    // pivote en la cadera/hombro; la capsula cuelga hacia abajo para que rote desde la articulacion
    static Transform Limb(string name, Transform parent, Vector3 pivotPos, Vector3 capsuleScale, float capsuleY, Material mat)
    {
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = pivotPos;
        Prim.Make(PrimitiveType.Capsule, "hueso", pivot.transform, new Vector3(0f, capsuleY, 0f), capsuleScale, mat);
        return pivot.transform;
    }
}
