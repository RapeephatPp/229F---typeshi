using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject gameClearPanel;
    
    [Header("Game Clear UI")]
    [SerializeField] private TextMeshProUGUI clearTimeText;
    [SerializeField] private TextMeshProUGUI clearBestTimeText;

    [Header("Level Information")]
    [HideInInspector] public string currentLevelName;
    [HideInInspector] public float bestTime;
    
    public static float sessionTime = 0f;
    public static bool isSessionTimerActive = true;
    
    public float currentTime { get { return sessionTime; } }
    public bool timerActive { get { return isSessionTimerActive; } }

    [Header("Options Sliders")]
    [SerializeField] private Slider masterVolSlider;
    [SerializeField] private Slider musicVolSlider;
    [SerializeField] private Slider vfxVolSlider;
    [SerializeField] private Slider fovSlider;
    [SerializeField] private Slider sensSlider;
    [SerializeField] private Toggle headBobToggle;
    [SerializeField] private Toggle screenShakeToggle;

    [Header("Options Value Texts")]
    [SerializeField] private TextMeshProUGUI masterVolText;
    [SerializeField] private TextMeshProUGUI musicVolText;
    [SerializeField] private TextMeshProUGUI vfxVolText;
    [SerializeField] private TextMeshProUGUI fovText;
    [SerializeField] private TextMeshProUGUI sensText;

    [Header("Audio Settings")]
    [Tooltip("ใส่ AudioSource ที่ใช้เล่นเพลงแบคกราวน์ของเกม")]
    public AudioSource bgmSource;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    [HideInInspector] public bool isPaused = false;
    [HideInInspector] public bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (masterVolSlider != null) masterVolSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (musicVolSlider != null) musicVolSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (vfxVolSlider != null) vfxVolSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (fovSlider != null) fovSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (sensSlider != null) sensSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (headBobToggle != null) headBobToggle.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (screenShakeToggle != null) screenShakeToggle.onValueChanged.AddListener(delegate { ApplySettings(); });

        LoadSettingsUI(); 

        currentLevelName = SceneManager.GetActiveScene().name;
        bestTime = PlayerPrefs.GetFloat("BestTime_" + currentLevelName, 0f); 
        
    }

    private void Update()
    {
        if (isSessionTimerActive && !isPaused && !isGameOver)
        {
            sessionTime += Time.deltaTime; 
        }

        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) && !isGameOver)
        {
            if (isPaused)
            {
                if (optionsPanel != null && optionsPanel.activeSelf) CloseOptions();
                else ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    
    public void LevelComplete()
    {
        if (bestTime == 0f || sessionTime < bestTime)
        {
            bestTime = sessionTime;
            PlayerPrefs.SetFloat("BestTime_" + currentLevelName, bestTime);
            PlayerPrefs.Save();
            Debug.Log("New Best Time for " + currentLevelName + ": " + bestTime);
        }
    }

    // ==============================
    // PAUSE & GAME OVER & OPTIONS
    // ==============================
    public void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        Time.timeScale = 1f; 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GameOver()
    {
        isGameOver = true;
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        
        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene(SceneManager.GetActiveScene().name); 
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        
        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene("MainMenu"); 
        else
            SceneManager.LoadScene("MainMenu"); 
    }

    public void OpenOptions()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
        LoadSettingsUI(); 
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void ApplySettings()
    {
        PlayerPrefs.SetFloat("MasterVol", masterVolSlider.value);
        PlayerPrefs.SetFloat("MusicVol", musicVolSlider.value);
        PlayerPrefs.SetFloat("VFXVol", vfxVolSlider.value);
        PlayerPrefs.SetFloat("FOV", fovSlider.value);
        PlayerPrefs.SetFloat("Sensitivity", sensSlider.value);
        PlayerPrefs.SetInt("HeadBob", headBobToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("ScreenShake", screenShakeToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();

        UpdateValueTexts(); 

        if (playerMovement != null)
        {
            playerMovement.ApplySettingsFromSave();
            
            // ให้ปืนอัปเดตความดังด้วยเผื่อผู้เล่นปรับสไลเดอร์
            Shotgun shotgun = playerMovement.GetComponentInChildren<Shotgun>();
            if (shotgun != null) shotgun.ApplySettingsFromSave();
        }
        
        AudioListener.volume = masterVolSlider.value;
        
        if (bgmSource != null)
        {
            bgmSource.volume = musicVolSlider.value;
        }
        
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy.monsterAudioSource != null)
            {
                enemy.monsterAudioSource.volume = vfxVolSlider.value;
            }
        }
    }

    private void LoadSettingsUI()
    {
        if (masterVolSlider == null) return; 
        masterVolSlider.value = PlayerPrefs.GetFloat("MasterVol", 1f);
        musicVolSlider.value = PlayerPrefs.GetFloat("MusicVol", 1f);
        vfxVolSlider.value = PlayerPrefs.GetFloat("VFXVol", 1f);
        fovSlider.value = PlayerPrefs.GetFloat("FOV", 60f); 
        sensSlider.value = PlayerPrefs.GetFloat("Sensitivity", 200f);
        headBobToggle.isOn = PlayerPrefs.GetInt("HeadBob", 1) == 1;
        screenShakeToggle.isOn = PlayerPrefs.GetInt("ScreenShake", 1) == 1;
        
        if (bgmSource != null)
        {
            bgmSource.volume = musicVolSlider.value;
        }

        AudioListener.volume = masterVolSlider.value;

        UpdateValueTexts();
    }

    public void ResetSettings()
    {
        masterVolSlider.value = 0.5f;
        musicVolSlider.value = 0.5f;
        vfxVolSlider.value = 0.5f;
        fovSlider.value = 60f;
        sensSlider.value = 300f;
        headBobToggle.isOn = true;
        screenShakeToggle.isOn = true;
        
        ApplySettings();
    }
    
    private void UpdateValueTexts()
    {
        if (masterVolText != null) masterVolText.text = Mathf.RoundToInt(masterVolSlider.value * 100) + "%";
        if (musicVolText != null) musicVolText.text = Mathf.RoundToInt(musicVolSlider.value * 100) + "%";
        if (vfxVolText != null) vfxVolText.text = Mathf.RoundToInt(vfxVolSlider.value * 100) + "%";
        if (fovText != null) fovText.text = Mathf.RoundToInt(fovSlider.value).ToString();
        if (sensText != null) sensText.text = Mathf.RoundToInt(sensSlider.value).ToString();
    }
    
    // ==============================
    // GAME CLEAR (จบเกมสมบูรณ์)
    // ==============================
    public void GameClear()
    {
        isSessionTimerActive = false; // หยุดเวลา Speedrun

        // คำนวณหา Best Time ของ "ทั้งเกม" (เซฟแยกจากแบบรายด่าน)
        float fullGameBest = PlayerPrefs.GetFloat("FullGameBestTime", 0f);
        if (fullGameBest == 0f || sessionTime < fullGameBest)
        {
            fullGameBest = sessionTime;
            PlayerPrefs.SetFloat("FullGameBestTime", fullGameBest);
            PlayerPrefs.Save();
        }

        // แสดงผลตัวเลขลงบน UI
        if (clearTimeText != null) clearTimeText.text = "YOUR TIME: " + FormatTime(sessionTime);
        if (clearBestTimeText != null) clearBestTimeText.text = "BEST TIME: " + FormatTime(fullGameBest);

        // เปิดหน้าต่างจบเกม และหยุดเวลา
        gameClearPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ReturnToMainMenuCredits()
    {
        Time.timeScale = 1f;
        
        // เซฟ "ธง" ไว้บอกหน้า Main Menu ให้เปิดหน้า Credits ทันที
        PlayerPrefs.SetInt("ShowCreditsOnLoad", 1);
        PlayerPrefs.Save();

        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }

    // ฟังก์ชันช่วยจัดรูปแบบเวลา 00:00.00
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }
    
}