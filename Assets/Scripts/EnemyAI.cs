using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    // Vision
    [Header("Vision & Movement")]
    public float visionRange = 15f;
    public LayerMask obstacleMask;

    // Stun
    [Header("Stun Mechanics & Visibility")]
    public float stunDuration = 2f;

    [Tooltip("Ziehe hier das 3D-Modell (Mesh Renderer oder Skinned Mesh Renderer) des Gegners hinein")]
    public Renderer[] modelsToHide;

    // State
    private float stunTimer = 0f;
    private NavMeshAgent agent;
    private bool hasBeenSeen = false;

    // Unity
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        foreach (Renderer r in modelsToHide)
        {
            if (r != null) r.enabled = false;
        }
    }

    void Update()
    {
        // Light check
        bool isIlluminated = CheckIfIlluminated();

        // Reveal
        if (isIlluminated && !hasBeenSeen)
        {
            hasBeenSeen = true;
            foreach (Renderer r in modelsToHide)
            {
                if (r != null) r.enabled = true;
            }
        }

        // Stun
        if (isIlluminated)
        {
            Stun();
        }

        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (agent.isActiveAndEnabled) agent.isStopped = true;
            return;
        }

        if (agent.isActiveAndEnabled) agent.isStopped = false;

        // Chase
        if (stunTimer <= 0f)
        {
            ChasePlayerIfSeen();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayerController pc = collision.gameObject.GetComponent<PlayerController>();

        if (pc != null)
        {
            pc.hunger = Mathf.Max(0f, pc.hunger - 25f);
            pc.thirst = Mathf.Max(0f, pc.thirst - 25f);

            while (pc.Inventory.Count > 0)
            {
                pc.RemoveItem(pc.Inventory[0]);
            }

            Destroy(gameObject);
        }
    }

    // Flashlight
    private bool CheckIfIlluminated()
    {
        if (PlayerController.Instance == null || PlayerController.Instance.flashlight == null) return false;

        Light playerLight = PlayerController.Instance.flashlight;

        if (!playerLight.enabled) return false;

        Vector3 dirFromLight = (transform.position - playerLight.transform.position).normalized;
        float distanceToLight = Vector3.Distance(playerLight.transform.position, transform.position);

        if (distanceToLight <= playerLight.range)
        {
            float angle = Vector3.Angle(playerLight.transform.forward, dirFromLight);
            if (angle <= playerLight.spotAngle / 2f)
            {
                if (!Physics.Raycast(playerLight.transform.position, dirFromLight, distanceToLight, obstacleMask))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void Stun()
    {
        stunTimer = stunDuration;
        if (agent.isActiveAndEnabled) agent.isStopped = true;
    }

    // Chase
    private void ChasePlayerIfSeen()
    {
        if (PlayerController.Instance == null) return;

        Transform playerTransform = PlayerController.Instance.transform;
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= visionRange)
        {
            if (!Physics.Raycast(transform.position, dirToPlayer, distanceToPlayer, obstacleMask))
            {
                if (agent.isActiveAndEnabled)
                {
                    agent.SetDestination(playerTransform.position);
                }
            }
        }
    }
}
