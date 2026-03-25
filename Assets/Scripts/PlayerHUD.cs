using UnityEngine;
using TMPro; 

public class PlayerHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI speedText;
    [SerializeField] TextMeshProUGUI stateText;
    [SerializeField] TextMeshProUGUI maxSpeedText; 
    [SerializeField] TextMeshProUGUI ammoText; // เพิ่ม UI กระสุน

    [Header("Target References")]
    [SerializeField] PlayerMovement player;
    [SerializeField] Shotgun playerShotgun; // ลิงก์ไปยังปืน

    [Header("Speed Colors")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color fastColor = Color.yellow;
    [SerializeField] Color blazingColor = Color.red;

    private float maxSpeedRecord = 0f;

    void Update()
    {
        if (player == null) return;

        float currentSpeed = player.GetCurrentSpeed();
        
        if (currentSpeed > maxSpeedRecord)
        {
            maxSpeedRecord = currentSpeed;
        }

        // อัปเดต UI ความเร็ว
        if (speedText != null) speedText.text = $"SPEED: {currentSpeed:F1} m/s";
        if (maxSpeedText != null) maxSpeedText.text = $"MAX SPEED: {maxSpeedRecord:F1} m/s";

        if (currentSpeed >= 20f) speedText.color = blazingColor;
        else if (currentSpeed >= 14f) speedText.color = fastColor;
        else speedText.color = normalColor;

        if (stateText != null) stateText.text = player.GetMovementState();

        // อัปเดต UI กระสุน
        if (ammoText != null && playerShotgun != null)
        {
            ammoText.text = $"{playerShotgun.GetCurrentAmmo()} / {playerShotgun.GetMaxAmmo()}";
            
            // เปลี่ยนสีเป็นสีแดงถ้ากระสุนหมด
            if (playerShotgun.GetCurrentAmmo() == 0)
                ammoText.color = Color.red;
            else
                ammoText.color = Color.white;
        }
    }
}