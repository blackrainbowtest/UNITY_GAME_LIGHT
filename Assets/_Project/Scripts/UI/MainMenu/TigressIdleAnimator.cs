using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TigressIdleAnimator : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private IdleAnimation animationData;

    public System.Action OnCycleComplete; // Событие завершения цикла

    private Coroutine _routine;

    void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        if (animationData != null)
            Play(animationData);
    }

    public void Play(IdleAnimation data)
    {
        animationData = data;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(PlayPingPong(animationData));
    }

    private IEnumerator PlayPingPong(IdleAnimation anim)
    {
        if (anim.frames == null || anim.frames.Length == 0 || targetImage == null)
            yield break;

        int i = 0;
        int dir = 1;
        float delay = 1f / Mathf.Max(1f, anim.frameRate);

        while (true)
        {
            targetImage.sprite = anim.frames[i];
            yield return new WaitForSeconds(delay);

            i += dir;

            // Проверяем завершение полного цикла (1 -> N -> 1)
            if (i >= anim.frames.Length - 1)
            {
                dir = -1;
            }
            else if (i <= 0 && dir == -1)
            {
                dir = 1;
                OnCycleComplete?.Invoke(); // Сигнал о завершении цикла
            }
        }
    }
}
