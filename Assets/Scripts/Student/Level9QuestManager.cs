using UnityEngine;

public class Level9QuestManager : MonoBehaviour
{
    [Header("Water Bottle (hidden after interacted)")]
    public GameObject waterBottleObject;

    [Header("NPCs to give water to (order matches Level9WaterRecipient npcIndex)")]
    public InteractableObject[] npcInteractables;

    [Header("HUDs to hide on complete")]
    public GameObject[] huds;

    [Header("Level Complete")]
    public LevelCompleteManager levelCompleteManager;

    private bool waterBottlePickedUp = false;
    private bool[] npcGiven;

    void Start()
    {
        npcGiven = new bool[npcInteractables.Length];
        SetNPCsInteractable(false);
    }

    // Called by water bottle's InteractableObject (assign questManager9)
    public void StartQuest()
    {
        Debug.Log("[Level9] StartQuest called. waterBottlePickedUp=" + waterBottlePickedUp);
        if (waterBottlePickedUp) return;

        waterBottlePickedUp = true;
        if (waterBottleObject != null) waterBottleObject.SetActive(false);
        else Debug.LogWarning("[Level9] waterBottleObject is NULL!");
        SetNPCsInteractable(true);
        Debug.Log("[Level9] Water bottle picked up. NPCs unlocked.");
    }

    // Called by InteractableObject via Level9WaterRecipient
    public void OnNPCGiven(int index, InteractableObject npc)
    {
        Debug.Log($"[Level9] OnNPCGiven index={index}, waterBottlePickedUp={waterBottlePickedUp}, already given={npcGiven[index]}");
        if (!waterBottlePickedUp) return;
        if (index < 0 || index >= npcGiven.Length) return;
        if (npcGiven[index]) return;

        npcGiven[index] = true;

        npc.enabled = false;
        if (npc.questionMark != null) npc.questionMark.SetActive(false);
        Collider2D col = npc.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        int doneCount = 0;
        foreach (bool g in npcGiven) if (g) doneCount++;
        Debug.Log($"[Level9] NPC {index} given. {doneCount}/{npcGiven.Length} total.");

        CheckAllGiven();
    }

    void CheckAllGiven()
    {
        foreach (bool given in npcGiven)
            if (!given) return;

        Debug.Log("[Level9] All NPCs given water! Triggering complete.");
        TriggerComplete();
    }

    void TriggerComplete()
    {
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);
        if (levelCompleteManager != null) levelCompleteManager.OnLevelComplete();
        else Debug.LogWarning("[Level9] levelCompleteManager is NULL!");
    }

    void SetNPCsInteractable(bool state)
    {
        foreach (InteractableObject npc in npcInteractables)
        {
            if (npc == null) continue;
            npc.enabled = state;
            Collider2D col = npc.GetComponent<Collider2D>();
            if (col != null) col.enabled = state;
        }
    }
}
