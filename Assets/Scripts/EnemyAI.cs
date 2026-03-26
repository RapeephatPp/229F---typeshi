using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("ใส่ Player Transform ที่ต้องการให้ AI วิ่งตาม")]
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 5f;
    [Tooltip("ความเร็วในการปีน/กระโดด ข้ามสิ่งกีดขวาง")]
    public float climbSpeed = 3f;

    [Header("Combat Settings")]
    public float attackDamage = 20f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    


    private NavMeshAgent agent;
    private PlayerHealth playerHealth;
    private float lastAttackTime;
    private bool isClimbing = false;
    private float lastClimbAttempt = 0f;
    private float lastPathUpdate = 0f;

    private void Start()
    {
        // ปราบอาการ "ผีเด้งดึ๋ง": ถ้า AI มี Rigidbody เผลอติดมาฟิสิกส์มันจะตีกับเรดาร์ทางเดินจนสั่น แก้โดยปิดแรงโน้มถ่วงทิ้งซะ
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.autoTraverseOffMeshLink = true; // ปล่อยให้เดินข้ามทางเองแบบสมูทๆ แทนการโดดเด้ง

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            else Debug.LogError("ศัตรูหาผู้เล่นไม่เจอ! ตรวจสอบว่าช่องตั้งค่า Player ของศัตรูว่างอยู่ และตัวผู้เล่นไม่ได้ตั้งแท็กเป็น 'Player' หรือไม่?");
        }

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (player == null || isClimbing || agent == null) return;

        moveSpeed = agent.speed;

        // แก้อาการเดินอืด/หนืด โดยปรับอัตราเร่ง (Acceleration) ให้สัมพันธ์กับความเร็ว
        if (agent.acceleration < moveSpeed * 5f) agent.acceleration = moveSpeed * 5f;
        if (agent.angularSpeed < 800f) agent.angularSpeed = 800f;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (agent.isOnNavMesh)
        {
            // ซิงค์การเดินแค่ 12 สแกนต่อวินาที ป้องกันบั๊ก NavMesh ไถลขอบแล้วกระตุกเด้งรัวๆ
            if (Time.time > lastPathUpdate + 0.08f)
            {
                agent.SetDestination(player.position);
                lastPathUpdate = Time.time;
            }
        }

        if (distanceToPlayer <= attackRange)
        {
            AttackPlayer();
        }

        // --- ระบบจับติดกำแพง แล้วปีนอัตโนมัติ (Parkour) ---
        // กระโดดเฉพาะตอนที่ AI เดินชนขอบกำแพง/หน้าผา แล้วทางไปต่อขาด
        if (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            // ถ้าวิ่งมาถึงขอบทางขาดแล้วความเร็วตก (ไถลขอบตาราง NavMesh) ให้หาทางปีนหรือโดดลง
            if (agent.velocity.sqrMagnitude < 2.5f)
            {
                TryAutoParkour();
            }
        }
    }

    private void TryAutoParkour()
    {
        if (Time.time < lastClimbAttempt + 0.3f) return; // ติดคูลดาวน์กันรัวเกิน 0.3 วิ
        lastClimbAttempt = Time.time;

        Vector3 rayStart = transform.position + Vector3.up * 0.5f; 
        
        Vector3 forwardDir = player.position - transform.position;
        forwardDir.y = 0;
        if (forwardDir.sqrMagnitude > 0.01f)
            forwardDir.Normalize();
        else
            forwardDir = transform.forward;

        // แก้ไขปัญหายิงเรดาร์ติดตัวเองหรือผู้เล่น (Raycast Hit Self)
        RaycastHit[] fwdHits = Physics.RaycastAll(rayStart, forwardDir, 2.5f);
        bool hitWall = false;
        RaycastHit wallHit = new RaycastHit();
        foreach (var h in fwdHits)
        {
            if (h.collider.transform.root == transform.root || h.collider.transform.root == player.root) continue;
            // ไม่ชนตัวเอง ไม่ชนผู้เล่น ถือว่าเป็นกำแพงกล่อง
            wallHit = h;
            hitWall = true;
            break;
        }

        if (hitWall) // ถ้ามีกำแพงขวาง แปลว่าต้องโดดขึ้น
        {
            Vector3 topDownStart = wallHit.point + (forwardDir * 0.5f) + (Vector3.up * 4f);
            RaycastHit[] downHits = Physics.RaycastAll(topDownStart, Vector3.down, 6f);
            foreach (var r in downHits)
            {
                if (r.collider.transform.root == transform.root || r.collider.transform.root == player.root) continue;
                
                // ตรวจสอบความสูงว่าเกิน 0.6m ค่อยโดด (น้อยกว่านั้นเดินก้าวขึ้นสบายๆ หนีบั๊กกระโดดรัว)
                if (r.point.y > transform.position.y + 0.6f)
                {
                    StartCoroutine(PerformManualJump(r.point, true));
                    return; 
                }
            }
        }
        else // ถ้าโล่ง ไม่มีกำแพงบัง แปลว่าอยู่ริมหน้าผา ต้องโดดลง
        {
            // ยิงเรดาร์ลงพื้นข้างหน้า 1.5 เมตร (กันยิงเลยขอบกล่องกว้างๆ จนพลาดเป้า)
            Vector3 dropCheckStart = transform.position + (forwardDir * 1.5f) + (Vector3.up * 0.5f);
            RaycastHit[] dropHits = Physics.RaycastAll(dropCheckStart, Vector3.down, 15f);
            foreach (var f in dropHits)
            {
                if (f.collider.transform.root == transform.root || f.collider.transform.root == player.root) continue;
                
                // กระโดดลงต่อเมื่อสูงกว่า 0.8m เท่านั้น กันบั๊กพื้นลาดเอียงแล้วเด้งไปมา
                if (f.point.y < transform.position.y - 0.8f)
                {
                    StartCoroutine(PerformManualJump(f.point, false));
                    return;
                }
            }
        }
    }

    private IEnumerator PerformManualJump(Vector3 targetPos, bool isJumpingUp)
    {
        isClimbing = true;
        agent.enabled = false; 

        Vector3 finalLandingPos = targetPos;
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
        {
            finalLandingPos = navHit.position;
        }

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        // หันหน้าไปหาจุดตกตอนลอยตัว
        Vector3 lookDir = finalLandingPos - startPos;
        lookDir.y = 0;
        Quaternion targetRot = startRot;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            targetRot = Quaternion.LookRotation(lookDir);
        }

        float distance = Vector3.Distance(startPos, finalLandingPos);
        // ปรับเวลาการกระโดดให้นานขึ้นนิดนึงและสมูทขึ้นตามระทาง (ไม่เร็วกระชาก)
        float jumpTime = Mathf.Max(0.6f, distance / (moveSpeed * 0.8f));
        float journey = 0f;
        
        // คำนวณความสูงให้โก่งข้ามขอบพ้นพอดี ไม่มุดทะลุกำแพง
        float yDifference = Mathf.Abs(finalLandingPos.y - startPos.y);
        float jumpHeightOffset = isJumpingUp ? (yDifference * 0.4f + 1.2f) : 0.5f;
        
        while (journey < 1f)
        {
            journey += Time.deltaTime / jumpTime;
            
            // ใช้ SmoothStep ทำให้เริ่มกระโดดและตอนลงพื้นดูมีน้ำหนัก (Ease in/out)
            float moveProgress = Mathf.SmoothStep(0f, 1f, journey);
            float heightCurve = Mathf.Sin(Mathf.PI * journey); 
            
            Vector3 lerpPos = Vector3.Lerp(startPos, finalLandingPos, moveProgress);
            // โค้งพาราโบลา
            lerpPos.y += (heightCurve * jumpHeightOffset);
            
            transform.position = lerpPos;
            
            // ให้หันหน้าเสร็จตั้งแต่ตอนลอยตัวครึ่งทางแรก จะได้ดูเป็นธรรมชาติ
            float rotProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(journey * 2f));
            transform.rotation = Quaternion.Slerp(startRot, targetRot, rotProgress);
            
            yield return null;
        }

        transform.position = finalLandingPos;
        transform.rotation = targetRot;
        
        agent.enabled = true; 
        yield return null;
        isClimbing = false;
    }

    private IEnumerator ClimbOrJump()
    {
        isClimbing = true;
        OffMeshLinkData data = agent.currentOffMeshLinkData;

        // จุดเริ่มต้น และจุดหมายปลายทางของการปีน
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        
        float journey = 0f;
        while (journey < 1f)
        {
            journey += Time.deltaTime * climbSpeed;
            // ลดความดุเดือดของการเด้ง OffMeshLink อัตโนมัติลง เหลือแค่พุ่งเรียบๆ
            float heightCurve = Mathf.Sin(Mathf.PI * journey); 
            agent.transform.position = Vector3.Lerp(startPos, endPos, journey) + (Vector3.up * heightCurve * 0.3f);
            
            yield return null;
        }

        agent.CompleteOffMeshLink();
        isClimbing = false;
    }

    private void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            agent.isStopped = true;
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }

            lastAttackTime = Time.time;
            Invoke("ResumeAgent", 0.5f);
        }
    }

    private void ResumeAgent()
    {
        if (agent != null && agent.isOnNavMesh && !isClimbing)
        {
            agent.isStopped = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
