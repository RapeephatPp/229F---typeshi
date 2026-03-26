using Unity.VisualScripting;
using UnityEngine;

public class Door_Disappear : MonoBehaviour
{
    [SerializeField] GameObject key;


    // Update is called once per frame
    void Update()
    {
        if (key == null)
        {
            Destroy(this.gameObject);
        }
    }
}
