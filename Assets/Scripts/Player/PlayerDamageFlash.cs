using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerDamageFlash : MonoBehaviour
{
    [SerializeField] private Image flashImage;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color flashColor = new Color(1, 0, 0, 0.4f); // đỏ trong suốt

    private Coroutine flashRoutine;

    private void Awake()
    {
        if (flashImage != null)
            flashImage.color = Color.clear;
    }

    public void Flash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        flashImage.color = flashColor;

        float timer = 0f;
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            flashImage.color = Color.Lerp(flashColor, Color.clear, timer / flashDuration);
            yield return null;
        }

        flashImage.color = Color.clear;
    }
}
