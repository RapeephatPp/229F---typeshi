using Unity.VisualScripting;
using UnityEngine;

public class Door_Disappear : MonoBehaviour
{
    [SerializeField] GameObject key;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (key == null)
        {
            Destroy(this.gameObject);
        }
    }
}
