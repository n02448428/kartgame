using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class KartUI : MonoBehaviour
{
    // Drag your GameUI GameObject (with the UIDocument component) into this slot in the Inspector.
    public UIDocument uiDocument;

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
        // Find your KartController in the scene.
        kart = FindObjectOfType<KartController>();
        if (kart == null)
        {
            Debug.LogError("No KartController found in the scene!");
        }

        if (uiDocument == null)
        {
            Debug.LogError("uiDocument is not assigned in KartUI!");
            return;
        }

        // Get the root visual element from the UIDocument.
        var root = uiDocument.rootVisualElement;

        // Fetch the Labels by their names – these names must exactly match those in GameUI.uxml.
        speedText = root.Q<Label>("SpeedText");
        controlsText = root.Q<Label>("ControlsText");
        creditsText = root.Q<Label>("CreditsText");
        debugText = root.Q<Label>("DebugText");

        // Log errors if any label is missing.
        if (speedText == null) Debug.LogError("SpeedText label not found!");
        if (controlsText == null) Debug.LogError("ControlsText label not found!");
        if (creditsText == null) Debug.LogError("CreditsText label not found!");
        if (debugText == null) Debug.LogError("DebugText label not found!");

        // Set initial static texts.
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
        // FPS Calculation.
        frameCount++;
        deltaTime += Time.unscaledDeltaTime;
        if (deltaTime >= 1.0f)
        {
            fps = frameCount / deltaTime;
            frameCount = 0;
            deltaTime = 0.0f;
        }

        // Update Speed Display using Rigidbody.velocity (not linearVelocity).
        if (kart != null && speedText != null)
        {
            float speedKmh = kart.GetComponent<Rigidbody>().velocity.magnitude * 3.6f;
            speedText.text = $"Speed: {Mathf.Round(speedKmh)} km/h";
        }

        // Update Debug Info (FPS and frame time).
        if (debugText != null)
        {
            float frameTime = (1.0f / Mathf.Max(fps, 0.0001f)) * 1000; // in ms
            debugText.text = $"FPS: {Mathf.Round(fps)}\nFrame Time: {frameTime:F2} ms";
        }

        // Check for the "R" key to reset the scene.
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R Key Pressed! Resetting Scene...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
