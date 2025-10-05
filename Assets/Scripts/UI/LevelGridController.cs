using UnityEngine;
using UnityEngine.UI;

public class LevelGridController : MonoBehaviour
{
    private ScreenOrientation lastOrientation;

    [SerializeField]
    private int portraitColumnCount;

    [SerializeField]
    private int landscapeColumnCount;

    [SerializeField]
    private int portraitSpacing;

    [SerializeField]
    private int landscapeSpacing;

    [SerializeField]
    private GridLayoutGroup grid;

    private void Start()
    {
        UpdateGrid();
    }

    private void Update()
    {
        UpdateGrid();
    }

    private void UpdateGrid()
    {
        if (Screen.orientation != lastOrientation)
        {
            if (Screen.height > Screen.width) // Portrait
            {
                grid.constraintCount = portraitColumnCount;
                grid.spacing = new Vector2(portraitSpacing, portraitSpacing);
            }
            else // Landscape
            {
                grid.constraintCount = landscapeColumnCount;
                grid.spacing = new Vector2(landscapeSpacing, landscapeSpacing);
            }

            lastOrientation = Screen.orientation;
        }
    }
}

