using UnityEngine;

// Attach this alongside InteractableObject on Bench1 and Bench2 only
public class Level4FlyerInteractable : MonoBehaviour
{
    public Level4QuestManager questManager;
    public int flyerIndex; // 0 = Bench1, 1 = Bench2
}
