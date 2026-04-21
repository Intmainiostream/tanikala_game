using UnityEngine;

// Attach this to each required interactable object in PreLevel10Scene.
// InteractableObject will notify PreLevel10Manager when this object is interacted with.
public class Level10Trackable : MonoBehaviour
{
    public PreLevel10Manager manager;
}
