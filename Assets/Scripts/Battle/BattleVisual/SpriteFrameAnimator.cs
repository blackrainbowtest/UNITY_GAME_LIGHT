//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\Visual\SpriteFrameAnimator.cs                                              */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:43:51 by UDA                                                                    */
/*   Updated: 2026/01/23 01:43:51 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using System;
using UnityEngine;

namespace Game.Battle.Visual
{
    public sealed class SpriteFrameAnimator : MonoBehaviour
    {
        public event Action OnLooped;

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

        private int impactFrameIndex0Based;
        private bool impactInvoked;
        private Action onImpact;

        public bool IsPlaying => isPlaying;
        public bool IsLooping => isPlaying && loop;

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
                        OnLooped?.Invoke();
                    }
                    else
                    {
                        index = frames.Length - 1;
                        ApplyFrame(index);
                        TryInvokeImpact();
                        StopInternal(invokeFinished: true);
                        return;
                    }
                }

                TryInvokeImpact();
                ApplyFrame(index);
            }
        }

        public void SetTarget(SpriteRenderer spriteRenderer)
        {
            target = spriteRenderer;
        }

        public void SetFramesPerSecond(float fps)
        {
            framesPerSecond = Mathf.Max(0.01f, fps);
        }

        public void PlayLoop(Sprite[] newFrames)
        {
            PlayInternal(newFrames, shouldLoop: true, finished: null, impactFrameIndex: -1, impact: null);
        }

        public void PlayOnce(Sprite[] newFrames, Action finished = null)
        {
            PlayInternal(newFrames, shouldLoop: false, finished, impactFrameIndex: -1, impact: null);
        }

        public void PlayOnce(Sprite[] newFrames, Action finished = null, int impactFrameIndex = -1, Action onImpact = null)
        {
            PlayInternal(newFrames, shouldLoop: false, finished, impactFrameIndex, onImpact);
        }

        public void Stop()
        {
            StopInternal(invokeFinished: false);
        }

        private void PlayInternal(Sprite[] newFrames, bool shouldLoop, Action finished, int impactFrameIndex, Action impact)
        {
            frames = newFrames;
            loop = shouldLoop;
            onFinished = finished;

            // Impact is only supported for one-shot playback.
            this.onImpact = shouldLoop ? null : impact;
            impactInvoked = false;
            impactFrameIndex0Based = (!shouldLoop && impactFrameIndex > 0) ? impactFrameIndex - 1 : -1;

            accumulator = 0f;
            index = 0;
            isPlaying = true;

            ApplyFrame(index);

            // If impact is on the first frame, trigger immediately.
            TryInvokeImpact();
        }

        private void StopInternal(bool invokeFinished)
        {
            isPlaying = false;
            accumulator = 0f;

            onImpact = null;
            impactInvoked = false;
            impactFrameIndex0Based = -1;

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

        private void TryInvokeImpact()
        {
            if (impactInvoked)
                return;

            if (impactFrameIndex0Based < 0)
                return;

            if (index < impactFrameIndex0Based)
                return;

            impactInvoked = true;
            var cb = onImpact;
            onImpact = null;
            cb?.Invoke();
        }
    }
}
