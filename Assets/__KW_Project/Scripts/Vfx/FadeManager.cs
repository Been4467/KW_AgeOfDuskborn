using UnityEngine;
using System.Collections;  // ✅ 이거 추가
using UnityEngine;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    private void Awake()
    {
        // 중복 방지
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator FadeOut(float duration = 0.5f) => Fade(0f, 1f, duration);
    public IEnumerator FadeIn(float duration = 0.5f) => Fade(1f, 0f, duration);

    private IEnumerator Fade(float from, float to, float duration)
    {
        fadeCanvasGroup.alpha = from;
        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
        if (to == 0f) fadeCanvasGroup.blocksRaycasts = false;
    }
}