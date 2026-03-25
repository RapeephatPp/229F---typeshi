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
    [SerializeField] private Vector2 slideOffset = new Vector2(500f, 0f); // ระยะสไลด์ออกด้านข้าง

    [Header("Sound Settings UI")]
    [SerializeField] private Slider masterVolSlider;
    [SerializeField] private Slider musicVolSlider;
    [SerializeField] private Slider vfxVolSlider;

    [Header("Player Settings UI")]
    [SerializeField] private Slider fovSlider;
    [SerializeField] private Slider sensSlider;
    [SerializeField] private Toggle headBobToggle;
    [SerializeField] private Toggle screenShakeToggle;

    private CanvasGroup currentPanel;

    void Start()
    {
        // ปลดเมาส์ให้คลิก UI ได้
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ตั้งค่าหน้าจอเริ่มต้น
        currentPanel = mainMenuPanel;
        ShowPanelImmediate(mainMenuPanel);
        HidePanelImmediate(optionsPanel);
        HidePanelImmediate(creditsPanel);

        LoadSettings();
    }

    // ==========================================
    // MENU NAVIGATION (ฟังก์ชันกดปุ่ม)
    // ==========================================
    public void PlayGame()
    {
        // ใส่ชื่อ Scene หรือ Index ของด่านแรกที่คุณต้องการโหลด
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
    // SETTINGS LOGIC (การตั้งค่า)
    // ==========================================
    public void ApplySettings()
    {
        // บันทึกค่าทั้งหมดลง PlayerPrefs
        PlayerPrefs.SetFloat("MasterVol", masterVolSlider.value);
        PlayerPrefs.SetFloat("MusicVol", musicVolSlider.value);
        PlayerPrefs.SetFloat("VFXVol", vfxVolSlider.value);
        
        PlayerPrefs.SetFloat("FOV", fovSlider.value);
        PlayerPrefs.SetFloat("Sensitivity", sensSlider.value);
        
        // Toggle ใช้ Int (1 = true, 0 = false)
        PlayerPrefs.SetInt("HeadBob", headBobToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("ScreenShake", screenShakeToggle.isOn ? 1 : 0);

        PlayerPrefs.Save();
        
        // TODO: นำค่า Volume ไปผูกกับ AudioMixer ของ Unity (ถ้ามี)
        AudioListener.volume = masterVolSlider.value;
    }

    private void LoadSettings()
    {
        // โหลดค่ามาแสดงบน UI (มีค่า Default ไว้เผื่อเพิ่งเปิดเกมครั้งแรก)
        masterVolSlider.value = PlayerPrefs.GetFloat("MasterVol", 1f);
        musicVolSlider.value = PlayerPrefs.GetFloat("MusicVol", 1f);
        vfxVolSlider.value = PlayerPrefs.GetFloat("VFXVol", 1f);

        fovSlider.value = PlayerPrefs.GetFloat("FOV", 80f); // Default FOV 80
        sensSlider.value = PlayerPrefs.GetFloat("Sensitivity", 100f);

        headBobToggle.isOn = PlayerPrefs.GetInt("HeadBob", 1) == 1;
        screenShakeToggle.isOn = PlayerPrefs.GetInt("ScreenShake", 1) == 1;
    }

    public void ResetSettings()
    {
        // คืนค่า Default ทั้งหมด
        masterVolSlider.value = 1f;
        musicVolSlider.value = 1f;
        vfxVolSlider.value = 1f;
        fovSlider.value = 80f;
        sensSlider.value = 100f;
        headBobToggle.isOn = true;
        screenShakeToggle.isOn = true;
        
        ApplySettings();
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
        // 1. นำหน้าต่างใหม่มาวางเตรียมไว้
        showPanel.gameObject.SetActive(true);
        showPanel.alpha = 0f;
        showPanel.blocksRaycasts = false;
        
        RectTransform hideRect = hidePanel.GetComponent<RectTransform>();
        RectTransform showRect = showPanel.GetComponent<RectTransform>();

        // ให้หน้าต่างใหม่เริ่มสไลด์มาจากด้านขวา
        showRect.anchoredPosition = slideOffset;
        Vector2 hideTargetPos = -slideOffset; // หน้าต่างเก่าสไลด์หลบไปซ้าย

        float time = 0f;
        while (time < transitionSpeed)
        {
            time += Time.unscaledDeltaTime; // ใช้ unscaled เผื่อเวลาเกม Pause อยู่
            float t = time / transitionSpeed;
            // ทำให้กราฟความเร็วสมูทขึ้น (Ease Out)
            t = t * t * (3f - 2f * t); 

            // อัปเดตการ Fade (Alpha)
            hidePanel.alpha = Mathf.Lerp(1f, 0f, t);
            showPanel.alpha = Mathf.Lerp(0f, 1f, t);

            // อัปเดตการ Slide (Position)
            hideRect.anchoredPosition = Vector2.Lerp(Vector2.zero, hideTargetPos, t);
            showRect.anchoredPosition = Vector2.Lerp(slideOffset, Vector2.zero, t);

            yield return null;
        }

        // 2. จัดระเบียบตอนจบ Animation
        HidePanelImmediate(hidePanel);
        ShowPanelImmediate(showPanel);
        
        // คืนตำแหน่งหน้าต่างเก่าเผื่อเรียกใช้ใหม่
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