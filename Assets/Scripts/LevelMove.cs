using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMove : MonoBehaviour
{
   [Header("Settings")]
    public string nextSceneName;
    public bool useBuildIndex = true;

    
    private void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag("Player"))
        {
                   
            GoToNextLevel();
        }
    }

    void GoToNextLevel()
    {
        Debug.Log("Player reached the exit! Loading next level...");

        if (useBuildIndex)
        {          
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else
        {
            // โหลดฉากตามชื่อที่พิมพ์ไว้ในช่อง nextSceneName
            SceneManager.LoadScene(nextSceneName);
        }
    }
}