using UnityEngine;
using UnityEngine.UI;

public class TotemPickupUI : MonoBehaviour
{
    [Header("UI Components")]
    public Slider pickupSlider;
    public Text pickupText;
    public CanvasGroup canvasGroup;
    
    [Header("Settings")]
    public float fadeSpeed = 5f;
    public float showDuration = 2f;
    
    private bool isShowing = false;
    private float showTimer = 0f;
    private float targetAlpha = 0f;
    
    private void Awake()
    {
        // Автоматически находим компоненты если не назначены
        if (pickupSlider == null)
            pickupSlider = GetComponentInChildren<Slider>();
        
        if (pickupText == null)
        {
            Text[] texts = GetComponentsInChildren<Text>();
            if (texts.Length > 0)
                pickupText = texts[0];
        }
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
    
    private void Update()
    {
        // Плавное появление/исчезновение
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
        
        // Таймер показа
        if (isShowing)
        {
            showTimer += Time.deltaTime;
            if (showTimer >= showDuration)
            {
                Hide();
            }
        }
    }
    
    public void Show()
    {
        isShowing = true;
        showTimer = 0f;
        targetAlpha = 1f;
        
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
        }
    }
    
    public void Hide()
    {
        isShowing = false;
        targetAlpha = 0f;
        
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
    
    public void UpdateProgress(float progress, float timeRemaining)
    {
        if (pickupSlider != null)
        {
            pickupSlider.value = Mathf.Clamp01(progress);
            
            // Обновляем текст
            if (pickupText != null)
            {
                if (progress <= 0f)
                {
                    pickupText.text = "Нажмите E для подбора";
                }
                else if (progress >= 1f)
                {
                    pickupText.text = "Подобран!";
                }
                else
                {
                    pickupText.text = $"Подбор... {Mathf.RoundToInt(progress * 100)}%";
                }
            }
        }
    }
    
    public void ResetProgress()
    {
        if (pickupSlider != null)
        {
            pickupSlider.value = 0f;
        }
        
        if (pickupText != null)
        {
            pickupText.text = "Нажмите E для подбора";
        }
    }
    
    public bool IsVisible()
    {
        return canvasGroup != null && canvasGroup.alpha > 0.1f;
    }
}