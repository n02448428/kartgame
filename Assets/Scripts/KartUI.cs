using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class KartUI : MonoBehaviour
{
    public KartController kart;  // Reference to the KartController
    public Text speedText;
    public Text controlsText;
    public Text creditsText;

    void Start()
    {
        if (kart == null)
        {
            kart = FindObjectOfType<KartController>(); // Auto-find if not set
        }

        // Set up UI text
        if (controlsText != null)
        {
            controlsText.text = "CONTROLS:\n" +
                                "Arrow Keys - Turn\n" +
                                "W - Jump/Drift\n" +
                                "D - Accelerate\n" +
                                "S - Reverse\n" +
                                "R - Reset Kart";
        }

        if (creditsText != null)
        {
            creditsText.text = "Made by @dmitrymakelove\nFollow on Twitter!";
        }
    }

    void Update()
    {
        if (kart != null && speedText != null)
        {
            // Convert speed from Unity units/sec to km/h
            float speedKmh = kart.GetComponent<Rigidbody>().velocity.magnitude * 3.6f;
            speedText.text = $"Speed: {Mathf.Round(speedKmh)} km/h";
        }

        // Reset scene if "R" is pressed
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
