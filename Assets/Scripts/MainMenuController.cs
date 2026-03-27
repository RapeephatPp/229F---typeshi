using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // ใช้สำหรับ TextMeshPro

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private CanvasGroup mainMenuPanel;
    [SerializeField] private CanvasGroup optionsPanel;
    [SerializeField] private CanvasGroup creditsPanel;

    [Header("Animation Settings")]
    [SerializeField] private float transitionSpeed = 0.3f;
    [SerializeField] private Vector2 slideOffset = new Vector2(500f, 0f);

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

    private CanvasGroup currentPanel;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        currentPanel = mainMenuPanel;
        ShowPanelImmediate(mainMenuPanel);
        HidePanelImmediate(optionsPanel);
        HidePanelImmediate(creditsPanel);

        LoadSettings();
    }

    // ==========================================
    // MENU NAVIGATION 
    // ==========================================
    public void PlayGame()
    {
        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene("SampleScene"); // Insert First stage here
        else
            SceneManager.LoadScene("SampleScene");
    }

    public void OpenOptions() { SwitchPanel(optionsPanel); }
    public void OpenCredits() { SwitchPanel(creditsPanel); }
    public void BackToMainMenu() { SwitchPanel(mainMenuPanel); }

    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }

    // ==========================================
    // SETTINGS LOGIC 
    // ==========================================
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
        
        UpdateValueTexts(); // อัปเดตตัวเลขบนจอทันที

        AudioListener.volume = masterVolSlider.value;
    }

    private void LoadSettings()
    {
        if (masterVolSlider == null) return;
        
        masterVolSlider.value = PlayerPrefs.GetFloat("MasterVol", 0.5f);
        musicVolSlider.value = PlayerPrefs.GetFloat("MusicVol", 0.5f);
        vfxVolSlider.value = PlayerPrefs.GetFloat("VFXVol", 0.5f);

        fovSlider.value = PlayerPrefs.GetFloat("FOV", 60f); 
        sensSlider.value = PlayerPrefs.GetFloat("Sensitivity", 200f);

        headBobToggle.isOn = PlayerPrefs.GetInt("HeadBob", 1) == 1;
        screenShakeToggle.isOn = PlayerPrefs.GetInt("ScreenShake", 1) == 1;

        UpdateValueTexts(); // อัปเดตตัวเลขตอนเปิดหน้าต่างครั้งแรก
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

    // ฟังก์ชันแปลงค่าจาก Slider มาเป็นตัวหนังสือ
    private void UpdateValueTexts()
    {
        if (masterVolText != null) masterVolText.text = Mathf.RoundToInt(masterVolSlider.value * 100) + "%";
        if (musicVolText != null) musicVolText.text = Mathf.RoundToInt(musicVolSlider.value * 100) + "%";
        if (vfxVolText != null) vfxVolText.text = Mathf.RoundToInt(vfxVolSlider.value * 100) + "%";
        if (fovText != null) fovText.text = Mathf.RoundToInt(fovSlider.value).ToString();
        if (sensText != null) sensText.text = Mathf.RoundToInt(sensSlider.value).ToString();
    }

    // ==========================================
    // UI ANIMATIONS (Slide & Fade)
    // ==========================================
    private void SwitchPanel(CanvasGroup targetPanel)
    {
        if (currentPanel == targetPanel) return;
        StartCoroutine(AnimateTransition(currentPanel, targetPanel));
        currentPanel = targetPanel;
    }

    private IEnumerator AnimateTransition(CanvasGroup hidePanel, CanvasGroup showPanel)
    {
        showPanel.gameObject.SetActive(true);
        showPanel.alpha = 0f;
        showPanel.blocksRaycasts = false;
        
        RectTransform hideRect = hidePanel.GetComponent<RectTransform>();
        RectTransform showRect = showPanel.GetComponent<RectTransform>();

        showRect.anchoredPosition = slideOffset;
        Vector2 hideTargetPos = -slideOffset; 

        float time = 0f;
        while (time < transitionSpeed)
        {
            time += Time.unscaledDeltaTime; 
            float t = time / transitionSpeed;
            t = t * t * (3f - 2f * t); 

            hidePanel.alpha = Mathf.Lerp(1f, 0f, t);
            showPanel.alpha = Mathf.Lerp(0f, 1f, t);

            hideRect.anchoredPosition = Vector2.Lerp(Vector2.zero, hideTargetPos, t);
            showRect.anchoredPosition = Vector2.Lerp(slideOffset, Vector2.zero, t);

            yield return null;
        }

        HidePanelImmediate(hidePanel);
        ShowPanelImmediate(showPanel);
        hideRect.anchoredPosition = Vector2.zero; 
    }

    private void HidePanelImmediate(CanvasGroup panel)
    {
        panel.alpha = 0f;
        panel.blocksRaycasts = false;
        panel.interactable = false;
        panel.gameObject.SetActive(false);
    }

    private void ShowPanelImmediate(CanvasGroup panel)
    {
        panel.alpha = 1f;
        panel.blocksRaycasts = true;
        panel.interactable = true;
        panel.gameObject.SetActive(true);
        panel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }
}