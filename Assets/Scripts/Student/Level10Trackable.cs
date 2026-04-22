using UnityEngine;

// Attach this to each required interactable object in PreLevel10Scene.
public class Level10Trackable : MonoBehaviour
{
    public PreLevel10Manager manager;
    public int objectIndex; // 0, 1, 2... unique per object
}
