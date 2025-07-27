using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaController : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private ScreenOrientation lastOrientation;

    [SerializeField]
    private Vector2 portraitOffset;

    [SerializeField]
    private Vector2 landscapeOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        if (Screen.safeArea != lastSafeArea || Screen.orientation != lastOrientation)
        {
            Rect safeArea = Screen.safeArea;
            Vector2 offset = Screen.height > Screen.width ? portraitOffset : landscapeOffset;
            // Convert safe area rectangle to anchor coordinates
            Vector2 anchorMin = safeArea.position + offset;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            lastSafeArea = safeArea;
            lastOrientation = Screen.orientation;
        }
    }
}
