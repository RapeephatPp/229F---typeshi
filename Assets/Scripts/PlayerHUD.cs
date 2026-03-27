using UnityEngine;
using TMPro; 

public class PlayerHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI speedText;
    [SerializeField] TextMeshProUGUI stateText;
    [SerializeField] TextMeshProUGUI maxSpeedText; 
    [SerializeField] TextMeshProUGUI ammoText; 
    
    [Header("Level Info UI")]
    [SerializeField] TextMeshProUGUI levelText;    // ชื่อด่าน
    [SerializeField] TextMeshProUGUI timerText;    // เวลาปัจจุบัน
    [SerializeField] TextMeshProUGUI bestTimeText; // เวลาที่ดีที่สุด
    
    [Header("Target References")]
    [SerializeField] PlayerMovement player;
    [SerializeField] Shotgun playerShotgun; 

    [Header("Speed Colors")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color fastColor = Color.yellow;
    [SerializeField] Color blazingColor = Color.red;

    private float maxSpeedRecord = 0f;

    void Update()
    {
        if (player == null) return;

        float currentSpeed = player.GetCurrentSpeed();
        if (currentSpeed > maxSpeedRecord) maxSpeedRecord = currentSpeed;

        if (speedText != null) speedText.text = $"SPEED: {currentSpeed:F1} m/s";
        if (maxSpeedText != null) maxSpeedText.text = $"MAX SPEED: {maxSpeedRecord:F1} m/s";

        if (currentSpeed >= 20f) speedText.color = blazingColor;
        else if (currentSpeed >= 14f) speedText.color = fastColor;
        else speedText.color = normalColor;

        if (stateText != null) stateText.text = player.GetMovementState();

        // อัปเดต UI กระสุนแบบใหม่ (กระสุนในปืน / กระสุนสำรอง)
        if (ammoText != null && playerShotgun != null)
        {
            ammoText.text = $"{playerShotgun.GetCurrentAmmo()} / {playerShotgun.GetTotalAmmo()}";
            
            // เปลี่ยนสีแจ้งเตือน
            if (playerShotgun.GetCurrentAmmo() == 0 && playerShotgun.GetTotalAmmo() == 0)
                ammoText.color = Color.red; // สีแดง = กระสุนหมดเกลี้ยง ไม่มีให้รีโหลดแล้ว!
            else if (playerShotgun.GetCurrentAmmo() == 0)
                ammoText.color = Color.yellow; // สีเหลือง = ปืนว่าเปล่า แต่ยังกด R เพื่อรีโหลดได้
            else
                ammoText.color = Color.white; // สีขาว = ปกติ
        }
        
        if (GameManager.Instance != null)
        {
            // แสดงชื่อด่าน (ดึงมาจากชื่อ Scene)
            if (levelText != null) 
                levelText.text = "LEVEL: " + GameManager.Instance.currentLevelName;
            
            // แสดงเวลาปัจจุบัน
            if (timerText != null) 
                timerText.text = "TIME: " + FormatTime(GameManager.Instance.currentTime);
            
            // แสดง Best Time
            if (bestTimeText != null) 
            {
                if (GameManager.Instance.bestTime > 0f)
                    bestTimeText.text = "BEST: " + FormatTime(GameManager.Instance.bestTime);
                else
                    bestTimeText.text = "BEST: --:--.--"; // ถ้ายังไม่มีสถิติให้โชว์ขีด
            }
        }
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }
}