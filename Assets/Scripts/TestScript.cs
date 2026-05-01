using UnityEngine;

// I-attach sa kahit anong GameObject sa scene
// Tapos i-drag ang iyong Box at Canvas sa Inspector
public class TestScript : MonoBehaviour
{
    [Header("I-drag dito sa Inspector")]
    public RectTransform box;    // I-drag ang iyong UI box dito
    public RectTransform canvas; // I-drag ang iyong Canvas dito

    [Header("Settings")]
    public float speed = 300f;   // Bilis ng galaw

    // Direksyon ng galaw
    private Vector2 direction;

    void Start()
    {
        // Random na direksyon sa simula
        float angle = Random.Range(0f, 360f);
        direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
    }

    void Update()
    {
        // Huwag ituloy kung walang naka-assign
        if (box == null || canvas == null) return;

        // Ilipat ang box
        box.anchoredPosition += direction * speed * Time.deltaTime;

        // Kalahati ng sukat ng canvas at box para sa bouncing
        Vector2 canvasHalf = canvas.rect.size * 0.5f;
        Vector2 boxHalf = box.rect.size * 0.5f;
        Vector2 pos = box.anchoredPosition;

        // Bounce kaliwa/kanan
        if (pos.x - boxHalf.x < -canvasHalf.x) { pos.x = -canvasHalf.x + boxHalf.x; direction.x = Mathf.Abs(direction.x); }
        else if (pos.x + boxHalf.x > canvasHalf.x) { pos.x = canvasHalf.x - boxHalf.x; direction.x = -Mathf.Abs(direction.x); }

        // Bounce taas/baba
        if (pos.y - boxHalf.y < -canvasHalf.y) { pos.y = -canvasHalf.y + boxHalf.y; direction.y = Mathf.Abs(direction.y); }
        else if (pos.y + boxHalf.y > canvasHalf.y) { pos.y = canvasHalf.y - boxHalf.y; direction.y = -Mathf.Abs(direction.y); }

        box.anchoredPosition = pos;
    }
}