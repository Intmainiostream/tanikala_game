using UnityEngine;

// Attach to any hiding spot object (post, bush, etc.)
// Set its Sprite Renderer Order in Layer higher than the player so player appears behind it.
public class HidingSpot : MonoBehaviour
{
    private static int hidingCount = 0;
    public static bool IsPlayerHiding => hidingCount > 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) hidingCount++;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) hidingCount = Mathf.Max(0, hidingCount - 1);
    }

    void OnDisable()
    {
        hidingCount = 0;
    }
}
