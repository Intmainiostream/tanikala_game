using UnityEngine;

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
    public float detectionDelay = 1f;

    public bool paused = false;

    private SpriteRenderer sr;
    private float timer = 0f;
    private float detectionTimer = 0f;
    private bool lookingLeft = true;
    private bool hasCaught = false;
    private Level3QuestManager questManager;
    private Transform player;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        questManager = FindObjectOfType<Level3QuestManager>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        SetSprite();
    }

    void Update()
    {
        if (paused) return;

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

    void CheckDetection()
    {
        if (hasCaught || player == null) return;
        if (InteractableObject.AnyInteractionActive) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool inRange = dist <= detectionRadius && !HidingSpot.IsPlayerHiding;

        if (inRange)
        {
            detectionTimer += Time.deltaTime;
            if (detectionTimer >= detectionDelay)
            {
                hasCaught = true;
                detectionTimer = 0f;
                questManager?.OnPlayerCaught();
            }
        }
        else
        {
            detectionTimer = 0f;
        }
    }

    public void ResetCaught()
    {
        hasCaught = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
