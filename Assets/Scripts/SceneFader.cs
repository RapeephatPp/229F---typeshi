using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float defaultDuration = 0.5f;

    bool isFading = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (canvasGroup != null && canvasGroup.alpha > 0f)
        {
            StartCoroutine(Fade(1f, 0f, defaultDuration));
        }
    }

    public void FadeToScene(string sceneName, float duration = -1f)
    {
        if (isFading) return;
        if (duration <= 0f) duration = defaultDuration;

        StartCoroutine(FadeToSceneRoutine(sceneName, duration));
    }

    // ---------------------------------------------------------
    // [NEW] เพิ่มฟังก์ชันนี้สำหรับ Effect ตอนนอนพัก (หน้ามืด -> ทำคำสั่ง -> สว่าง)
    // ---------------------------------------------------------
    public void FadeAndExecute(System.Action midAction, float duration = 0.5f)
    {
        if (isFading) return;
        StartCoroutine(FadeAndExecuteRoutine(midAction, duration));
    }

    IEnumerator FadeAndExecuteRoutine(System.Action midAction, float duration)
    {
        isFading = true;

        // 1. Fade มืด
        yield return Fade(0f, 1f, duration);

        // 2. ทำคำสั่งที่ส่งมา (เช่น ฮีลเลือด) และรอแป๊บนึงให้คนเล่นรู้สึกว่าพักผ่อน
        midAction?.Invoke();
        yield return new WaitForSeconds(0.5f); 

        // 3. Fade สว่าง
        yield return Fade(1f, 0f, duration);

        isFading = false;
    }
    // ---------------------------------------------------------

    IEnumerator FadeToSceneRoutine(string sceneName, float duration)
    {
        isFading = true;
        yield return Fade(0f, 1f, duration);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
        yield return null;

        yield return Fade(1f, 0f, duration);
        isFading = false;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;

        canvasGroup.blocksRaycasts = true;
        float t = 0f;
        canvasGroup.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            canvasGroup.alpha = a;
            yield return null;
        }

        canvasGroup.alpha = to;
        canvasGroup.blocksRaycasts = (to > 0.01f);
    }
}