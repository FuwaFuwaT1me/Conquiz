using UnityEngine;

namespace Conquiz.UI
{
    /// <summary>
    /// Adjusts RectTransform to respect device safe area (notches, rounded corners, etc.)
    /// Attach to a Panel that acts as the root for all UI elements in a Canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        void Update()
        {
            // Reapply if safe area changes (e.g., orientation change)
            if (_lastSafeArea != Screen.safeArea)
                ApplySafeArea();
        }

        void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;

            // Convert safe area rectangle to normalized anchor coordinates
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}
