using UnityEngine;
using TMPro; 

public class PlayerHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI speedText;
    [SerializeField] TextMeshProUGUI stateText;

    [Header("Target Player")]
    [SerializeField] PlayerMovement player;

    [Header("Speed Colors")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color fastColor = Color.yellow;
    [SerializeField] Color blazingColor = Color.red;

    void Update()
    {
        if (player == null) return;

        // 1. Update Speed Text (Format to 1 decimal place, e.g., 15.2)
        float currentSpeed = player.GetCurrentSpeed();
        speedText.text = $"SPEED: {currentSpeed:F1} m/s";

        // Change color based on how fast we are going
        if (currentSpeed >= 20f) 
            speedText.color = blazingColor;
        else if (currentSpeed >= 14f) 
            speedText.color = fastColor;
        else 
            speedText.color = normalColor;

        // 2. Update Movement State Text
        stateText.text = player.GetMovementState();
    }
}