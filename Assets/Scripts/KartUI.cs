using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class KartUI : MonoBehaviour
{
    // Make sure to assign the UIDocument component in the Inspector!
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
        // Find the KartController in the scene
        kart = FindObjectOfType<KartController>();
        if (kart == null)
        {
            Debug.LogError("No KartController found in the scene!");
        }
        
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument is not assigned in KartUI!");
            return;
        }

        // Get the root visual element from the UIDocument
        var root = uiDocument.rootVisualElement;

        // Query UI elements by their names (make sure these match exactly with your GameUI.uxml)
        speedText = root.Q<Label>("SpeedText");
        controlsText = root.Q<Label>("ControlsText");
        creditsText = root.Q<Label>("CreditsText");
        debugText = root.Q<Label>("DebugText");

        // Log errors if any element wasn't found
        if (speedText == null) Debug.LogError("SpeedText label not found in UI Document!");
        if (controlsText == null) Debug.LogError("ControlsText label not found in UI Document!");
        if (creditsText == null) Debug.LogError("CreditsText label not found in UI Document!");
        if (debugText == null) Debug.LogError("DebugText label not found in UI Document!");

        // Set initial text for controls and credits
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
        // Calculate FPS (frames per second)
        frameCount++;
        deltaTime += Time.unscaledDeltaTime;
        if (deltaTime >= 1.0f)
        {
            fps = frameCount / deltaTime;
            frameCount = 0;
            deltaTime = 0.0f;
        }

        // Update the speed display (using Rigidbody.velocity, not linearVelocity)
        if (kart != null && speedText != null)
        {
            float speedKmh = kart.GetComponent<Rigidbody>().velocity.magnitude * 3.6f;
            speedText.text = $"Speed: {Mathf.Round(speedKmh)} km/h";
        }

        // Update debug info (FPS and frame time)
        if (debugText != null)
        {
            float frameTime = (1.0f / Mathf.Max(fps, 0.0001f)) * 1000; // in milliseconds
            debugText.text = $"FPS: {Mathf.Round(fps)}\nFrame Time: {frameTime:F2} ms";
        }

        // Check for the "R" key to reset the scene
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R Key Pressed! Resetting Scene...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
