using UnityEngine;
using TMPro;

public class GuardPatrol : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite leftIdleSprite;
    public Sprite rightIdleSprite;

    [Header("Look Durations")]
    public float lookLeftDuration = 3f;
    public float lookRightDuration = 4f;

    [Header("Detection")]
    public float detectionRadius = 3f;
    public float mobileDetectionRadius = 80f;
    public float detectionDelay = 1f;
    public bool requireFacingPlayer = true;

    [Header("Debug UI")]
    public GameObject SeenTxt;
    public GameObject HiddenTxt;
    public GameObject WarningTxt;

    public bool paused = false;

    private SpriteRenderer sr;
    private float timer = 0f;
    private float detectionTimer = 0f;
    private float startupTimer = 0f;
    private bool ready = false;
    public bool lookingLeft = true;
    private bool hasCaught = false;
    private Level3QuestManager questManager;
    public Transform player;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        questManager = FindObjectOfType<Level3QuestManager>();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        HidingSpot.Reset();
        InteractableObject.AnyInteractionActive = false;

        SetSprite();
    }

    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (paused)
        {
            HideAllUI();
            return;
        }

        if (!ready)
        {
            startupTimer += Time.deltaTime;
            if (startupTimer >= 2f) ready = true;
            return;
        }

        timer += Time.deltaTime;
        float currentDuration = lookingLeft ? lookLeftDuration : lookRightDuration;
        if (timer >= currentDuration)
        {
            timer = 0f;
            lookingLeft = !lookingLeft;
            SetSprite();
        }

        CheckDetection();
    }

    void SetSprite()
    {
        if (sr == null) return;
        sr.sprite = lookingLeft ? leftIdleSprite : rightIdleSprite;
    }

    void HideAllUI()
    {
        if (SeenTxt != null) SeenTxt.SetActive(false);
        if (HiddenTxt != null) HiddenTxt.SetActive(false);
        if (WarningTxt != null) WarningTxt.SetActive(false);
    }

    void CheckDetection()
    {
        if (hasCaught || player == null) return;

        if (InteractableObject.AnyInteractionActive)
        {
            HideAllUI();
            detectionTimer = 0f;
            return;
        }

        if (HidingSpot.IsPlayerHiding)
        {
            if (HiddenTxt != null) HiddenTxt.SetActive(true);
            if (SeenTxt != null) SeenTxt.SetActive(false);
            if (WarningTxt != null) WarningTxt.SetActive(false);
            detectionTimer = 0f;
            return;
        }

        float dx = player.position.x - transform.position.x;
        float dist = Vector2.Distance(transform.position, player.position);
#if UNITY_ANDROID || UNITY_IOS
        float radius = mobileDetectionRadius;
#else
        float radius = detectionRadius;
#endif
        bool inRange = dist <= radius;
        bool playerIsLeft = dx < 0f;
        bool facingPlayer = (lookingLeft && playerIsLeft) || (!lookingLeft && !playerIsLeft);

        bool canSee = inRange && (!requireFacingPlayer || facingPlayer);

        if (canSee)
        {
            if (SeenTxt != null) SeenTxt.SetActive(true);
            if (HiddenTxt != null) HiddenTxt.SetActive(false);
            if (WarningTxt != null) WarningTxt.SetActive(false);

            detectionTimer += Time.deltaTime;
            if (detectionTimer >= detectionDelay)
            {
                detectionTimer = 0f;
                hasCaught = true;
                if (SeenTxt != null) SeenTxt.SetActive(false);
                if (WarningTxt != null) WarningTxt.SetActive(true);
                questManager?.OnPlayerCaught();
            }
        }
        else
        {
            HideAllUI();
            detectionTimer = 0f;
        }
    }

    public void ResetCaught()
    {
        hasCaught = false;
        detectionTimer = 0f;
        HideAllUI();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
