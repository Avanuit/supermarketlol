using UnityEngine;

/// <summary>
/// Jugador en primera persona: WASD, mouse, Shift correr, F linterna, Esc libera el cursor.
/// Requiere en el proyecto: Project Settings > Player > Active Input Handling = Both (o Input Manager Old).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Transform cameraTransform;
    public Light flashlight;                 // spot light hija de la camara
    public float walkSpeed = 2.4f;
    public float runSpeed = 4.4f;
    public float mouseSensitivity = 2.0f;
    public float gravity = -20f;

    [Header("Cabeceo al caminar")]
    public float bobAmount = 0.035f;
    public float bobFrequency = 1.9f;

    CharacterController cc;
    float pitch;
    float verticalVel;
    float bobTimer;
    float camBaseY;
    bool lastStepSide;
    float flashBaseIntensity;
    float flashStutterTimer;
    float nextStutterIn;
    Quaternion flashRot;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        camBaseY = cameraTransform.localPosition.y;
        if (flashlight != null)
        {
            flashBaseIntensity = flashlight.intensity;
            flashRot = flashlight.transform.rotation;
        }
        nextStutterIn = Random.Range(6f, 14f);
        LockCursor(true);
    }

    void LockCursor(bool on)
    {
        Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !on;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // --- mirar ---
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(0f, mx, 0f);
        pitch = Mathf.Clamp(pitch - my, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // --- moverse ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool running = Input.GetKey(KeyCode.LeftShift);
        float speed = running ? runSpeed : walkSpeed;
        Vector3 move = (transform.right * h + transform.forward * v);
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (cc.isGrounded && verticalVel < 0f) verticalVel = -2f;
        verticalVel += gravity * Time.deltaTime;
        Vector3 vel = move * speed + Vector3.up * verticalVel;
        cc.Move(vel * Time.deltaTime);

        // --- cabeceo y pasos ---
        bool moving = move.sqrMagnitude > 0.01f && cc.isGrounded;
        if (moving)
        {
            float prev = Mathf.Sin(bobTimer);
            bobTimer += Time.deltaTime * bobFrequency * (running ? 1.5f : 1f) * Mathf.PI * 2f * 0.5f;
            float cur = Mathf.Sin(bobTimer);
            if ((prev > 0f) != (cur > 0f))
            {
                lastStepSide = !lastStepSide;
                if (HorrorAmbience.Instance != null) HorrorAmbience.Instance.Footstep(running);
            }
        }
        else bobTimer = Mathf.Lerp(bobTimer, Mathf.Round(bobTimer / Mathf.PI) * Mathf.PI, Time.deltaTime * 6f);
        Vector3 lp = cameraTransform.localPosition;
        lp.y = camBaseY + Mathf.Sin(bobTimer) * bobAmount * (running ? 1.4f : 1f);
        cameraTransform.localPosition = lp;

        // --- linterna ---
        if (flashlight != null)
        {
            if (Input.GetKeyDown(KeyCode.F)) flashlight.enabled = !flashlight.enabled;

            // la linterna "sigue" la camara con un pequeno retraso (se siente pesada)
            flashRot = Quaternion.Slerp(flashRot, cameraTransform.rotation, Time.deltaTime * 14f);
            flashlight.transform.rotation = flashRot;

            // parpadeo ocasional de la linterna
            nextStutterIn -= Time.deltaTime;
            if (nextStutterIn <= 0f)
            {
                flashStutterTimer = Random.Range(0.25f, 0.7f);
                nextStutterIn = Random.Range(8f, 22f);
            }
            if (flashStutterTimer > 0f)
            {
                flashStutterTimer -= Time.deltaTime;
                flashlight.intensity = flashBaseIntensity * (Random.value < 0.5f ? 0.1f : 0.9f);
            }
            else flashlight.intensity = flashBaseIntensity;
        }
    }
}
