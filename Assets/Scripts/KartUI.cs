using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Diagnostics;

public class KartUI : MonoBehaviour
{
    public UIDocument uiDocument; // UI Document reference
    private Label speedText;
    private Label controlsText;
    private Label creditsText;
    private Label debugText; // Debug Info (FPS, frame time, etc.)
    private KartController kart;

    private int frameCount = 0;
    private float deltaTime = 0.0f;
    private float fps = 0.0f;
    private Stopwatch stopwatch = new Stopwatch();

    void Start()
    {
        // Auto-find the KartController
        kart = FindObjectOfType<KartController>();

        // Get the root of the UI document
        var root = uiDocument.rootVisualElement;

        // Assign UI elements by their names
        speedText = root.Q<Label>("SpeedText");
        controlsText = root.Q<Label>("ControlsText");
        creditsText = root.Q<Label>("CreditsText");
        debugText = root.Q<Label>("DebugText"); // New Debug Label

        // Set initial UI text
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

        // Start FPS timer
        stopwatch.Start();
    }

    void Update()
    {
        // Update FPS counter
        frameCount++;
        deltaTime += Time.unscaled
