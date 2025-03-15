using UnityEngine;
using System.Collections;

public class KartController : MonoBehaviour
{
    [Header("Kart Settings")]
    public float maxSpeed = 20f;
    public float acceleration = 30f;
    public float turnSpeed = 100f;
    public float gravity = 20f;
    public float groundRayDistance = 1.2f;     // Increased to detect ground from higher up
    public float targetHoverHeight = 0.4f;     // Target height to maintain above ground
    public float hoverForce = 300f;            // Force to maintain hover height
    public float hoverDamping = 0.5f;          // Damping to prevent bouncing
    public float dragForce = 0.98f;
    
    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float jumpCooldown = 0.5f;
    
    [Header("Drift Settings")]
    public float driftTurnMultiplier = 1.5f;     // Increases turn rate while drifting
    public float driftVelocityMultiplier = 0.9f; // Reduces forward velocity while initiating drift
    public float minSpeedToDrift = 8f;           // Minimum speed required to drift
    public float driftHopForce = 5f;             // Force for the small hop when starting to drift (Mario Kart style)
    public Color[] boostColors = new Color[] { 
        Color.yellow, 
        new Color(1.0f, 0.5f, 0.0f), // Orange (RGB: 255, 127, 0)
        Color.red 
    }; // Colors for mini-turbo levels
    public float[] boostThresholds = new float[] { 1f, 2f, 3f }; // Time thresholds for mini-turbo levels
    public float[] boostPowers = new float[] { 5f, 10f, 15f };   // Speed boost for each mini-turbo level
    
    [Header("References")]
    public Transform kartModel;
    public LayerMask groundLayer;
    public ParticleSystem leftDriftPS;
    public ParticleSystem rightDriftPS;
    
    [Header("Corner Raycast Settings")]
    public Vector2 kartDimensions = new Vector2(1.0f, 2.0f); // Width and Length of the kart
    public float cornerRaycastHeight = 0.5f;  // Increased height for corner raycasts
    public float unstuckForce = 5f;           // Force applied to unstuck the kart when needed
    
    // Private variables
    private Rigidbody rb;
    private float currentSpeed = 0f;
    private bool isGrounded = true;
    private float rotationAmount = 0f;
    private float lastJumpTime = -10f;
    private Vector3 moveDirection;
    
    // Corner raycast points
    private Vector3[] cornerPoints = new Vector3[4];
    private bool[] cornerGrounded = new bool[4];
    private float[] cornerDistances = new float[4];
    
    // Drift variables
    private bool isDrifting = false;
    private float driftDirection = 0f;
    private float driftTime = 0f;
    private int currentBoostLevel = -1;
    private float driftBoostReserve = 0f;
    
    // Mario Kart 64 style drift variables
    private bool driftButtonWasHeldInAir = false;
    private bool hasDriftJumped = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Configure rigidbody properly
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        if (kartModel == null)
        {
            kartModel = transform.GetChild(0);
        }
        
        // Set up particle systems if they don't exist
        if (leftDriftPS == null || rightDriftPS == null)
        {
            Debug.LogWarning("Drift particle systems not assigned. Drift visual effects won't be shown.");
        }
        else
        {
            // Ensure particles start stopped
            leftDriftPS.Stop();
            rightDriftPS.Stop();
        }
    }
    
    void Update()
    {
        // Update corner points and check if grounded
        UpdateCornerRaycasts();
        
        // Get player input - MODIFIED CONTROLS
        float accelerationInput = 0f;
        if (Input.GetKey(KeyCode.D)) accelerationInput += 1f;
        if (Input.GetKey(KeyCode.S)) accelerationInput -= 1f;
        
        float steeringInput = 0f;
        if (Input.GetKey(KeyCode.RightArrow)) steeringInput += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) steeringInput -= 1f;
        
        // Handle jump input - only allow if not in drift mode
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && !isDrifting && Time.time > lastJumpTime + jumpCooldown)
        {
            Jump();
        }
        
        // Handle drift input - modified to use W key instead of Space
        HandleDrift(steeringInput);
        
        // Calculate rotation amount
        float effectiveSteeringInput = isDrifting ? driftDirection * driftTurnMultiplier : steeringInput;
        float targetRotationAmount = effectiveSteeringInput * turnSpeed;
        rotationAmount = Mathf.Lerp(rotationAmount, targetRotationAmount, Time.deltaTime * 5f);
        
        // Only allow turning when moving
        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            // Apply rotation
            transform.Rotate(0, rotationAmount * Time.deltaTime * Mathf.Sign(currentSpeed), 0);
        }
        
        // Apply tilting effect to kart model
        if (kartModel != null)
        {
            Vector3 newRotation = kartModel.localEulerAngles;
            newRotation.z = -effectiveSteeringInput * 15f; // Tilt when turning
            
            // Add additional tilt when drifting
            if (isDrifting)
            {
                newRotation.z -= driftDirection * 10f;
            }
            
            kartModel.localEulerAngles = newRotation;
        }
        
        // Apply boost if available
        if (driftBoostReserve > 0)
        {
            currentSpeed += driftBoostReserve * Time.deltaTime;
            driftBoostReserve = Mathf.Max(0, driftBoostReserve - (Time.deltaTime * 10f));
        }
        
        // Debug logging for drift conditions
        if (Input.GetKey(KeyCode.W) && Mathf.Abs(steeringInput) > 0.5f)
        {
            Debug.Log($"Drift conditions: Button=true, Grounded={isGrounded}, Steering={steeringInput}, Speed={currentSpeed}, MinSpeed={minSpeedToDrift}, InAir={driftButtonWasHeldInAir}, HasHopped={hasDriftJumped}");
        }
    }
    
    void UpdateCornerRaycasts()
    {
        float halfWidth = kartDimensions.x / 2;
        float halfLength = kartDimensions.y / 2;
        
        // Calculate the four corner points in local space
        cornerPoints[0] = new Vector3(-halfWidth, cornerRaycastHeight, halfLength);  // Front Left
        cornerPoints[1] = new Vector3(halfWidth, cornerRaycastHeight, halfLength);   // Front Right
        cornerPoints[2] = new Vector3(-halfWidth, cornerRaycastHeight, -halfLength); // Rear Left
        cornerPoints[3] = new Vector3(halfWidth, cornerRaycastHeight, -halfLength);  // Rear Right
        
        // Check if each corner is grounded
        int groundedCorners = 0;
        
        for (int i = 0; i < 4; i++)
        {
            // Convert corner point to world space
            Vector3 worldCorner = transform.TransformPoint(cornerPoints[i]);
            
            // Cast ray downward from each corner
            RaycastHit hit;
            if (Physics.Raycast(worldCorner, -transform.up, out hit, groundRayDistance, groundLayer))
            {
                cornerGrounded[i] = true;
                cornerDistances[i] = hit.distance;
                groundedCorners++;
            }
            else
            {
                cornerGrounded[i] = false;
                cornerDistances[i] = groundRayDistance; // Use max distance if no hit
            }
        }
        
        // Kart is considered grounded if at least one corner is detecting ground within range
        isGrounded = groundedCorners > 0;
        
        // If the kart is stuck (at a strange angle with only one corner grounded), apply force to unstuck it
        if (groundedCorners == 1 && !isDrifting && rb.linearVelocity.magnitude < 1f)
        {
            // Apply an upward force to unstuck the kart
            rb.AddForce(transform.up * unstuckForce, ForceMode.Impulse);
        }
    }
    
    void HandleDrift(float steeringInput)
    {
        bool driftButtonHeld = Input.GetKey(KeyCode.W); // Changed from Space to W
        
        // Track if drift button is held while in air (needed for Mario Kart 64 style drifting)
        if (!isGrounded && driftButtonHeld)
        {
            driftButtonWasHeldInAir = true;
        }
        
        // If we just landed and drift button was held in air and we're steering (Mario Kart 64 style)
        if (isGrounded && !hasDriftJumped && driftButtonWasHeldInAir && 
            Mathf.Abs(steeringInput) > 0.5f && currentSpeed > minSpeedToDrift)
        {
            // Perform a small hop (like Mario Kart 64)
            rb.AddForce(Vector3.up * driftHopForce, ForceMode.Impulse);
            hasDriftJumped = true;
            
            // Start tracking drift once we land again
            StartCoroutine(BeginDriftAfterHop(steeringInput));
        }
        
        // Handle active drifting
        if (isDrifting)
        {
            // Make sure drift button is still held
            if (!driftButtonHeld)
            {
                EndDrift();
            }
            else
            {
                // Accumulate drift time for mini-turbo
                driftTime += Time.deltaTime;
                
                // Check boost levels
                UpdateBoostLevel();
            }
        }
        
        // Reset when drift button is released
        if (!driftButtonHeld)
        {
            driftButtonWasHeldInAir = false;
            if (!isDrifting)
            {
                hasDriftJumped = false;
            }
        }
    }
    
    IEnumerator BeginDriftAfterHop(float initialSteeringInput)
    {
        // Wait for a small time to simulate the hop
        yield return new WaitForSeconds(0.1f);
        
        // Wait until we land again
        yield return new WaitUntil(() => isGrounded);
        
        // Begin drifting
        isDrifting = true;
        driftDirection = Mathf.Sign(initialSteeringInput);
        currentSpeed *= driftVelocityMultiplier;
        
        // Start drift particles
        if (driftDirection < 0 && leftDriftPS != null)
        {
            leftDriftPS.Play();
            if (rightDriftPS != null) rightDriftPS.Stop();
            Debug.Log("Left Drift Particles Activated");
        }
        else if (driftDirection > 0 && rightDriftPS != null)
        {
            rightDriftPS.Play();
            if (leftDriftPS != null) leftDriftPS.Stop();
            Debug.Log("Right Drift Particles Activated");
        }
    }
    
    void EndDrift()
    {
        // End drift and apply boost if earned
        isDrifting = false;
        hasDriftJumped = false;
        
        // Stop drift particles
        if (leftDriftPS != null) leftDriftPS.Stop();
        if (rightDriftPS != null) rightDriftPS.Stop();
        
        // Apply boost based on drift time
        if (currentBoostLevel >= 0)
        {
            driftBoostReserve = boostPowers[currentBoostLevel];
            Debug.Log("Drift Boost Applied: Level " + (currentBoostLevel + 1));
        }
        
        // Reset drift variables
        driftTime = 0f;
        currentBoostLevel = -1;
    }
    
    void UpdateBoostLevel()
    {
        // Determine boost level based on drift time
        int newBoostLevel = -1;
        for (int i = boostThresholds.Length - 1; i >= 0; i--)
        {
            if (driftTime >= boostThresholds[i])
            {
                newBoostLevel = i;
                break;
            }
        }
        
        // Update particle color based on boost level
        if (newBoostLevel != currentBoostLevel)
        {
            currentBoostLevel = newBoostLevel;
            
            if (currentBoostLevel >= 0 && currentBoostLevel < boostColors.Length)
            {
                Color boostColor = boostColors[currentBoostLevel];
                
                if (leftDriftPS != null)
                {
                    var mainModule = leftDriftPS.main;
                    mainModule.startColor = boostColor;
                }
                
                if (rightDriftPS != null)
                {
                    var mainModule = rightDriftPS.main;
                    mainModule.startColor = boostColor;
                }
                
                Debug.Log("Boost Level Changed: " + (currentBoostLevel + 1));
            }
        }
    }
    
    void FixedUpdate()
    {
        // Get player input - MODIFIED CONTROLS
        float accelerationInput = 0f;
        if (Input.GetKey(KeyCode.D)) accelerationInput += 1f;
        if (Input.GetKey(KeyCode.S)) accelerationInput -= 1f;
        
        // Apply hover forces to maintain height above ground
        if (isGrounded)
        {
            ApplyHoverForces();
            AlignToGround();  // Align to ground slopes
            
            // Accelerate or brake
            if (accelerationInput != 0)
            {
                currentSpeed += accelerationInput * acceleration * Time.fixedDeltaTime;
            }
            else
            {
                // Apply friction when no input - smoother approach
                currentSpeed = Mathf.Lerp(currentSpeed, 0, (1 - dragForce) * Time.fixedDeltaTime * 10f);
            }
            
            // Clamp speed
            currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed/2, maxSpeed);
        }
        else
        {
            // Apply less drag in air
            currentSpeed *= 0.99f;
            
            // Apply manual gravity when not grounded
            rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        }
        
        // Calculate move direction
        moveDirection = transform.forward * currentSpeed;
        
        // Move kart with smoother acceleration to reduce stuttering
        Vector3 targetVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);
    }
    
    void AlignToGround()
    {
        if (!isGrounded) return;
        
        // Calculate ground normal by averaging the normals at each grounded corner
        Vector3 groundNormal = Vector3.up;
        int groundedCornerCount = 0;
        
        // Calculate up to 4 hit normals from grounded corners
        for (int i = 0; i < 4; i++)
        {
            if (cornerGrounded[i])
            {
                // Get the hit normal for this corner
                Vector3 worldCorner = transform.TransformPoint(cornerPoints[i]);
                RaycastHit hit;
                if (Physics.Raycast(worldCorner, -transform.up, out hit, groundRayDistance, groundLayer))
                {
                    groundNormal += hit.normal;
                    groundedCornerCount++;
                }
            }
        }
        
        if (groundedCornerCount > 0)
        {
            // Average the normals
            groundNormal /= groundedCornerCount;
            
            // Calculate rotation to align with ground
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
            
            // Smoothly rotate to match ground
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }
    }
    
    void ApplyHoverForces()
    {
        for (int i = 0; i < 4; i++)
        {
            if (cornerGrounded[i])
            {
                // Calculate the force needed to maintain hover height
                float currentHeight = cornerDistances[i];
                float heightError = targetHoverHeight - currentHeight;
                
                // Convert corner point to world space
                Vector3 worldCorner = transform.TransformPoint(cornerPoints[i]);
                
                // Calculate the hover force - higher force when too close to ground
                float forceAmount = hoverForce * heightError;
                
                // Add damping to prevent bouncing
                forceAmount -= rb.linearVelocity.y * hoverDamping;
                
                // Apply the hover force at this corner
                rb.AddForceAtPosition(transform.up * forceAmount, worldCorner, ForceMode.Force);
            }
        }
    }
    
    void Jump()
    {
        // Apply upward force
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        lastJumpTime = Time.time;
        isGrounded = false;
        
        // Reset drift parameters on manual jump
        hasDriftJumped = false;
        driftButtonWasHeldInAir = false;
    }
    
    void OnDrawGizmos()
    {
        // Draw corner raycasts in the editor for debugging
        float halfWidth = kartDimensions.x / 2;
        float halfLength = kartDimensions.y / 2;
        
        // Calculate the four corner points
        Vector3 frontLeft = transform.TransformPoint(new Vector3(-halfWidth, cornerRaycastHeight, halfLength));
        Vector3 frontRight = transform.TransformPoint(new Vector3(halfWidth, cornerRaycastHeight, halfLength));
        Vector3 rearLeft = transform.TransformPoint(new Vector3(-halfWidth, cornerRaycastHeight, -halfLength));
        Vector3 rearRight = transform.TransformPoint(new Vector3(halfWidth, cornerRaycastHeight, -halfLength));
        
        // Draw rays at each corner
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(frontLeft, frontLeft + (-transform.up * groundRayDistance));
        Gizmos.DrawLine(frontRight, frontRight + (-transform.up * groundRayDistance));
        Gizmos.DrawLine(rearLeft, rearLeft + (-transform.up * groundRayDistance));
        Gizmos.DrawLine(rearRight, rearRight + (-transform.up * groundRayDistance));
        
        // Draw hover height markers
        Gizmos.color = Color.green;
        float hoverY = cornerRaycastHeight - targetHoverHeight;
        Vector3 flHover = transform.TransformPoint(new Vector3(-halfWidth, hoverY, halfLength));
        Vector3 frHover = transform.TransformPoint(new Vector3(halfWidth, hoverY, halfLength));
        Vector3 rlHover = transform.TransformPoint(new Vector3(-halfWidth, hoverY, -halfLength));
        Vector3 rrHover = transform.TransformPoint(new Vector3(halfWidth, hoverY, -halfLength));
        
        // Draw small spheres at hover height
        Gizmos.DrawSphere(flHover, 0.05f);
        Gizmos.DrawSphere(frHover, 0.05f);
        Gizmos.DrawSphere(rlHover, 0.05f);
        Gizmos.DrawSphere(rrHover, 0.05f);
    }
}