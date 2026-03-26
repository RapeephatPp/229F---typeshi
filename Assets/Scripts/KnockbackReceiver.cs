using UnityEngine;

public class KnockbackReceiver : MonoBehaviour
{

    private CharacterController cc;
    private Vector3 impact = Vector3.zero;
    public float recoverySpeed = 5f; // ความไวในการทรงตัวกลับมาปกติ

    void Start()
    {
        
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        
        if (impact.magnitude > 0.2f)
        {
            cc.Move(impact * Time.deltaTime);
            
            // ลดแรงกระแทกเบาลงเรื่อยๆ จนหยุด 
            impact = Vector3.Lerp(impact, Vector3.zero, recoverySpeed * Time.deltaTime);
        }
    }

    // ไว้ให้เรียกใช้เพื่อส่งแรงกระแทกมา
    public void AddImpact(Vector3 direction, float force)
    {
        direction.Normalize();
        
        // งัดให้ลอยขึ้นพื้นนิดนึง จะได้กระเด็นสวยๆ
        if (direction.y < 0.5f) direction.y = 0.5f; 
        
        // รับแรงกระแทกเข้ามาสะสมไว้
        impact += direction * force;
    }
}