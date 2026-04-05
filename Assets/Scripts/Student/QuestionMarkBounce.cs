using UnityEngine;

public class QuestionMarkBounce : MonoBehaviour
{
    public float bounceHeight = 5f;
    public float bounceSpeed = 3f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}
