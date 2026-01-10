using System;
using UnityEngine;

/// <summary>
/// Ядро логики для отслеживания долгого нажатия (long press).
/// Не зависит от UI и слотов. Переиспользуемый компонент.
/// </summary>
public class LongPressHandler
{
    public event Action OnStarted;
    public event Action<float> OnProgress; // progress: 0..1
    public event Action OnCompleted;
    public event Action OnCanceled;

    private float _duration;
    private float _elapsed;
    private bool _isPressing;
    private bool _completed;

    public LongPressHandler(float duration)
    {
        _duration = duration;
        Reset();
    }

    public void StartPress()
    {
        if (_isPressing) return;
        _isPressing = true;
        _completed = false;
        _elapsed = 0f;
        OnStarted?.Invoke();
    }

    public void CancelPress()
    {
        if (!_isPressing) return;
        _isPressing = false;
        if (!_completed)
            OnCanceled?.Invoke();
        Reset();
    }

    public void Update(float deltaTime)
    {
        if (!_isPressing || _completed) return;
        _elapsed += deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);
        OnProgress?.Invoke(progress);
        if (_elapsed >= _duration)
        {
            _completed = true;
            _isPressing = false;
            OnCompleted?.Invoke();
        }
    }

    public void Reset()
    {
        _elapsed = 0f;
        _isPressing = false;
        _completed = false;
    }
}
