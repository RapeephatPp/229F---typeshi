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
    [SerializeField] private float fireRate = 1f; 
    [SerializeField] private int magSize = 5; // ความจุกระสุนในปืน (นัด)
    [SerializeField] private int startingTotalAmmo = 15; // กระสุนสำรองเริ่มต้น
    [SerializeField] private int maxTotalAmmo = 50; // พกกระสุนสำรองได้มากสุด
    [SerializeField] private KeyCode reloadKey = KeyCode.R; 
    
    [Header("Effects")]
    [SerializeField] private GameObject bulletTracePrefab;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptyGunSound;
    private AudioSource audioSource;

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
    
    // ตัวแปรระบบกระสุน
    private int currentAmmo; // กระสุนในปืนตอนนี้
    private int totalAmmo;   // กระสุนสำรองทั้งหมด

    private Vector2 currentSway;
    private Vector2 currentBob;
    private float bobTimer;
    private Vector2 currentRecoilPos;
    private float currentRecoilRot;

    void Start()
    {
        // เริ่มเกมมา เติมกระสุนให้เต็มแม็ก และตั้งค่ากระสุนสำรอง
        currentAmmo = magSize; 
        totalAmmo = startingTotalAmmo;

        if (gunRectTransform != null) originalPosition = gunRectTransform.anchoredPosition;
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (gunImage != null && idleSprite != null) gunImage.sprite = idleSprite;

        // สังเคราะห์ AudioSource เข้ากับปืนเพื่อเล่นเสียงดึงจากค่า Slider VFXVol
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = PlayerPrefs.GetFloat("VFXVol", 1f);
    }

    void Update()
    {   
        if (Time.timeScale == 0f) return;
        
        // ระบบยิง
        if (Input.GetMouseButtonDown(0) && !isShooting && !isReloading && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                StartCoroutine(ShootSequence());
            }
            else if (totalAmmo > 0)
            {
                // ถ้าในปืนหมด แต่มีกระสุนสำรอง ให้รีโหลด
                StartCoroutine(ReloadSequence());
            }
            else
            {
                // กระสุนหมดเกลี้ยง! เล่นเสียงแกร๊ก
                if (emptyGunSound != null) audioSource.PlayOneShot(emptyGunSound);
                Debug.Log("Out of Ammo!");
            }
        }

        // กดรีโหลดด้วยปุ่ม R (ต้องกระสุนในปืนไม่เต็ม และมีกระสุนสำรองเหลือ)
        if (Input.GetKeyDown(reloadKey) && !isShooting && !isReloading && currentAmmo < magSize && totalAmmo > 0)
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
        currentAmmo--; // หักกระสุนในปืน 1 นัด
        nextFireTime = Time.time + fireRate; 

        // เล่นเสียงยิงปืน
        if (shootSound != null) audioSource.PlayOneShot(shootSound); 

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

        // เล่นเสียงรีโหลด (อาจจะดึงความยาวของ Animation ให้ตรงกับเสียงได้)
        if (reloadSound != null) audioSource.PlayOneShot(reloadSound);

        // คำนวณจำนวนกระสุนที่ต้องหยิบมาจากกระเป๋า
        int ammoNeeded = magSize - currentAmmo;
        if (totalAmmo < ammoNeeded) 
        {
            ammoNeeded = totalAmmo; // ถ้ากระสุนสำรองมีไม่พอเติมเต็มแม็ก ก็หยิบมาเท่าที่มี
        }

        // ดันปืนลงเพื่อซ่อนตอนรีโหลด
        currentRecoilPos = new Vector2(0, -50f);
        currentRecoilRot = 15f;

        for (int round = 0; round < 2; round++)
        {
            if (pumpFrames.Length > 0)
            {
                for (int i = 0; i < pumpFrames.Length; i++)
                {
                    gunImage.sprite = pumpFrames[i];
                    yield return new WaitForSeconds(timePerFrame * 1.5f);
                }
            }
        }

        // โอนย้ายกระสุนจากสำรองมาใส่ปืน
        totalAmmo -= ammoNeeded;
        currentAmmo += ammoNeeded; 
        
        gunImage.sprite = idleSprite;
        isReloading = false;
    }

    private void FirePellets()
    {
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
                
                SpawnTrace(fakeBarrelEnd, hit.point);
            }
            else
            {
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

    // ===============================================
    // ฟังก์ชันใหม่: เอาไว้เรียกตอนเก็บกล่อง Ammo Pickup
    // ===============================================
    public void AddAmmo(int amount)
    {
        totalAmmo += amount;
        if (totalAmmo > maxTotalAmmo) 
        {
            totalAmmo = maxTotalAmmo; // กันไม่ให้พกกระสุนเกินขีดจำกัด
        }
    }

    // Getters สำหรับ UI
    public int GetCurrentAmmo() { return currentAmmo; }
    public int GetTotalAmmo() { return totalAmmo; }
}