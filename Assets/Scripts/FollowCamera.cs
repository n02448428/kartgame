using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 6.0f;
    public float height = 2.0f;
    public float smoothSpeed = 10.0f;
    
    // Added variables for smoother camera motion
    public float lookAheadFactor = 0.2f;     // How much to look ahead based on velocity
    public float maxLookAhead = 3.0f;        // Maximum look-ahead distance
    public bool useFixedUpdate = true;       // Use FixedUpdate for smoother physics-based following
    
    private Vector3 offset;
    private Vector3 desiredPosition;
    private Vector3 velocity = Vector3.zero;  // For SmoothDamp
    
    void Start()
    {
        offset = new Vector3(0, height, -distance);
        
        // Initialize desired position
        if (target != null)
        {
            desiredPosition = target.position + target.TransformDirection(offset);
            transform.position = desiredPosition;
        }
    }
    
    void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            UpdateCameraPosition();
        }
    }
    
    void LateUpdate()
    {
        if (!useFixedUpdate)
        {
            UpdateCameraPosition();
        }
    }
    
    void UpdateCameraPosition()
    {
        if (target == null)
            return;
            
        // Get target's rigidbody for velocity-based look-ahead
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        Vector3 lookAheadPos = Vector3.zero;
        
        // Add velocity-based look-ahead if rigidbody exists
        if (targetRb != null)
        {
            // Calculate normalized velocity direction in local space
            Vector3 localVelocity = target.InverseTransformDirection(targetRb.linearVelocity);
            
            // Only use forward velocity for look-ahead
            lookAheadPos = target.TransformDirection(new Vector3(0, 0, localVelocity.z * lookAheadFactor));
            
            // Clamp look-ahead to maximum value
            lookAheadPos = Vector3.ClampMagnitude(lookAheadPos, maxLookAhead);
        }
        
        // Calculate desired position with look-ahead
        desiredPosition = target.position + lookAheadPos + target.TransformDirection(offset);
        
        // Use SmoothDamp instead of Lerp for smoother motion without lag
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, Time.deltaTime * (1f/smoothSpeed));
        
        // Look at target (slightly ahead of the actual target position)
        transform.LookAt(target.position + Vector3.up * 0.5f + lookAheadPos * 0.5f);
    }
}