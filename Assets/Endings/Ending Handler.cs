using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndingHandler : MonoBehaviour
{
    public Image silhouette;
    public Image text;
    public Image black;
    public float silhouetteFadeTime = 1f;
    public float textFadeTime = 2f;
    public float zoomDuration = 2f;
    public Vector3 zoomScale = new Vector3(1f, 1f, 1f); // How much bigger the text should get
    public AudioSource audioSource;
    public AudioClip deathSound;

    void Start()
    {
        silhouette.color = new Color(silhouette.color.r, silhouette.color.g, silhouette.color.b, 0);
        text.color = new Color(text.color.r, text.color.g, text.color.b, 0);
    }

    public void Death()
    {
        StartCoroutine(FadeImage(silhouette, silhouetteFadeTime, true));
        StartCoroutine(FadeImage(text, textFadeTime, true));
        StartCoroutine(ZoomText(text, zoomDuration, true));
        audioSource.PlayOneShot(deathSound);
    }

    IEnumerator FadeImage(Image img, float duration, bool fadeIn)
    {
        float startAlpha = fadeIn ? 0 : 1;
        float endAlpha = fadeIn ? 1 : 0;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            img.color = new Color(255, 255, 255, alpha);
            yield return null;
        }
        
    }

    IEnumerator ZoomText(Image img, float duration, bool deathMessage)
    {
        RectTransform rect = text.rectTransform;
        Vector3 startScale = new Vector3(0.8f, 0.8f, 0.8f);
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            rect.localScale = Vector3.Lerp(startScale, zoomScale, time / duration);
            yield return null;
        }
        if (deathMessage == true)
        {
            StartCoroutine(RestartLevel(black, silhouetteFadeTime * 2));
            StartCoroutine(FadeImage(img, silhouetteFadeTime, false));
        }
    }

    IEnumerator RestartLevel(Image img, float duration)
    {
        float startAlpha = 0;
        float endAlpha = 1;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            img.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
