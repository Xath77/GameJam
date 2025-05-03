using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JShake : MonoBehaviour
{
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 5f;
    public AudioSource shakeSource;

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        Shake();
        PlaySound();
    }

    public void Shake()
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    public void PlaySound()
    {
        if (shakeSource != null && !shakeSource.isPlaying)
            shakeSource.Play();
    }

    private System.Collections.IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            rectTransform.anchoredPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
    }
}
