using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraOrientation : MonoBehaviour
{
    private ScreenOrientation lastOrientation;
    private Camera mainCamera;
    private Vector3 initialPosition;

    [SerializeField]
    private Vector3 portraitOffset;

    [SerializeField]
    private Vector3 landscapeOffset;

    [SerializeField]
    private float portraitFOV;

    [SerializeField]
    private float landscapeFOV;

    [SerializeField]
    private float portraitOS;

    [SerializeField]
    private float landscapeOS;

    private void Awake()
    {
        initialPosition = transform.position;
        mainCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        AdjustCamera();
    }

    private void Update()
    {
        AdjustCamera();
    }

    private void AdjustCamera()
    {
        if (Screen.orientation != lastOrientation)
        {
            lastOrientation = Screen.orientation;
            bool isPortrait = Screen.height > Screen.width;
            Vector3 offset = isPortrait ? portraitOffset : landscapeOffset;

            if (mainCamera.orthographic)
            {
               
                mainCamera.orthographicSize = isPortrait ? portraitOS : landscapeOS;
            }
            else
            {
                mainCamera.fieldOfView = isPortrait ? portraitFOV : landscapeFOV;
            }

            transform.position = initialPosition + offset;
        }
    }
}
