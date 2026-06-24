using System;
using PrimeTween;
using HelloDev.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace HelloDev.UI.Tweening
{
    /// <summary>
    /// ITweenProvider implementation using PrimeTween.
    /// </summary>
    public class PrimeTweenProvider : ITweenProvider
    {
        #region Transform Tweens

        public ITweenHandle Scale(Transform target, Vector3 endValue, float duration, float delay = 0)
        {
            var tween = Tween.Scale(target, endValue, duration, startDelay: delay);
            return new PrimeTweenHandle(tween);
        }

        public ITweenHandle Scale(Transform target, float endValue, float duration, float delay = 0)
        {
            var tween = Tween.Scale(target, endValue, duration, startDelay: delay);
            return new PrimeTweenHandle(tween);
        }

        #endregion

        #region Graphic/UI Tweens

        public ITweenHandle Fade(Graphic target, float endValue, float duration, float delay = 0)
        {
            var tween = Tween.Alpha(target, endValue, duration, startDelay: delay);
            return new PrimeTweenHandle(tween);
        }

        public ITweenHandle Fade(CanvasGroup target, float endValue, float duration, float delay = 0)
        {
            var tween = Tween.Alpha(target, endValue, duration, startDelay: delay);
            return new PrimeTweenHandle(tween);
        }

        public ITweenHandle FillAmount(Image target, float endValue, float duration, float delay = 0)
        {
            var tween = Tween.UIFillAmount(target, endValue, duration, startDelay: delay);
            return new PrimeTweenHandle(tween);
        }

        #endregion

        #region Kill Tweens

        public void Kill(Transform target)
        {
            Tween.StopAll(target);
        }

        public void Kill(Component target)
        {
            Tween.StopAll(target);
        }

        public void KillAll()
        {
            Tween.StopAll();
        }

        #endregion
    }

    /// <summary>
    /// ITweenHandle implementation wrapping a PrimeTween Tween.
    /// </summary>
    internal class PrimeTweenHandle : ITweenHandle
    {
        private Tween _tween;
        private Ease _ease = Ease.Default;
        private bool _useUnscaledTime = false;
        private Action _onComplete = null;

        public PrimeTweenHandle(Tween tween)
        {
            _tween = tween;
        }

        public ITweenHandle From(float value)
        {
            // PrimeTween doesn't have a direct From method like DOTween
            // The tween is already configured with start/end values
            return this;
        }

        public ITweenHandle SetEase(EaseType ease)
        {
            _ease = ConvertEase(ease);
            // PrimeTween sets ease at creation time, but we can still apply it
            // For now, store the ease for potential use
            return this;
        }

        public ITweenHandle OnComplete(Action callback)
        {
            _onComplete = callback;
            _tween.OnComplete(callback);
            return this;
        }

        public ITweenHandle SetUpdate(bool useUnscaledTime)
        {
            // Note: PrimeTween requires useUnscaledTime at creation time, not after.
            // This is stored but cannot be applied to an already-created tween.
            // For unscaled time support, the ITweenProvider interface would need redesigning.
            _useUnscaledTime = useUnscaledTime;
            return this;
        }
        
        public void Kill()
        {
            if (_tween.isAlive)
            {
                _tween.Stop();
            }
        }

        private static Ease ConvertEase(EaseType ease)
        {
            return ease switch
            {
                EaseType.Linear => Ease.Linear,
                EaseType.InQuad => Ease.InQuad,
                EaseType.OutQuad => Ease.OutQuad,
                EaseType.InOutQuad => Ease.InOutQuad,
                EaseType.InCubic => Ease.InCubic,
                EaseType.OutCubic => Ease.OutCubic,
                EaseType.InOutCubic => Ease.InOutCubic,
                EaseType.InQuart => Ease.InQuart,
                EaseType.OutQuart => Ease.OutQuart,
                EaseType.InOutQuart => Ease.InOutQuart,
                EaseType.InQuint => Ease.InQuint,
                EaseType.OutQuint => Ease.OutQuint,
                EaseType.InOutQuint => Ease.InOutQuint,
                EaseType.InSine => Ease.InSine,
                EaseType.OutSine => Ease.OutSine,
                EaseType.InOutSine => Ease.InOutSine,
                EaseType.InExpo => Ease.InExpo,
                EaseType.OutExpo => Ease.OutExpo,
                EaseType.InOutExpo => Ease.InOutExpo,
                EaseType.InCirc => Ease.InCirc,
                EaseType.OutCirc => Ease.OutCirc,
                EaseType.InOutCirc => Ease.InOutCirc,
                EaseType.InElastic => Ease.InElastic,
                EaseType.OutElastic => Ease.OutElastic,
                EaseType.InOutElastic => Ease.InOutElastic,
                EaseType.InBack => Ease.InBack,
                EaseType.OutBack => Ease.OutBack,
                EaseType.InOutBack => Ease.InOutBack,
                EaseType.InBounce => Ease.InBounce,
                EaseType.OutBounce => Ease.OutBounce,
                EaseType.InOutBounce => Ease.InOutBounce,
                _ => Ease.Linear
            };
        }
    }
}
