using System.Runtime.Serialization;
using System.Security;
using UnityEngine;

public class PendulumSwing : MonoBehaviour
{
       [SerializeField] float damage = 20f;     

public float pushForce = 15f;     
    public float maxSpeed = 8f;      
    public float startKickForce = 50f; 
    public float knockbackForce = 15f; // ความแรงตอนกระเด็น

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
       
        rb.AddForce(new Vector3(0, 0, startKickForce), ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        // 1. อ่านค่าความเร็วปัจจุบันของลูกบอล ว่าพุ่งไปตามแกน Z เท่าไหร่
        
        float currentVelocityZ = rb.linearVelocity.z; 

        
        if (Mathf.Abs(currentVelocityZ) < maxSpeed)
        {
           
            if (currentVelocityZ > 0.1f)
            {
                rb.AddForce(new Vector3(0, 0, pushForce), ForceMode.Force);
            }
            
            else if (currentVelocityZ < -0.1f)
            {
                rb.AddForce(new Vector3(0, 0, -pushForce), ForceMode.Force);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           
            PlayerHealth ply = other.GetComponent<PlayerHealth>();
            if (ply != null)
            {
                ply.TakeDamage(damage); 
            }

            // (Knockback)
            KnockbackReceiver kbReceiver = other.GetComponent<KnockbackReceiver>();
            if (kbReceiver != null)
            {
                // หาระยะทิศทางจาก "ลูกตุ้ม" พุ่งไปหา "ผู้เล่น"
                Vector3 pushDirection = other.transform.position - transform.position;
                
                
                kbReceiver.AddImpact(pushDirection, knockbackForce);
            }
        }
    }
}