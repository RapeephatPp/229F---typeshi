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
        Debug.Log("Player reached the exit! Fading to next level...");

        string targetScene = nextSceneName;

        // ถ้าตั้งให้โหลดด่านตาม Index ให้ไปดึงชื่อด่านถัดไปมา
        if (useBuildIndex)
        {          
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            string scenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
            targetScene = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }

        // เรียกใช้งาน SceneFader ถ้ามีในฉาก
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(targetScene);
        }
        else
        {
            // ถ้าลืมใส่ SceneFader ไว้ ก็ให้โหลดแบบปกติกันเหนียว
            SceneManager.LoadScene(targetScene);
        }
    }
}