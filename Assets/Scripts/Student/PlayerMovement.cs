using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 300f;
    public float sprintSpeed = 600f;
    public float doubleTapTime = 0.3f;

    [Header("Boundaries")]
    public float leftLimit = -500f;
    public float rightLimit = 500f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private RectTransform rectTransform;
    private bool movingLeft = false;
    private bool movingRight = false;
    private bool facingRight = false;
    private bool isSprinting = false;

    private float lastLeftTapTime = -1f;
    private float lastRightTapTime = -1f;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (movingLeft)
        {
            float newX = rectTransform.anchoredPosition.x - currentSpeed * Time.deltaTime;
            newX = Mathf.Max(newX, leftLimit);
            rectTransform.anchoredPosition = new Vector2(newX, rectTransform.anchoredPosition.y);
        }
        else if (movingRight)
        {
            float newX = rectTransform.anchoredPosition.x + currentSpeed * Time.deltaTime;
            newX = Mathf.Min(newX, rightLimit);
            rectTransform.anchoredPosition = new Vector2(newX, rectTransform.anchoredPosition.y);
        }

        spriteRenderer.flipX = facingRight;
    }

    public void OnLeftDown()
    {
        if (Time.time - lastLeftTapTime <= doubleTapTime)
            isSprinting = true;

        lastLeftTapTime = Time.time;
        movingLeft = true;
        facingRight = false;
        animator.SetBool("isWalking", true);
    }

    public void OnLeftUp()
    {
        movingLeft = false;
        isSprinting = false;
        animator.SetBool("isWalking", false);
    }

    public void OnRightDown()
    {
        if (Time.time - lastRightTapTime <= doubleTapTime)
            isSprinting = true;

        lastRightTapTime = Time.time;
        movingRight = true;
        facingRight = true;
        animator.SetBool("isWalking", true);
    }

    public void OnRightUp()
    {
        movingRight = false;
        isSprinting = false;
        animator.SetBool("isWalking", false);
    }
}
