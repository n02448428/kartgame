using UnityEngine;

public class KartController : MonoBehaviour {
    Rigidbody2D rb;
    public float speed = 5f, turnSpeed = 100f, jumpForce = 300f;
    bool isGrounded = true; // To prevent mid-air jumping

    void Start() {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update() {
        float move = Input.GetAxis("Vertical") * speed;
        float turn = Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime;
        rb.linearVelocity = transform.up * move;
        transform.Rotate(0, 0, -turn);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded) {
            rb.AddForce(Vector2.up * jumpForce);
            isGrounded = false;
            Debug.Log("Jump triggered!"); // For debugging
        }
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Ground")) {
            isGrounded = true; // Reset when landing
        }
    }
}