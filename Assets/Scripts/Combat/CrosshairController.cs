using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    public Image crosshairImage;
    public Color defaultColor = Color.white;
    public Color enemyColor = Color.red;
    public Color outOfRangeColor = Color.gray;
    
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (crosshairImage == null)
            crosshairImage = GetComponent<Image>();
    }
    
    public void UpdateCrosshair(bool isEnemy, bool inRange)
    {
        if (isEnemy)
        {
            crosshairImage.color = inRange ? enemyColor : outOfRangeColor;
        }
        else
        {
            crosshairImage.color = defaultColor;
        }
    }
    
    void Update()
    {
        // Следим за позицией мыши
        transform.position = Input.mousePosition;
    }
}