using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public Transform target; // Drag the Kart here
    public float smoothSpeed = 0.125f;

    void LateUpdate() {
        if (target != null) { // Safety check
            Vector3 desiredPosition = new Vector3(target.position.x, target.position.y + 5, -5);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        }
    }
}