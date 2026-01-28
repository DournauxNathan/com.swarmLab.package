using UnityEngine;

public class SimpleRunTrigger : MonoBehaviour
{
    [Header("Settings")]
    public float threshold = 0.1f;

    private Animator anim;
    private Vector3 lastPosition;

    void Start()
    {
        anim = GetComponent<Animator>();
        lastPosition = transform.position;
    }

    void Update()
    {
        // Calculate the distance moved since the last frame
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        
        // Convert distance to speed (optional, but makes threshold more intuitive)
        float currentSpeed = distanceMoved / Time.deltaTime;

        // Update the Animator boolean
        if (currentSpeed > threshold)
        {
            anim.SetBool("Run", true);
        }
        else
        {
            anim.SetBool("Run", false);
        }

        // Store current position for the next frame
        lastPosition = transform.position;
    }
}