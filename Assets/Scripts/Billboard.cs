using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCam != null)
        {
            // ล็อกแกน Y ให้ผีมองซ้ายขวาอย่างเดียว (ไม่ก้มเงยเอียงคอตามสั่นของกล้อง Head-bobbing)
            // นี่คือสิ่งที่จะแก้บั๊ก "ผีเด้งดึ๋ง" เวลาภาพเอียงจมลงไปในดินครับ
            Vector3 lookDir = mainCam.transform.forward;
            lookDir.y = 0; 
            
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }
}
