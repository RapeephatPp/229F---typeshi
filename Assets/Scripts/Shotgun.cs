using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Shotgun : MonoBehaviour
{
    [Header("Gun Stats")]
    [SerializeField] private int pelletCount = 12; 
    [SerializeField] private float damagePerPellet = 10f; 
    [SerializeField] private float spreadAngle = 7f; 
    [SerializeField] private float range = 30f; 

    [Header("Mobility & External Recoil")]
    [SerializeField] private float playerRecoilForce = 15f; 

    [Header("2D Animation (Sprite Swap)")]
    [SerializeField] private RectTransform gunRectTransform; 
    [SerializeField] private Image gunImage; 
    [SerializeField] private Sprite idleSprite; 
    [SerializeField] private Sprite[] fireFrames; 
    [SerializeField] private Sprite[] pumpFrames; 
    [SerializeField] private float timePerFrame = 0.05f; 
    
    [Header("Dynamic: Weapon Sway (Look)")]
    [SerializeField] private float swayAmount = 8f;
    [SerializeField] private float maxSwayAmount = 15f;
    [SerializeField] private float swaySmoothness = 10f;

    [Header("Dynamic: Movement Bob (Walk/Run)")]
    [SerializeField] private float bobSpeed = 12f;
    [SerializeField] private float bobAmount = 15f;

    [Header("Dynamic: Punchy Recoil Feel (No Scaling)")]
    // [แก้ไข] เพิ่มแรงกระตุกถอยหลังและลงล่างให้รุนแรงขึ้นเพื่อชดเชยการลบ Scale
    [SerializeField] private Vector2 recoilKickback = new Vector2(0f, -150f); 
    // [แก้ไข] เพิ่มมุมเอียงปืนตอนโดนถีบให้เยอะขึ้น (สะใจขึ้น)
    [SerializeField] private float recoilRotation = 12f; 
    [SerializeField] private float recoilRecoverySpeed = 10f; // ความเร็วดึงปืนกลับ

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;

    private bool isShooting = false;
    private Vector2 originalPosition;

    // ตัวแปรเก็บค่า Dynamic
    private Vector2 currentSway;
    private Vector2 currentBob;
    private float bobTimer;
    
    private Vector2 currentRecoilPos;
    private float currentRecoilRot;

    void Start()
    {
        if (gunRectTransform != null)
        {
            originalPosition = gunRectTransform.anchoredPosition;
        }
        
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        
        if (gunImage != null && idleSprite != null)
        {
            gunImage.sprite = idleSprite;
        }
    }

    void Update()
    {
        // ยิง
        if (Input.GetMouseButtonDown(0) && !isShooting)
        {
            StartCoroutine(ShootSequence());
        }

        HandleDynamicWeaponFeel();
    }

    private void HandleDynamicWeaponFeel()
    {
        if (gunRectTransform == null) return;

        // 1. Weapon Sway (หน่วงตามเมาส์)
        float mouseX = -Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = -Input.GetAxis("Mouse Y") * swayAmount;
        mouseX = Mathf.Clamp(mouseX, -maxSwayAmount, maxSwayAmount);
        mouseY = Mathf.Clamp(mouseY, -maxSwayAmount, maxSwayAmount);
        currentSway = Vector2.Lerp(currentSway, new Vector2(mouseX, mouseY), Time.deltaTime * swaySmoothness);

        // 2. Movement Bob (เด้งตามการเดิน)
        float currentSpeed = playerMovement != null ? playerMovement.GetCurrentSpeed() : 0f;
        if (currentSpeed > 1f) // ถ้ากำลังเดิน/วิ่ง
        {
            // เร่งจังหวะ Bob ตามความเร็วตัวละคร
            bobTimer += Time.deltaTime * bobSpeed * (currentSpeed / 10f); 
            currentBob.x = Mathf.Cos(bobTimer / 2f) * bobAmount; // ขยับซ้ายขวาแบบ Figure-8
            currentBob.y = Mathf.Sin(bobTimer) * bobAmount;      // ขยับขึ้นลง
        }
        else // ถ้าหยุดนิ่ง
        {
            bobTimer = 0f;
            currentBob = Vector2.Lerp(currentBob, Vector2.zero, Time.deltaTime * 5f);
        }

        // 3. Recoil Recovery (ดึงค่า Recoil กลับเป็น 0 อย่างนุ่มนวล)
        currentRecoilPos = Vector2.Lerp(currentRecoilPos, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
        currentRecoilRot = Mathf.Lerp(currentRecoilRot, 0f, Time.deltaTime * recoilRecoverySpeed);

        // === รวมร่างทุกระบบแล้วอัปเดตใส่ UI ปืน ===
        // ตำแหน่ง (Position)
        gunRectTransform.anchoredPosition = originalPosition + currentSway + currentBob + currentRecoilPos;
        
        // การหมุน (Rotation - เอียงเวลาหันเมาส์ + เอียงจาก Recoil)
        float tiltOffset = currentSway.x * 0.2f; 
        gunRectTransform.localRotation = Quaternion.Euler(0, 0, currentRecoilRot + tiltOffset);
        
        // === [ลบส่วน Scale ทิ้งไปแล้ว] ===
    }

    private IEnumerator ShootSequence()
    {
        isShooting = true;

        // --- เพิ่มแรงถีบแบบ Dynamic (Punchy) ---
        // สุ่มให้มันเอียงซ้ายหรือขวานิดๆ เพื่อความเป็นธรรมชาติ
        float randomRot = Random.Range(-recoilRotation, recoilRotation);
        // สุ่มให้ปืนเยื้องไปซ้ายหรือขวานิดหน่อยเวลาถีบ
        float randomX = Random.Range(-30f, 30f); 
        
        currentRecoilPos = recoilKickback + new Vector2(randomX, 0); // กระตุกปืนลงและเยื้องข้าง
        currentRecoilRot = randomRot; // ปืนเอียง
        // === [ลบ currentRecoilScale ทิ้งไปแล้ว] ===

        // โชว์รูปและทำดาเมจ
        if (fireFrames.Length > 0)
        {
            gunImage.sprite = fireFrames[0];
            FirePellets();

            if (playerMovement != null)
            {
                Vector3 recoilDir = -playerCamera.transform.forward;
                playerMovement.ApplyRecoil(recoilDir * playerRecoilForce);
            }

            yield return new WaitForSeconds(timePerFrame);

            for (int i = 1; i < fireFrames.Length; i++)
            {
                gunImage.sprite = fireFrames[i];
                yield return new WaitForSeconds(timePerFrame);
            }
        }

        if (pumpFrames.Length > 0)
        {
            for (int i = 0; i < pumpFrames.Length; i++)
            {
                gunImage.sprite = pumpFrames[i];
                yield return new WaitForSeconds(timePerFrame);
            }
        }

        gunImage.sprite = idleSprite;
        isShooting = false;
    }

    private void FirePellets()
    {
        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 spread = playerCamera.transform.forward;
            spread = Quaternion.AngleAxis(Random.Range(-spreadAngle, spreadAngle), playerCamera.transform.up) * spread;
            spread = Quaternion.AngleAxis(Random.Range(-spreadAngle, spreadAngle), playerCamera.transform.right) * spread;

            if (Physics.Raycast(playerCamera.transform.position, spread, out RaycastHit hit, range))
            {
                EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damagePerPellet);
                }
            }
        }
    }
}