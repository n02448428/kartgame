using UnityEngine;
using System.Collections;

public class KartController : MonoBehaviour
{
    [Header("Kart Settings")]
    public float maxSpeed = 20f;
    public float acceleration = 30f;
    public float turnSpeed = 100f;
    public float gravity = 20f;
    public float groundRayDistance = 1.2f;
    public float targetHoverHeight = 0.4f;
    public float hoverForce = 300f;
    public float hoverDamping = 0.5f;
    public float dragForce = 0.98f;
    
    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float jumpCooldown = 0.5f;
    
    [Header("Drift Settings")]
    public float driftTurnMultiplier = 1.5f;
    public float driftVelocityMultiplier = 0.9f;
    public float minSpeedToDrift = 8f;
    public float driftHopForce = 5f;
    public Color[] boostColors = new Color[] { 
        Color.yellow, 
        new Color(1.0f, 0.5f, 0.0f),
        Color.red 
    };
    public float[] boostThresholds = new float[] { 1f, 2f, 3f };
    // Updated boost powers (in m/s) for drift levels:
    // Level 1: maxSpeed + 2.2 m/s ≈ 80 km/h,
    // Level 2: maxSpeed + 7.8 m/s ≈ 100 km/h,
    // Level 3: maxSpeed + 16.1 m/s ≈ 130 km/h.
    public float[] boostPowers = new float[] { 2.2f, 7.8f, 16.1f };   
    
    [Header("References")]
    public Transform kartModel;
    public LayerMask groundLayer;
    public ParticleSystem leftDriftPS;
    public ParticleSystem rightDriftPS;
    
    [Header("Corner Raycast Settings")]
    public Vector2 kartDimensions = new Vector2(1.0f, 2.0f);
    public float cornerRaycastHeight = 0.5f;
    public float unstuckForce = 5f;
    
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
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        if (kartModel == null)
        {
            kartModel = transform.GetChild(0);
        }
        
        if (leftDriftPS == null || rightDriftPS == null)
        {
            Debug.LogWarning("Drift particle systems not assigned. Drift visual effects won't be shown.");
        }
        else
        {
            leftDriftPS.Stop();
            rightDriftPS.Stop();
        }
    }
    
    void Update()
    {
        UpdateCornerRaycasts();
        
        float accelerationInput = 0f;
        if (Input.GetKey(KeyCode.D)) accelerationInput += 1f;
        if (Input.GetKey(KeyCode.S)) accelerationInput -= 1f;
        
        float steeringInput = 0f;
        if (Input.GetKey(KeyCode.RightArrow)) steeringInput += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) steeringInput -= 1f;
        
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && !isDrifting && Time.time > lastJumpTime + jumpCooldown)
        {
            Jump();
        }
        
        HandleDrift(steeringInput);
        
        float effectiveSteeringInput = isDrifting ? driftDirection * driftTurnMultiplier : steeringInput;
        float targetRotationAmount = effectiveSteeringInput * turnSpeed;
        rotationAmount = Mathf.Lerp(rotationAmount, targetRotationAmount, Time.deltaTime * 5f);
        
        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            transform.Rotate(0, rotationAmount * Time.deltaTime * Mathf.Sign(currentSpeed), 0);
        }
        
        if (kartModel != null)
        {
            Vector3 newRotation = kartModel.localEulerAngles;
            newRotation.z = -effectiveSteeringInput * 15f;
            if (isDrifting)
            {
                newRotation.z -= driftDirection * 10f;
            }
            kartModel.localEulerAngles = newRotation;
        }
        
        // Apply drift boost gradually if available.
        if (driftBoostReserve > 0)
        {
            currentSpeed += driftBoostReserve * Time.deltaTime;
            driftBoostReserve = Mathf.Max(0, driftBoostReserve - (Time.deltaTime * 10f));
        }
        
        if (Input.GetKey(KeyCode.W) && Mathf.Abs(steeringInput) > 0.5f)
        {
            Debug.Log($"Drift conditions: Button=true, Grounded={isGrounded}, Steering={steeringInput}, Speed={currentSpeed}, MinSpeed={minSpeedToDrift}, InAir={driftButtonWasHeldInAir}, HasHopped={hasDriftJumped}");
        }
    }
    
    void UpdateCornerRaycasts()
    {
        float halfWidth = kartDimensions.x / 2;
        float halfLength = kartDimensions.y / 2;
        cornerPoints[0] = new Vector3(-halfWidth, cornerRaycastHeight, halfLength);
        cornerPoints[1] = new Vector3(halfWidth, cornerRaycastHeight, halfLength);
        cornerPoints[2] = new Vector3(-halfWidth, cornerRaycastHeight, -halfLength);
        cornerPoints[3] = new Vector3(halfWidth, cornerRaycastHeight, -halfLength);
        
        int groundedCorners = 0;
        for (int i = 0; i < 4; i++)
        {
            Vector3 worldCorner = transform.TransformPoint(cornerPoints[i]);
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
                cornerDistances[i] = groundRayDistance;
            }
        }
        isGrounded = groundedCorners > 0;
        if (groundedCorners == 1 && !isDrifting && rb.velocity.magnitude < 1f)
        {
            rb.AddForce(transform.up * unstuckForce, ForceMode.Impulse);
        }
    }
    
    void HandleDrift(float steeringInput)
    {
        bool driftButtonHeld = Input.GetKey(KeyCode.W);
        if (!isGrounded && driftButtonHeld)
        {
            driftButtonWasHeldInAir = true;
        }
        
        if (isGrounded && !hasDriftJumped && driftButtonWasHeldInAir && 
            Mathf.Abs(steeringInput) > 0.5f && currentSpeed > minSpeedToDrift)
        {
            rb.AddForce(Vector3.up * driftHopForce, ForceMode.Impulse);
            hasDriftJumped = true;
            StartCoroutine(BeginDriftAfterHop(steeringInput));
        }
        
        if (isDrifting)
        {
            if (!driftButtonHeld)
            {
                EndDrift();
            }
            else
            {
                driftTime += Time.deltaTime;
                UpdateBoostLevel();
            }
        }
        
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
        yield return new WaitForSeconds(0.1f);
        yield return new WaitUntil(() => isGrounded);
        isDrifting = true;
        driftDirection = Mathf.Sign(initialSteeringInput);
        currentSpeed *= driftVelocityMultiplier;
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
        isDrifting = false;
        hasDriftJumped = false;
        if (leftDriftPS != null) leftDriftPS.Stop();
        if (rightDriftPS != null) rightDriftPS.Stop();
        if (currentBoostLevel >= 0)
        {
            driftBoostReserve = boostPowers[currentBoostLevel];
            Debug.Log("Drift Boost Applied: Level " + (currentBoostLevel + 1));
        }
        driftTime = 0f;
        currentBoostLevel = -1;
    }
    
    void UpdateBoostLevel()
    {
        int newBoostLevel = -1;
        for (int i = boostThresholds.Length - 1; i >= 0; i--)
        {
            if (driftTime >= boostThresholds[i])
            {
                newBoostLevel = i;
                break;
            }
        }
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
        float accelerationInput = 0f;
        if (Input.GetKey(KeyCode.D)) accelerationInput += 1f;
        if (Input.GetKey(KeyCode.S)) accelerationInput -= 1f;
        
        if (isGrounded)
        {
            ApplyHoverForces();
            AlignToGround();
            if (accelerationInput != 0)
            {
                currentSpeed += accelerationInput * acceleration * Time.fixedDeltaTime;
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0, (1 - dragForce) * Time.fixedDeltaTime * 10f);
            }
            // Allow currentSpeed to exceed maxSpeed by drift boost
            float effectiveMaxSpeed = maxSpeed + driftBoostReserve;
            currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed / 2, effectiveMaxSpeed);
        }
        else
        {
            currentSpeed *= 0.99f;
            rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        }
        
        moveDirection = transform.forward * currentSpeed;
        Vector3 targetVelocity = new Vector3(moveDirection.x, rb.velocity.y, moveDirection.z);
        rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * 10f);
    }
    
    void AlignToGround()
    {
        if (!isGrounded) return;
        Vector3 groundNormal = Vector3.up;
        int groundedCornerCount = 0;
        for (int i = 0; i < 4; i++)
        {
            if (cornerGrounded[i])
            {
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
            groundNormal /= groundedCornerCount;
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }
    }
    
    void ApplyHoverForces()
    {
        for (int i = 0; i < 4; i++)
        {
            if (cornerGrounded[i])
            {
                float currentHeight = cornerDistances[i];
               
