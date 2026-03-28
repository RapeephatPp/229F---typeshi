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
        // ต่อ OnValueChanged เข้ากับ ApplySettings เพื่อให้อัปเดตทันทีที่เลื่อน Slider
        if (masterVolSlider != null) masterVolSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (musicVolSlider != null) musicVolSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (vfxVolSlider != null) vfxVolSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (fovSlider != null) fovSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (sensSlider != null) sensSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (headBobToggle != null) headBobToggle.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (screenShakeToggle != null) screenShakeToggle.onValueChanged.AddListener(delegate { ApplySettings(); });

        LoadSettingsUI(); 

        currentLevelName = SceneManager.GetActiveScene().name;
        // ดึงสถิติเวลา Best Time (เซฟแยกตามชื่อด่าน)
        bestTime = PlayerPrefs.GetFloat("BestTime_" + currentLevelName, 0f); 
        
        // สังเกตว่าเรา "เอาบรรทัด sessionTime = 0 ออกไปแล้ว"
        // เพื่อให้เวลาโหลดด่านใหม่ / Restart ตัวเลขเวลายังเดินต่อจากของเดิม
    }

    private void Update()
    {
        // นับเวลาไปเรื่อยๆ ตราบใดที่ยังจับเวลาอยู่ และไม่ได้ Pause หรือตาย
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

    // ==============================
    // LEVEL COMPLETION (จบด่าน)
    // ==============================
    public void LevelComplete()
    {
        // สามารถปล่อยให้เวลาเดินต่อได้ถ้านี่ไม่ใช่ด่านสุดท้าย (คอมเมนต์บรรทัดล่างออก)
        // isSessionTimerActive = false; 

        // เช็คว่าทำลายสถิติไหม (Split Time)
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
        }
        
        AudioListener.volume = masterVolSlider.value;
        
        // ผูกความดังของ Background Music เข้ากับตัวเลื่อน Music
        if (bgmSource != null)
        {
            bgmSource.volume = musicVolSlider.value;
        }

        // อัปเดตความดังของศัตรูทั้งหมดที่มีในฉากให้เท่ากับ VFXVol
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
        
        // ดึงความดังมาเซ็ตให้ BGM ตอนเปิดเกมครั้งแรกด้วย
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
}