using UnityEngine;

public class FadeAnimation : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 1f; // tempo do fade in/out
    public bool startVisible = true;

    private void Start()
    {
        if (startVisible)
            canvasGroup.alpha = 1f;
        else
            canvasGroup.alpha = 0f;

        StartCoroutine(BlinkLoop());
    }

    private System.Collections.IEnumerator BlinkLoop()
    {
        while (true)
        {
            // Fade out
            yield return StartCoroutine(FadeTo(0f));
            // Fade in
            yield return StartCoroutine(FadeTo(1f));
        }
    }

    private System.Collections.IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}
