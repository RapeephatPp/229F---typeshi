using UnityEngine;
using TMPro; 

public class PlayerHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI speedText;
    [SerializeField] TextMeshProUGUI stateText;
    [SerializeField] TextMeshProUGUI maxSpeedText; // New text element for Max Speed

    [Header("Target Player")]
    [SerializeField] PlayerMovement player;

    [Header("Speed Colors")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color fastColor = Color.yellow;
    [SerializeField] Color blazingColor = Color.red;

    // Internal variable to keep track of the highest speed reached
    private float maxSpeedRecord = 0f;

    void Update()
    {
        if (player == null) return;

        // 1. Get current speed from player
        float currentSpeed = player.GetCurrentSpeed();
        
        // 2. Update Max Speed Record if current speed is higher
        if (currentSpeed > maxSpeedRecord)
        {
            maxSpeedRecord = currentSpeed;
        }

        // 3. Update UI Texts
        speedText.text = $"SPEED: {currentSpeed:F1} m/s";
        
        if (maxSpeedText != null)
        {
            maxSpeedText.text = $"MAX SPEED: {maxSpeedRecord:F1} m/s";
        }

        // Change color based on how fast we are going
        if (currentSpeed >= 20f) 
            speedText.color = blazingColor;
        else if (currentSpeed >= 14f) 
            speedText.color = fastColor;
        else 
            speedText.color = normalColor;

        // 4. Update Movement State Text
        stateText.text = player.GetMovementState();
    }
}