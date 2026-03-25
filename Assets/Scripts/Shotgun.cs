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
    
    [Header("Ammo & Fire Rate")]
    [SerializeField] private float fireRate = 1f; // Shoot speed = 1 (ยิงได้วิละ 1 นัด)
    [SerializeField] private int maxAmmo = 5; // Max Ammo
    [SerializeField] private KeyCode reloadKey = KeyCode.R; // Reload button
    
    [Header("Effects")]
    [SerializeField] private GameObject bulletTracePrefab;

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

    [Header("Dynamic: Punchy Recoil Feel")]
    [SerializeField] private Vector2 recoilKickback = new Vector2(0f, -150f); 
    [SerializeField] private float recoilRotation = 12f; 
    [SerializeField] private float recoilRecoverySpeed = 10f; 

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;

    private bool isShooting = false;
    private bool isReloading = false;
    private Vector2 originalPosition;
    private float nextFireTime = 0f;
    private int currentAmmo; // เก็บกระสุนปัจจุบัน

    private Vector2 currentSway;
    private Vector2 currentBob;
    private float bobTimer;
    private Vector2 currentRecoilPos;
    private float currentRecoilRot;

    void Start()
    {
        currentAmmo = maxAmmo; 

        if (gunRectTransform != null) originalPosition = gunRectTransform.anchoredPosition;
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (gunImage != null && idleSprite != null) gunImage.sprite = idleSprite;
    }

    void Update()
    {   
        if (Time.timeScale == 0f) return;
        
        // Shoot systems
        if (Input.GetMouseButtonDown(0) && !isShooting && !isReloading && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                StartCoroutine(ShootSequence());
            }
            else
            {
                // If out of ammo reload
                StartCoroutine(ReloadSequence());
            }
        }

        // Reload with R key
        if (Input.GetKeyDown(reloadKey) && !isShooting && !isReloading && currentAmmo < maxAmmo)
        {
            StartCoroutine(ReloadSequence());
        }

        HandleDynamicWeaponFeel();
    }

    private void HandleDynamicWeaponFeel()
    {
        if (gunRectTransform == null) return;

        float mouseX = -Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = -Input.GetAxis("Mouse Y") * swayAmount;
        mouseX = Mathf.Clamp(mouseX, -maxSwayAmount, maxSwayAmount);
        mouseY = Mathf.Clamp(mouseY, -maxSwayAmount, maxSwayAmount);
        currentSway = Vector2.Lerp(currentSway, new Vector2(mouseX, mouseY), Time.deltaTime * swaySmoothness);

        float currentSpeed = playerMovement != null ? playerMovement.GetCurrentSpeed() : 0f;
        if (currentSpeed > 1f) 
        {
            bobTimer += Time.deltaTime * bobSpeed * (currentSpeed / 10f); 
            currentBob.x = Mathf.Cos(bobTimer / 2f) * bobAmount; 
            currentBob.y = Mathf.Sin(bobTimer) * bobAmount;      
        }
        else 
        {
            bobTimer = 0f;
            currentBob = Vector2.Lerp(currentBob, Vector2.zero, Time.deltaTime * 5f);
        }

        currentRecoilPos = Vector2.Lerp(currentRecoilPos, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
        currentRecoilRot = Mathf.Lerp(currentRecoilRot, 0f, Time.deltaTime * recoilRecoverySpeed);

        gunRectTransform.anchoredPosition = originalPosition + currentSway + currentBob + currentRecoilPos;
        float tiltOffset = currentSway.x * 0.2f; 
        gunRectTransform.localRotation = Quaternion.Euler(0, 0, currentRecoilRot + tiltOffset);
    }

    private IEnumerator ShootSequence()
    {
        isShooting = true;
        currentAmmo--; 
        nextFireTime = Time.time + fireRate; 

        float randomRot = Random.Range(-recoilRotation, recoilRotation);
        float randomX = Random.Range(-30f, 30f); 
        
        currentRecoilPos = recoilKickback + new Vector2(randomX, 0); 
        currentRecoilRot = randomRot; 

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

        // Pump action animation
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

    private IEnumerator ReloadSequence()
    {
        isReloading = true;

        // ดันปืนลงเพื่อซ่อนตอนรีโหลด
        currentRecoilPos = new Vector2(0, -50f);
        currentRecoilRot = 15f;

        // วนลูปเล่นแอนิเมชันชักปืนจนกว่ากระสุนจะเต็ม (หรือเล่นซ้ำ 2 รอบเพื่อให้ดูนานขึ้น)
        for (int round = 0; round < 2; round++)
        {
            if (pumpFrames.Length > 0)
            {
                for (int i = 0; i < pumpFrames.Length; i++)
                {
                    gunImage.sprite = pumpFrames[i];
                    yield return new WaitForSeconds(timePerFrame * 1.5f); // เล่นช้าลงนิดนึงตอนรีโหลด
                }
            }
        }

        currentAmmo = maxAmmo; // เติมกระสุนเต็ม
        gunImage.sprite = idleSprite;
        isReloading = false;
    }

    private void FirePellets()
    {
        // จุดกำเนิดรอยกระสุน (จำลองว่ายิงออกมาจากมุมขวาล่างของกล้อง)
        Vector3 fakeBarrelEnd = playerCamera.transform.position + playerCamera.transform.forward * 0.8f - playerCamera.transform.up * 0.2f + playerCamera.transform.right * 0.2f;

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
                
                // วาดรอยกระสุนไปที่จุดที่โดน
                SpawnTrace(fakeBarrelEnd, hit.point);
            }
            else
            {
                // ถ้าไม่โดนอะไรเลย ให้วาดรอยกระสุนไปสุดระยะ
                SpawnTrace(fakeBarrelEnd, playerCamera.transform.position + spread * range);
            }
        }
    }

    private void SpawnTrace(Vector3 start, Vector3 end)
    {
        if (bulletTracePrefab != null)
        {
            GameObject trace = Instantiate(bulletTracePrefab, start, Quaternion.identity);
            BulletTrace traceScript = trace.GetComponent<BulletTrace>();
            if (traceScript != null)
            {
                traceScript.SetTrace(start, end);
            }
        }
    }

    // Getter สำหรับให้ UI ดึงข้อมูลไปแสดง
    public int GetCurrentAmmo() { return currentAmmo; }
    public int GetMaxAmmo() { return maxAmmo; }
}