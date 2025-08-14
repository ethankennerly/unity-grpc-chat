using UnityEngine;
using UnityEngine.UI;

namespace MinimalChat
{
    /// <summary>
    /// Delays scroll-to-bottom by three Update frames, then snaps once.
    /// Includes optional diagnostics (off by default) to trace timing and sizing issues.
    /// </summary>
    public sealed class ChatAutoScroller : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;

        /// <summary>
        /// When true, logs one summary per request and important anomalies.
        /// </summary>
        [SerializeField] private bool _diagnostics;

        // Counts down Update frames before snapping.
        private int _framesUntilSnap;

        // Tracks content and viewport geometry for diagnostics.
        private float _reqContentHeight;
        private float _reqViewportHeight;
        private float _reqNormalizedPos;

        // Diagnostics book-keeping.
        private int _reqId;
        private int _reqFrame;
        private int _snapFrame;

        /// <summary>
        /// Request a bottom snap. Safe to call multiple times per frame.
        /// </summary>
        public void RequestScrollToBottom()
        {
            if (_scrollRect == null)
            {
                return;
            }

            if (_framesUntilSnap > 0 && _diagnostics)
            {
                Debug.LogWarning(
                    "[AutoScroll][diag] New request overwrote pending snap. " +
                    "This may indicate Update order races."
                );
            }

            _framesUntilSnap = 3;

            var content = _scrollRect.content;
            var viewport = _scrollRect.viewport != null
                ? _scrollRect.viewport
                : (_scrollRect.transform as RectTransform);

            _reqContentHeight = content != null ? content.rect.height : -1f;
            _reqViewportHeight = viewport != null ? viewport.rect.height : -1f;
            _reqNormalizedPos = _scrollRect.verticalNormalizedPosition;

            _reqId = _reqId + 1;
            _reqFrame = Time.frameCount;
            _snapFrame = -1;

            if (_diagnostics)
            {
                Debug.Log(
                    "[AutoScroll][diag] Request id=" + _reqId +
                    " at frame=" + _reqFrame +
                    " contentH=" + _reqContentHeight.ToString("F1") +
                    " viewH=" + _reqViewportHeight.ToString("F1") +
                    " normPos=" + _reqNormalizedPos.ToString("F3")
                );
            }
        }

        private void OnDisable()
        {
            _framesUntilSnap = 0;
        }

        private void Update()
        {
            if (_scrollRect == null)
            {
                return;
            }

            if (_framesUntilSnap <= 0)
            {
                return;
            }

            _framesUntilSnap = _framesUntilSnap - 1;

            if (_framesUntilSnap > 0)
            {
                return;
            }

            var beforePos = _scrollRect.verticalNormalizedPosition;

            _scrollRect.verticalNormalizedPosition = 0f;

            var afterPos = _scrollRect.verticalNormalizedPosition;
            _snapFrame = Time.frameCount;

            if (_diagnostics)
            {
                var content = _scrollRect.content;
                var viewport = _scrollRect.viewport != null
                    ? _scrollRect.viewport
                    : (_scrollRect.transform as RectTransform);

                var curContentH = content != null ? content.rect.height : -1f;
                var curViewportH = viewport != null ? viewport.rect.height : -1f;

                var contentDelta = curContentH - _reqContentHeight;
                var viewDelta = curViewportH - _reqViewportHeight;

                Debug.Log(
                    "[AutoScroll][diag] Snap id=" + _reqId +
                    " reqFrame=" + _reqFrame +
                    " snapFrame=" + _snapFrame +
                    " framesDelta=" + (_snapFrame - _reqFrame) +
                    " contentH " + _reqContentHeight.ToString("F1") +
                    "→" + curContentH.ToString("F1") +
                    " (Δ=" + contentDelta.ToString("F1") + ")" +
                    " viewH " + _reqViewportHeight.ToString("F1") +
                    "→" + curViewportH.ToString("F1") +
                    " (Δ=" + viewDelta.ToString("F1") + ")" +
                    " normPos " + beforePos.ToString("F3") +
                    "→" + afterPos.ToString("F3")
                );

                if (curContentH <= curViewportH && curContentH >= 0f && curViewportH >= 0f)
                {
                    Debug.LogWarning(
                        "[AutoScroll][diag] Content not larger than viewport at snap; " +
                        "scrolling may be a no-op (nothing to scroll)."
                    );
                }

                if (Mathf.Approximately(contentDelta, 0f) &&
                    Mathf.Approximately(viewDelta, 0f))
                {
                    Debug.LogWarning(
                        "[AutoScroll][diag] Geometry unchanged across delay. " +
                        "If the text actually grew, a different script execution order, " +
                        "layout, or the adapter may be updating after this snap."
                    );
                }
            }
        }
    }
}