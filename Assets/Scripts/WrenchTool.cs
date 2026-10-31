using UnityEngine;

/// <summary>Отображает гаечный ключ и обрабатывает удар левой кнопкой мыши.</summary>
public class WrenchTool : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField, Min(0f)] private float damage = 30f;
    [SerializeField, Min(0f)] private float range = 2.5f;
    [SerializeField, Min(0f)] private float cooldown = 0.45f;
    [SerializeField, Min(0.01f)] private float swingDuration = 0.28f;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField, Min(0.01f)] private float hitRadius = 0.18f;
    [SerializeField, Min(0.01f)] private float sparkDuration = 0.12f;

    [Header("Wrench Appearance")]
    [Tooltip("Поворот ключа в состоянии покоя. Меняйте X/Y/Z прямо в Inspector.")]
    [SerializeField] private Vector3 restingRotation = new Vector3(-110f, 180f, 0f);
    [Tooltip("Цвет металлических частей ключа. Применяется к дочерним Renderer.")]
    [SerializeField] private Color metalColor = new Color(0.7f, 0.74f, 0.8f, 1f);
    [Tooltip("Цвет рукоятки ключа.")]
    [SerializeField] private Color handleColor = new Color(0.08f, 0.09f, 0.11f, 1f);
    [SerializeField] private bool applyColorsOnStart = true;

    private float sparkVisibleUntil;

    [Header("Scene Model")]
    [Tooltip("Перетащите сюда корневой объект модели гаечного ключа из сцены или префаба.")]
    [SerializeField] private Transform wrenchModel;

    private Transform wrench => wrenchModel;
    private float nextSwingTime;
    private float swingTime;
    private bool isSwinging;
    private bool hitRegistered;

    private void Awake()
    {
        if (applyColorsOnStart) ApplyWrenchMaterials();
        if (wrenchModel != null) wrenchModel.localRotation = Quaternion.Euler(restingRotation);
        if (wrenchModel == null)
        {
            Debug.LogWarning("WrenchTool: не назначена модель гаечного ключа в инспекторе.", this);
        }
    }

    private void OnGUI()
    {
        if (Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked) return;
        float x = Screen.width * 0.5f;
        float y = Screen.height * 0.5f;
        Color oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(x - 1f, y - 10f, 2f, 20f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 10f, y - 1f, 20f, 2f), Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

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
    }

    private void AnimateSwing()
    {
        swingTime += Time.deltaTime;
        float progress = Mathf.Clamp01(swingTime / swingDuration);
        float rotation = -110f + Mathf.Sin(progress * Mathf.PI) * 80f;
        if (wrench == null) return;
        wrench.localRotation = Quaternion.Euler(rotation, 180f, 0f);

        if (!hitRegistered && progress > 0.35f)
        {
            hitRegistered = true;
            TryHitTarget();
        }

        if (progress >= 1f)
        {
            isSwinging = false;
            if (wrench != null) wrench.localRotation = Quaternion.Euler(restingRotation);
        }
    }

    private void TryHitTarget()
    {
        if (aimCamera == null)
        {
            return;
        }

        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, range))
        {
            return;
        }

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target == null)
        {
            Collider[] nearby = Physics.OverlapSphere(hit.point, hitRadius, hitLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider nearbyCollider in nearby)
            {
                target = nearbyCollider.GetComponentInParent<IDamageable>();
                if (target != null) break;
            }
        }

        if (target == null) return;
        target.TakeDamage(damage);
        sparkVisibleUntil = Time.time + sparkDuration;
        CreateHitSparks(hit.point, hit.normal);
    }

    private void CreateHitSparks(Vector3 position, Vector3 normal)
    {
        GameObject sparks = new GameObject("SpiderHitSparks");
        sparks.transform.position = position;
        sparks.transform.rotation = Quaternion.LookRotation(normal);
        ParticleSystem particles = sparks.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.playOnAwake = false;
        main.duration = 0.12f;
        main.startLifetime = 0.12f;
        main.startSpeed = 3.5f;
        main.startSize = 0.045f;
        main.startColor = new Color(1f, 0.65f, 0.08f);
        main.maxParticles = 18;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.02f;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Standard"));
        particles.Play();
        Destroy(sparks, 0.4f);
    }

    private void ApplyWrenchMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material handleMaterial = new Material(shader) { color = handleColor };
        Material metalMaterial = new Material(shader) { color = metalColor };

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