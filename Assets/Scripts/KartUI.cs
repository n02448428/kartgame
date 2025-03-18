using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class KartUI : MonoBehaviour
{
    public UIDocument uiDocument; // UI Document reference
    private Label speedText;
    private Label controlsText;
    private Label creditsText;
    private Label debugText;
    private KartController kart;

    private int frameCount = 0;
    private float deltaTime = 0.0f;
    private float fps = 0.0f;

    void Start()
    {
        kart = FindObjectOfType<KartController>();

        // Get UI root
        var root = uiDocument.rootVisualElement;

        // Assign UI elements
        speedText = root.Q<Label>("SpeedText");
        controlsText = root.Q<Label>("ControlsText");
        creditsText = root.Q<Label>("CreditsText");
        debugText = root.Q<Label>("DebugText");

        // Set initial text
        if (controlsText != null)
        {
            controlsText.text = "CONTROLS:\nArrow Keys - Turn\nW - Jump/Drift\nD - Accelerate\nS - Reverse\nR - Reset Kart";
        }

        if (creditsText != null)
        {
            creditsText.text = "Made by @dmitrymakelove\nFollow on Twitter!";
        }
    }

    void Update()
    {
        // FPS Calculation
        frameCount++;
        deltaTime += Time.unscaledDeltaTime;
        if (deltaTime >= 1.0f)
        {
            fps = frameCount / deltaTime;
            frameCount = 0;
            deltaTime = 0.0f;
        }

        // Update Speed Display
        if (kart != null && speedText != null)
        {
            float speedKmh = kart.GetComponent<Rigidbody>().velocity.magnitude * 3.6f;
            speedText.text = $"Speed: {Mathf.Round(speedKmh)} km/h";
        }

        // Update Debug Info (FPS, Frame Time)
        if (debugText != null)
        {
            float frameTime = (1.0f / Mathf.Max(fps, 0.0001f)) * 1000;
            debugText.text = $"FPS: {Mathf.Round(fps)}\nFrame Time: {frameTime:F2} ms";
        }

        // Reset Scene on "R" Key Press
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
