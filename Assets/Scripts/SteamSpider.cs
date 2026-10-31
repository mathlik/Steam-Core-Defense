using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Враг-паук. Ходит между точками, при тревоге направляется к активному вентилю,
/// получает три удара и взрывается. Если его не убить вовремя, ломает вентиль сам.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SteamSpider : MonoBehaviour, IDamageable
{
    private enum State { Roaming, GoingToValve, Dead }

    [Header("Movement")]
    [SerializeField, Min(0f)] private float speed = 3f;
    [SerializeField, Min(0f)] private float rotationSpeed = 12f;
    [SerializeField, Min(0.05f)] private float arrivalDistance = 1.2f;
    [SerializeField, Min(0.1f)] private float patrolRadius = 8f;
    [Tooltip("Сила небольшого бокового отклонения маршрута, чтобы паук не шёл идеально по прямой.")]
    [SerializeField, Min(0f)] private float pathWanderAmplitude = 0.65f;
    [SerializeField, Min(0.1f)] private float pathWanderFrequency = 1.2f;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [Tooltip("Если включено, паук использует NavMeshAgent и обходит стены вместо попыток идти сквозь них.")]
    [SerializeField] private bool useNavMeshPathfinding = true;
    [SerializeField, Min(0.05f)] private float obstacleRepathDelay = 0.35f;
    [SerializeField, Min(0f)] private float valveAttackDelay = 3f;
    [SerializeField, Min(0f)] private float patrolBeforeAttack = 0f;
    [SerializeField, Min(1)] private int valvesToBreakIfIgnored = 3;
    [SerializeField, Min(0.1f)] private float nextValveAttackInterval = 2f;
    [Tooltip("Минимальное расстояние до вентиля, с которого паук может его сломать.")]
    [SerializeField, Min(0.1f)] private float valveBreakDistance = 1.5f;

    [Header("Explosion Audio")]
    [Tooltip("Единственный звук, который проигрывается при взрыве паука. Оставьте пустым, чтобы звук не проигрывался.")]
    [SerializeField] private AudioClip explosionClip;
    [SerializeField, Range(0f, 1f)] private float explosionVolume = 1f;

    [Header("Hit Audio")]
    [Tooltip("Звук при получении урона гаечным ключом.")]
    [SerializeField] private AudioClip hitClip;
    [SerializeField, Range(0f, 1f)] private float hitVolume = 1f;

    [Header("Combat")]
    [SerializeField, Min(1)] private int hitsToKill = 3;
    [SerializeField, Min(0f)] private float explosionRadius = 3f;
    [SerializeField, Min(0f)] private float explosionDamage = 35f;
    [SerializeField] private LayerMask explosionLayers = ~0;
    [SerializeField, Min(0f)] private float destroyDelay = 2f;

    private State state;
    private int hitCount;
    private int patrolIndex;
    private float nextValveAttackTime;
    private int valvesBroken;
    private Vector3 patrolCenter;
    private CoreGameplay gameplay;
    private AudioSource audioSource;
    private NavMeshAgent navMeshAgent;
    private float nextRepathTime;
    private ParticleSystem steamBurst;
    private Transform[] patrolPoints;
    private Vector3 automaticPatrolTarget;
    private float attackStartTime;
    private float pathWanderSeed;
    private Transform currentValveTarget;

    private void Awake()
    {
        gameplay = FindFirstObjectByType<CoreGameplay>();
        pathWanderSeed = Random.Range(0f, 1000f);
        patrolBeforeAttack = Mathf.Max(0f, patrolBeforeAttack);
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 18f;
        audioSource.playOnAwake = false;
        audioSource.clip = null;

        navMeshAgent = GetComponent<NavMeshAgent>();
        if (useNavMeshPathfinding && navMeshAgent != null)
        {
            navMeshAgent.speed = speed;
            navMeshAgent.angularSpeed = rotationSpeed * 60f;
            navMeshAgent.stoppingDistance = Mathf.Max(0.05f, arrivalDistance);
            navMeshAgent.updatePosition = true;
            navMeshAgent.updateRotation = true;
        }

        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.45f, 0f);
            capsule.radius = 0.45f;
            capsule.height = 0.9f;
        }
        steamBurst = CreateSteamBurst();
        state = State.Roaming;
        patrolCenter = transform.position;
        automaticPatrolTarget = GetNewAutomaticPatrolTarget();
        attackStartTime = Time.time + patrolBeforeAttack;
        nextValveAttackTime = attackStartTime + valveAttackDelay;
    }

    private void Update()
    {
        if (state == State.Dead) return;

        if (gameplay != null && Time.time >= attackStartTime)
        {
            if (currentValveTarget == null || !gameplay.IsSpiderTargetAvailable(currentValveTarget))
                currentValveTarget = gameplay.GetSpiderTarget();

            if (currentValveTarget != null)
            {
                state = State.GoingToValve;
                MoveTowards(currentValveTarget.position);
                float distanceToValve = Vector3.Distance(transform.position, currentValveTarget.position);
                if (distanceToValve <= valveBreakDistance && valvesBroken < valvesToBreakIfIgnored && Time.time >= nextValveAttackTime)
                {
                    if (gameplay.BreakValveFromSpider(transform, currentValveTarget, valveBreakDistance))
                    {
                        valvesBroken++;
                        currentValveTarget = null;
                    }
                    nextValveAttackTime = Time.time + nextValveAttackInterval;
                }
                return;
            }
        }

        state = State.Roaming;
        MoveToPatrolPoint();
    }

    private void MoveToPatrolPoint()
    {
        Vector3 target;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Transform point = patrolPoints[patrolIndex];
            if (point == null)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                return;
            }
            target = point.position;
            if (Vector3.Distance(transform.position, target) <= arrivalDistance)
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }
        else
        {
            target = automaticPatrolTarget;
            if (Vector3.Distance(transform.position, target) <= arrivalDistance)
                automaticPatrolTarget = GetNewAutomaticPatrolTarget();
        }
        MoveTowards(target);
    }

    private Vector3 GetNewAutomaticPatrolTarget()
    {
        Vector2 offset = Random.insideUnitCircle * patrolRadius;
        Vector3 target = patrolCenter + new Vector3(offset.x, 0f, offset.y);
        if (Physics.Raycast(target + Vector3.up * 5f, Vector3.down, out RaycastHit ground, 10f, obstacleLayers, QueryTriggerInteraction.Ignore))
            target.y = ground.point.y;
        else
            target.y = transform.position.y;
        return target;
    }

    private void MoveTowards(Vector3 target)
    {
        if (useNavMeshPathfinding && navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            if (Time.time >= nextRepathTime)
            {
                navMeshAgent.SetDestination(target);
                nextRepathTime = Time.time + obstacleRepathDelay;
            }
            return;
        }

        Vector3 offset = target - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude < 0.001f) return;

        Vector3 forward = offset.normalized;
        Vector3 side = Vector3.Cross(Vector3.up, forward);
        float wander = (Mathf.PerlinNoise(pathWanderSeed, Time.time * pathWanderFrequency) - 0.5f) * 2f;
        Vector3 curvedOffset = forward * offset.magnitude + side * wander * pathWanderAmplitude;
        offset = curvedOffset;
        if (offset.sqrMagnitude < 0.001f) return;
        Quaternion desiredRotation = Quaternion.LookRotation(offset.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);

        Vector3 direction = offset.normalized;
        float step = speed * Time.deltaTime;
        Vector3 capsuleBottom = transform.position + Vector3.up * 0.2f;
        Vector3 capsuleTop = transform.position + Vector3.up * 0.8f;
        bool blocked = Physics.CapsuleCast(capsuleBottom, capsuleTop, 0.35f, direction, out RaycastHit obstacle, step + 0.05f, obstacleLayers, QueryTriggerInteraction.Ignore);
        bool hitSelf = blocked && obstacle.collider != null && obstacle.collider.transform.IsChildOf(transform);
        if (!blocked || hitSelf)
        {
            transform.position += direction * step;
        }
        else
        {
            Vector3 sideDirection = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 detour = transform.position + (sideDirection * 1.5f + direction * 1.5f);
            if (!Physics.CheckCapsule(detour + Vector3.up * 0.2f, detour + Vector3.up * 0.8f, 0.35f, obstacleLayers, QueryTriggerInteraction.Ignore))
                transform.position = Vector3.MoveTowards(transform.position, detour, step);
            else
                automaticPatrolTarget = GetNewAutomaticPatrolTarget();
        }
    }

    public void TakeDamage(float amount)
    {
        if (state == State.Dead || amount <= 0f) return;
        hitCount++;
        if (audioSource != null && hitClip != null)
            audioSource.PlayOneShot(hitClip, hitVolume);
        if (hitCount >= Mathf.Max(1, hitsToKill)) Die();
    }

    private void Die()
    {
        state = State.Dead;
        if (steamBurst != null) steamBurst.Play();
        if (audioSource != null && explosionClip != null)
            audioSource.PlayOneShot(explosionClip, explosionVolume);
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, explosionLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is SteamSpider)) damageable.TakeDamage(explosionDamage);
        }
        if (gameplay != null) gameplay.ScheduleSpiderSpawnAfterDeath();
        Destroy(gameObject, destroyDelay);
    }

    private ParticleSystem CreateSteamBurst()
    {
        GameObject effectObject = new GameObject("GeneratedSpiderSteamBurst");
        effectObject.transform.SetParent(transform, false);
        ParticleSystem effect = effectObject.AddComponent<ParticleSystem>();
        var main = effect.main;
        main.playOnAwake = false;
        main.duration = 0.7f;
        main.startLifetime = 0.7f;
        main.startSpeed = 2f;
        main.startSize = 0.25f;
        main.startColor = new Color(0.8f, 0.9f, 1f, 0.75f);
        main.maxParticles = 30;
        var emission = effect.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
        var shape = effect.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;
        effect.Stop();
        return effect;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
