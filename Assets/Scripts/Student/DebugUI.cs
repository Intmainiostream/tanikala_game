using UnityEngine;
using TMPro;

public class DebugUI : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    public TextMeshProUGUI debugMobileText;
    public GuardPatrol guard1;
    public GuardPatrol guard2;
    public Transform playerTransform;

    void Update()
    {
        Transform player = playerTransform;
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        string platform = Application.isMobilePlatform ? "MOBILE" : "EDITOR";
        string playerPos = player != null ? $"({player.position.x:F1},{player.position.y:F1})" : "NULL";
        string playerName = player != null ? player.gameObject.name : "NULL";
        string info = $"[{platform}] player:{playerName} {playerPos}\n";

        if (guard1 != null && player != null)
        {
            float dist = Vector2.Distance(guard1.transform.position, player.position);
            float dx = player.position.x - guard1.transform.position.x;
            bool playerIsLeft = dx < 0f;
            bool facing = (guard1.lookingLeft && playerIsLeft) || (!guard1.lookingLeft && !playerIsLeft);
#if UNITY_ANDROID || UNITY_IOS
            float r = guard1.mobileDetectionRadius;
#else
            float r = guard1.detectionRadius;
#endif
            info += $"G1 dist:{dist:F1} r:{r} inRange:{dist <= r} facing:{facing} lookL:{guard1.lookingLeft}\n";
        }

        if (guard2 != null && player != null)
        {
            float dist = Vector2.Distance(guard2.transform.position, player.position);
            float dx = player.position.x - guard2.transform.position.x;
            bool playerIsLeft = dx < 0f;
            bool facing = (guard2.lookingLeft && playerIsLeft) || (!guard2.lookingLeft && !playerIsLeft);
#if UNITY_ANDROID || UNITY_IOS
            float r = guard2.mobileDetectionRadius;
#else
            float r = guard2.detectionRadius;
#endif
            info += $"G2 dist:{dist:F1} r:{r} inRange:{dist <= r} facing:{facing} lookL:{guard2.lookingLeft}\n";
        }

        info += $"hiding:{HidingSpot.IsPlayerHiding}";

        if (debugText != null) debugText.text = info;
        if (debugMobileText != null) debugMobileText.text = info;
    }
}
