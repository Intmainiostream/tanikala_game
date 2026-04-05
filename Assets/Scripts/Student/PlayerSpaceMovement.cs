using UnityEngine;

public class PlayerSpaceMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float sprintSpeed = 6f;
    public float doubleTapTime = 0.3f;

    [Header("Boundaries")]
    public float leftLimit = -8f;
    public float rightLimit = 8f;

    private Animator animator;
    private bool movingLeft = false;
    private bool movingRight = false;
    private bool isSprinting = false;
    private float lastLeftTapTime = -1f;
    private float lastRightTapTime = -1f;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (movingLeft)
        {
            float newX = transform.position.x - currentSpeed * Time.deltaTime;
            newX = Mathf.Max(newX, leftLimit);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        }
        else if (movingRight)
        {
            float newX = transform.position.x + currentSpeed * Time.deltaTime;
            newX = Mathf.Min(newX, rightLimit);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        }
    }

    public void OnLeftDown()
    {
        if (Time.time - lastLeftTapTime <= doubleTapTime)
            isSprinting = true;
        lastLeftTapTime = Time.time;
        movingLeft = true;
        animator.SetBool("isWalking", true);
        animator.SetBool("goingRight", false);
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
        animator.SetBool("isWalking", true);
        animator.SetBool("goingRight", true);
    }

    public void OnRightUp()
    {
        movingRight = false;
        isSprinting = false;
        animator.SetBool("isWalking", false);
    }
}
