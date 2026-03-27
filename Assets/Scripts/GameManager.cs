using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // สำคัญ: ต้องมีเพื่อใช้ TextMeshPro

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject optionsPanel;

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
        LoadSettingsUI(); 
    }

    private void Update()
    {
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
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

        UpdateValueTexts(); // อัปเดตตัวเลขบนจอทันทีที่เลื่อน

        if (playerMovement != null)
        {
            playerMovement.ApplySettingsFromSave();
        }
        AudioListener.volume = masterVolSlider.value;
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