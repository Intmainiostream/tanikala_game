using UnityEngine;

// Attach this to any World Space canvas panel that should stay fixed on screen.
public class FollowCamera : MonoBehaviour
{
    public Vector3 offset = Vector3.zero;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        transform.position = new Vector3(
            cam.transform.position.x + offset.x,
            cam.transform.position.y + offset.y,
            transform.position.z // keep original Z
        );
    }
}
