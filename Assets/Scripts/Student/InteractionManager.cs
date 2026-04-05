using UnityEngine;
using UnityEngine.UI;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    [Header("Interact Button")]
    public Button interactBtn;

    private InteractableObject currentInteractable;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (interactBtn != null)
            interactBtn.onClick.AddListener(OnInteractPressed);

        SetInteractButtonActive(false);
    }

    public void SetNearbyInteractable(InteractableObject interactable)
    {
        currentInteractable = interactable;
        SetInteractButtonActive(true);
    }

    public void ClearNearbyInteractable(InteractableObject interactable)
    {
        if (currentInteractable == interactable)
        {
            currentInteractable = null;
            SetInteractButtonActive(false);
        }
    }

    void OnInteractPressed()
    {
        // If a sequence panel is open, advance it instead
        PanelSequence activeSequence = FindActiveSequence();
        if (activeSequence != null)
        {
            activeSequence.OnInteractPressed();
            return;
        }

        if (currentInteractable != null)
            currentInteractable.Interact();
    }

    PanelSequence FindActiveSequence()
    {
        foreach (PanelSequence seq in FindObjectsOfType<PanelSequence>())
        {
            if (seq.gameObject.activeSelf)
                return seq;
        }
        return null;
    }

    void SetInteractButtonActive(bool active)
    {
        if (interactBtn != null)
            interactBtn.gameObject.SetActive(active);
    }
}
