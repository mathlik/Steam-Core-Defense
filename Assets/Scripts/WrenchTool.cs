using UnityEngine;

/// <summary>Отображает гаечный ключ и обрабатывает удар левой кнопкой мыши.</summary>
public class WrenchTool : MonoBehaviour
{
    [SerializeField] private float damage = 30f;
    [SerializeField] private float range = 2.5f;
    [SerializeField] private float cooldown = 0.45f;

    private GameObject wrench;
    private AudioSource audioSource;
    private float nextSwingTime;
    private float swingTime;
    private bool isSwinging;
    private bool hitRegistered;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        CreateWrenchModel();
    }

    private void Update()
    {
        if (!isSwinging && Input.GetMouseButtonDown(0) && Time.time >= nextSwingTime)
        {
            StartSwing();
        }

        if (isSwinging)
        {
            AnimateSwing();
        }
    }

    private void StartSwing()
    {
        nextSwingTime = Time.time + cooldown;
        swingTime = 0f;
        isSwinging = true;
        hitRegistered = false;
        audioSource.PlayOneShot(CreateTone("WrenchSwing", 180f, 0.25f));
    }

    private void AnimateSwing()
    {
        swingTime += Time.deltaTime;
        float progress = Mathf.Clamp01(swingTime / 0.28f);
        float rotation = -110f + Mathf.Sin(progress * Mathf.PI) * 80f;
        wrench.transform.localRotation = Quaternion.Euler(rotation, 180f, 0f);

        if (!hitRegistered && progress > 0.35f)
        {
            hitRegistered = true;
            TryHitTarget();
        }

        if (progress >= 1f)
        {
            isSwinging = false;
            wrench.transform.localRotation = Quaternion.Euler(-110f, 180f, 0f);
        }
    }

    private void TryHitTarget()
    {
        if (Camera.main == null)
        {
            return;
        }

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, range))
        {
            return;
        }

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        target?.TakeDamage(damage);
        audioSource.PlayOneShot(CreateTone("WrenchHit", 65f, 0.8f));
    }

    private void CreateWrenchModel()
    {

        wrench = new GameObject("Wrench");
        wrench.transform.SetParent(transform, false);
        wrench.transform.localPosition = new Vector3(0.35f, -0.35f, 0.55f);
        wrench.transform.localRotation = Quaternion.Euler(-110f, 180f, 0f);
        wrench.transform.localScale = Vector3.one * 0.55f;

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.transform.SetParent(wrench.transform, false);
        handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        handle.transform.localScale = new Vector3(0.18f, 0.75f, 0.18f);

        GameObject jaw = GameObject.CreatePrimitive(PrimitiveType.Cube);
        jaw.transform.SetParent(wrench.transform, false);
        jaw.transform.localPosition = new Vector3(0f, 0f, 0.7f);
        jaw.transform.localScale = new Vector3(0.3f, 0.18f, 0.3f);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        handle.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = new Color(0.1f, 0.1f, 0.12f) };
        jaw.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = new Color(0.75f, 0.78f, 0.82f) };

        Destroy(handle.GetComponent<Collider>());
        Destroy(jaw.GetComponent<Collider>());
    }

    private AudioClip CreateTone(string name, float frequency, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = sampleRate / 8;
        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < samples.Length; i++)
        {
            float envelope = Mathf.Exp(-i / (float)(sampleCount / 5));
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * volume * envelope;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    private void ApplyWrenchMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material handleMaterial = new Material(shader) { color = new Color(0.08f, 0.09f, 0.11f) };
        Material metalMaterial = new Material(shader) { color = new Color(0.7f, 0.74f, 0.8f) };

        foreach (Renderer renderer in wrench.GetComponentsInChildren<Renderer>())
        {
            if (renderer.gameObject.name.Contains("Cylinder"))
            {
                renderer.sharedMaterial = handleMaterial;
            }
            else if (renderer.gameObject.name.Contains("Cube"))
            {
                renderer.sharedMaterial = metalMaterial;
            }
        }
    }

}