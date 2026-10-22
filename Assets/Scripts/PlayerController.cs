using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    // Movement
    [Header("Bewegung")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float turnSpeed = 12f;
    [Range(0.1f, 1f)] public float darkSpeedMultiplier = 0.6f;
    private float currentSpeed;

    // Stumble
    [Header("Folgen im Dunkeln")]
    public float stumbleChancePerSecond = 0.5f;
    public float stumbleStunSeconds = 1.2f;

    // Camera
    [Header("Kamera (fest von oben)")]
    public Transform playerCamera;
    public bool cameraFollowsPlayer = true;
    public Vector3 cameraOffset = new Vector3(0f, 12f, -4.4f);
    public Vector3 cameraAngles = new Vector3(70f, 0f, 0f);

    // Stamina
    [Header("Ausdauer (Stamina)")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 25f;
    public float staminaRegenRate = 15f;

    // Survival
    [Header("Überleben (Durst & Hunger) - bei 0 stirbt man")]
    public float maxThirst = 100f;
    public float thirst;
    public float thirstDrainPerSecond = 0.35f;
    public float maxHunger = 100f;
    public float hunger;
    public float hungerDrainPerSecond = 0.3f;
    public float lowSupplyThreshold = 20f;

    // Death texts
    [Header("Todesursachen & Tipps (Englisch, Todesbildschirm)")]
    [TextArea] public string thirstDeathReasonText = "You died of thirst. Your water supply wasn't enough.";
    [TextArea] public string thirstDeathTipText = "The game runs fast, but in a longer power outage water is the most important thing. Plan on about 2 liters of drinks per person per day and keep a supply for several days (Germany's BBK recommends about 10 days). During a longer outage, water pressure can also fail in taller buildings.";
    [TextArea] public string hungerDeathReasonText = "You starved. You had nothing to eat that keeps without electricity.";
    [TextArea] public string hungerDeathTipText = "Time is heavily compressed in the game, but the advice holds for real outages: build a stock of food that lasts without refrigeration or cooking (e.g. canned food, crispbread, nuts). Eat perishable food from the fridge first, because it is no longer safe after about 4 hours. The BBK recommends a supply for about 10 days.";

    // Flashlight
    [Header("Taschenlampe & Batterie")]
    public Light flashlight;
    public float maxBattery = 100f;
    public float currentBattery;
    public float batteryDrainPerSecond = 1f;

    // Inventory
    [Header("Inventar")]
    public int maxSlots = 4;
    [Tooltip("Frisches Essen im Inventar (ohne Kühlung) verdirbt nach so vielen SPIELSTUNDEN.")]
    public float foodSpoilHours = 4f;

    // Interaction
    [Header("Interaktion")]
    public float interactRadius = 1.8f;

    // Inventory state
    private readonly List<ItemType> inventory = new List<ItemType>();
    private readonly List<float> itemAgeHours = new List<float>();
    public IReadOnlyList<ItemType> Inventory { get { return inventory; } }

    // Keys
    private readonly HashSet<string> keys = new HashSet<string>();

    // Internal
    private Rigidbody rb;
    private NavMeshAgent agent;
    private bool isClickMoving;
    private Vector3 moveInput;
    private float baseFlashIntensity = 1f;
    private float stunTimer;
    private bool thirstWarned;
    private bool hungerWarned;
    private Interactable currentTarget;

    // State
    public string CurrentPrompt
    {
        get { return currentTarget != null ? currentTarget.GetPrompt() : ""; }
    }

    bool LightOn
    {
        get { return flashlight != null && flashlight.enabled; }
    }

    bool IsDark
    {
        get
        {
            BlackoutManager bm = BlackoutManager.Instance;
            bool powered = bm != null && bm.PowerOn;
            return !LightOn && !powered;
        }
    }

    // Unity
    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;

        currentStamina = maxStamina;
        thirst = maxThirst;
        hunger = maxHunger;
        currentBattery = maxBattery;
        currentSpeed = walkSpeed;

        if (flashlight != null)
        {
            baseFlashIntensity = flashlight.intensity;
            flashlight.enabled = true;
        }

        if (playerCamera != null)
        {
            playerCamera.rotation = Quaternion.Euler(cameraAngles);
        }

        if (BlackoutManager.Instance == null)
        {
            Debug.LogError("Kein BlackoutManager in der Szene! Erstelle ein leeres GameObject und ziehe das Skript BlackoutManager darauf.");
        }
    }

    void Update()
    {
        BlackoutManager bm = BlackoutManager.Instance;
        if (bm == null) return;

        // Game over
        if (bm.IsFinished)
        {
            moveInput = Vector3.zero;
            currentTarget = null;
            if (!bm.IsDead && flashlight != null) flashlight.enabled = false;
            if (agent.isActiveAndEnabled && agent.hasPath) agent.ResetPath();
            return;
        }

        // Day screen
        if (bm.IsPaused)
        {
            moveInput = Vector3.zero;
            currentTarget = null;
            return;
        }

        // Minigame
        bool busy = bm.AnyMinigameOpen;
        if (busy)
        {
            moveInput = Vector3.zero;
            currentTarget = null;
            if (agent.isActiveAndEnabled && agent.hasPath) agent.ResetPath();
        }
        else
        {
            UpdateMovementAndStamina(bm);
        }

        // Upkeep
        UpdateThirst(bm);
        UpdateHunger(bm);
        if (bm.IsFinished) return;
        UpdateInventoryAging(bm);
        UpdateFlashlight();
        if (busy) return;

        // Input
        UpdateTarget();
        HandleInventoryKeys(bm);

        if (Input.GetKeyDown(KeyCode.F) && flashlight != null)
        {
            if (currentBattery <= 0f)
                bm.ShowMessage("The battery is dead! Use a new battery from your inventory.");
            else
                flashlight.enabled = !flashlight.enabled;
        }

        if (Input.GetKeyDown(KeyCode.E) && currentTarget != null)
        {
            currentTarget.Interact(this);
        }
    }

    void FixedUpdate()
    {
        Vector3 targetVelocity = moveInput * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;
        rb.angularVelocity = Vector3.zero;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(moveInput, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, look, turnSpeed * Time.fixedDeltaTime));
        }
    }

    void LateUpdate()
    {
        if (playerCamera == null) return;
        playerCamera.rotation = Quaternion.Euler(cameraAngles);
        if (cameraFollowsPlayer)
        {
            playerCamera.position = transform.position + cameraOffset;
        }
    }

    // Movement
    void UpdateMovementAndStamina(BlackoutManager bm)
    {
        // Keyboard
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        Vector3 keyboardInput = new Vector3(moveX, 0f, moveZ).normalized;

        if (keyboardInput.sqrMagnitude > 0.01f)
        {
            isClickMoving = false;
            if (agent.hasPath) agent.ResetPath();
            moveInput = keyboardInput;
        }
        else
        {
            // Click move
            if (Input.GetMouseButtonDown(1))
            {
                Camera cam = playerCamera != null ? playerCamera.GetComponent<Camera>() : Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        agent.SetDestination(hit.point);
                        isClickMoving = true;
                    }
                }
            }

            if (isClickMoving)
            {
                agent.nextPosition = transform.position;

                if (agent.pathPending || agent.remainingDistance > 0.2f)
                {
                    moveInput = agent.desiredVelocity.normalized;
                }
                else
                {
                    isClickMoving = false;
                    moveInput = Vector3.zero;
                }
            }
            else
            {
                moveInput = Vector3.zero;
            }
        }

        // Stun
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            moveInput = Vector3.zero;
            if (agent.hasPath) agent.ResetPath();
            isClickMoving = false;
        }

        // Sprint
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        bool isHoldingShift = Input.GetKey(KeyCode.LeftShift);
        bool sprinting = isHoldingShift && isMoving && currentStamina > 0f
                         && thirst > lowSupplyThreshold && hunger > lowSupplyThreshold;

        if (sprinting)
        {
            currentSpeed = sprintSpeed;
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina < 0f) currentStamina = 0f;

            if (IsDark && Random.value < stumbleChancePerSecond * Time.deltaTime)
            {
                Stumble(bm);
            }
        }
        else
        {
            currentSpeed = walkSpeed;
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                if (currentStamina > maxStamina) currentStamina = maxStamina;
            }
        }

        if (IsDark) currentSpeed *= darkSpeedMultiplier;
    }

    void Stumble(BlackoutManager bm)
    {
        bm.stumbles++;
        currentStamina = 0f;
        stunTimer = stumbleStunSeconds;
        bm.ShowMessage("You stumbled in the dark! Walk slowly when you have no light.");
    }

    // Survival
    void UpdateThirst(BlackoutManager bm)
    {
        thirst -= thirstDrainPerSecond * Time.deltaTime;

        if (thirst <= lowSupplyThreshold)
        {
            bm.wasLowOnSupplies = true;
            if (!thirstWarned)
            {
                thirstWarned = true;
                bm.ShowMessage("You are very thirsty and can't sprint anymore. Drink something soon or you will die!", 6f);
            }
        }
        else
        {
            thirstWarned = false;
        }

        if (thirst <= 0f)
        {
            thirst = 0f;
            bm.PlayerDied(thirstDeathReasonText, thirstDeathTipText);
        }
    }

    void UpdateHunger(BlackoutManager bm)
    {
        if (bm.IsFinished) return;

        hunger -= hungerDrainPerSecond * Time.deltaTime;

        if (hunger <= lowSupplyThreshold)
        {
            bm.wasLowOnSupplies = true;
            if (!hungerWarned)
            {
                hungerWarned = true;
                bm.ShowMessage("You are very hungry and too weak to sprint. Eat something soon or you will die!", 6f);
            }
        }
        else
        {
            hungerWarned = false;
        }

        if (hunger <= 0f)
        {
            hunger = 0f;
            bm.PlayerDied(hungerDeathReasonText, hungerDeathTipText);
        }
    }

    // Flashlight
    void UpdateFlashlight()
    {
        if (flashlight == null || !flashlight.enabled) return;

        currentBattery -= batteryDrainPerSecond * Time.deltaTime;
        if (currentBattery <= 0f)
        {
            currentBattery = 0f;
            flashlight.enabled = false;
            BlackoutManager.Say("The battery is dead! Use a new battery (number key).");
            return;
        }

        float dim = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(currentBattery / 20f));
        flashlight.intensity = baseFlashIntensity * dim;
    }

    // Inventory
    public bool AddItem(ItemType item)
    {
        if (inventory.Count >= maxSlots) return false;
        inventory.Add(item);
        itemAgeHours.Add(0f);
        return true;
    }

    public bool HasItem(ItemType item)
    {
        return inventory.Contains(item);
    }

    public bool RemoveItem(ItemType item)
    {
        int index = inventory.IndexOf(item);
        if (index < 0) return false;
        RemoveAtIndex(index);
        return true;
    }

    void RemoveAtIndex(int index)
    {
        inventory.RemoveAt(index);
        itemAgeHours.RemoveAt(index);
    }

    void UpdateInventoryAging(BlackoutManager bm)
    {
        float hours = bm.GameHours(Time.deltaTime);
        if (hours <= 0f) return;

        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i] != ItemType.Food) continue;

            itemAgeHours[i] += hours;
            if (itemAgeHours[i] >= foodSpoilHours)
            {
                inventory[i] = ItemType.Spoiled;
                itemAgeHours[i] = 0f;
                bm.inventoryFoodSpoiled++;
                bm.ShowMessage("Some of your fresh food spoiled in your bag. Throw it away (X + number key), don't eat it!", 7f);
            }
        }
    }

    // Item keys
    void HandleInventoryKeys(BlackoutManager bm)
    {
        for (int i = 0; i < maxSlots && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (Input.GetKey(KeyCode.X)) DropItem(i, bm);
                else UseItem(i, bm);
            }
        }
    }

    void UseItem(int index, BlackoutManager bm)
    {
        if (index >= inventory.Count) return;
        ItemType item = inventory[index];

        switch (item)
        {
            case ItemType.Battery:
                if (currentBattery > 80f)
                {
                    bm.ShowMessage("The flashlight battery is still almost full. Save the new one for later.");
                    return;
                }
                currentBattery = maxBattery;
                RemoveAtIndex(index);
                if (flashlight != null) flashlight.enabled = true;
                bm.ShowMessage("New battery inserted.");
                break;

            case ItemType.Water:
                thirst = Mathf.Min(maxThirst, thirst + 40f);
                RemoveAtIndex(index);
                bm.ShowMessage("You drink some water.");
                break;

            case ItemType.Food:
                hunger = Mathf.Min(maxHunger, hunger + 35f);
                RemoveAtIndex(index);
                bm.ShowMessage("You eat the fresh food. Good thing you ate it before it spoiled.");
                break;

            case ItemType.Canned:
                hunger = Mathf.Min(maxHunger, hunger + 40f);
                RemoveAtIndex(index);
                bm.ShowMessage("You eat the canned food. Long-life supplies work even without power.");
                break;

            case ItemType.Spoiled:
                bm.ateSpoiled = true;
                hunger = Mathf.Min(maxHunger, hunger + 10f);
                thirst = Mathf.Max(0f, thirst - 25f);
                RemoveAtIndex(index);
                bm.ShowMessage("That was spoiled! You feel sick and lose fluids.", 6f);
                break;

            case ItemType.Radio:
                bm.ShowMessage("The radio is not portable. Walk up to it and press E.");
                break;

            case ItemType.Generator:
                bm.ShowMessage("Connect the generator at the fuse box: walk up to it and press E.");
                break;
        }
    }

    void DropItem(int index, BlackoutManager bm)
    {
        if (index >= inventory.Count) return;
        ItemType item = inventory[index];

        if (item == ItemType.Generator)
        {
            bm.ShowMessage("You need the generator. Don't throw it away!");
            return;
        }

        RemoveAtIndex(index);

        if (item == ItemType.Spoiled)
            bm.ShowMessage("You throw away the spoiled food. Good call!");
        else
            bm.ShowMessage(ItemNames.Full(item) + " thrown away.");
    }

    // Keys
    public void AddKey(string keyId) { keys.Add(keyId); }
    public bool HasKey(string keyId) { return keys.Contains(keyId); }

    // Interaction
    void UpdateTarget()
    {
        currentTarget = null;

        Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius, ~0, QueryTriggerInteraction.Collide);
        float best = float.MaxValue;

        foreach (Collider h in hits)
        {
            if (h.transform.IsChildOf(transform)) continue;

            Interactable it = h.GetComponentInParent<Interactable>();
            if (it == null || !it.enabled || !it.CanInteract(this)) continue;

            float d = (it.transform.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                currentTarget = it;
            }
        }
    }
}
