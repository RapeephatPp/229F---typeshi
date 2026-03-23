using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // ฟังก์ชันนี้ถูกเรียกโดยปืน Shotgun 
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        
        // เอฟเฟกต์ศัตรูโดนยิง (เช่น เปลี่ยนเป็นสีแดงแวบๆ) สามารถทำได้ที่นี่

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // ทำลายศัตรู
        Destroy(gameObject);
        // สามารถเพิ่ม Particle ศัตรูระเบิด หรือเสียงตอนตายได้
    }
}