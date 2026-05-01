using UnityEngine;
using UnityEngine.SceneManagement;

public class HidingSpot : MonoBehaviour
{
    public static bool IsPlayerHiding { get; private set; }

    private static int instanceCount = 0;

    // ── Scene reset ──────────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        IsPlayerHiding = false;
        instanceCount  = 0;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsPlayerHiding = false;
        instanceCount = 0;
    }

    public static void Reset()
    {
        IsPlayerHiding = false;
        instanceCount = 0;
    }

    // ────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            Debug.LogError($"[HidingSpot] {gameObject.name} has no BoxCollider2D.");
            enabled = false;
            return;
        }
        col.isTrigger = true;
    }

    void OnDisable()
    {
        // If disabled while player is inside, clean up this instance's contribution
        if (playerInsideMe)
        {
            instanceCount = Mathf.Max(0, instanceCount - 1);
            playerInsideMe = false;
            IsPlayerHiding = instanceCount > 0;
        }
    }

    // ── Trigger callbacks ────────────────────────────────────────────────────
    private bool playerInsideMe = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || playerInsideMe) return;
        playerInsideMe = true;
        instanceCount++;
        IsPlayerHiding = instanceCount > 0;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || !playerInsideMe) return;
        playerInsideMe = false;
        instanceCount = Mathf.Max(0, instanceCount - 1);
        IsPlayerHiding = instanceCount > 0;
    }

    // ── Editor visualisation (unchanged) ─────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = IsPlayerHiding ? Color.green : Color.cyan;
        Gizmos.DrawWireCube(
            transform.position + (Vector3)col.offset,
            col.size * transform.lossyScale
        );
    }
}