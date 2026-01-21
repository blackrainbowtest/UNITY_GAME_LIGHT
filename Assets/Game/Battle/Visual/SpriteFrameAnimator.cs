using System;
using UnityEngine;

namespace Game.Battle.Visual
{
    public sealed class SpriteFrameAnimator : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private SpriteRenderer target;

        [Header("Frames")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 8f;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool playOnEnable = true;

        private float accumulator;
        private int index;
        private bool isPlaying;
        private Action onFinished;

        private void Reset()
        {
            target = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (playOnEnable)
                PlayLoop(frames);
        }

        private void Update()
        {
            if (!isPlaying)
                return;

            if (frames == null || frames.Length == 0)
                return;

            var fps = Mathf.Max(0.01f, framesPerSecond);
            accumulator += Time.deltaTime;

            var frameDuration = 1f / fps;
            while (accumulator >= frameDuration)
            {
                accumulator -= frameDuration;
                index++;

                if (index >= frames.Length)
                {
                    if (loop)
                    {
                        index = 0;
                    }
                    else
                    {
                        index = frames.Length - 1;
                        ApplyFrame(index);
                        StopInternal(invokeFinished: true);
                        return;
                    }
                }

                ApplyFrame(index);
            }
        }

        public void SetTarget(SpriteRenderer spriteRenderer)
        {
            target = spriteRenderer;
        }

        public void PlayLoop(Sprite[] newFrames)
        {
            PlayInternal(newFrames, shouldLoop: true, finished: null);
        }

        public void PlayOnce(Sprite[] newFrames, Action finished = null)
        {
            PlayInternal(newFrames, shouldLoop: false, finished);
        }

        public void Stop()
        {
            StopInternal(invokeFinished: false);
        }

        private void PlayInternal(Sprite[] newFrames, bool shouldLoop, Action finished)
        {
            frames = newFrames;
            loop = shouldLoop;
            onFinished = finished;

            accumulator = 0f;
            index = 0;
            isPlaying = true;

            ApplyFrame(index);
        }

        private void StopInternal(bool invokeFinished)
        {
            isPlaying = false;
            accumulator = 0f;

            if (invokeFinished)
            {
                var cb = onFinished;
                onFinished = null;
                cb?.Invoke();
            }
            else
            {
                onFinished = null;
            }
        }

        private void ApplyFrame(int i)
        {
            if (target == null)
                return;

            if (frames == null || frames.Length == 0)
                return;

            target.sprite = frames[Mathf.Clamp(i, 0, frames.Length - 1)];
        }
    }
}
