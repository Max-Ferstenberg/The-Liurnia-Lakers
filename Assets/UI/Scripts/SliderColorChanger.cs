using UnityEngine;
using UnityEngine.UI;

public class SliderColorChanger : MonoBehaviour
{
    [Header("UI References")]
    public Slider slider;      
    public Image fillImage;       
    
    [Header("Color Settings")]
    public Color fullColor = Color.green;      
    public Color emptyColor = Color.green;    
    
    void Start()
    {
        if (slider == null)
            slider = GetComponent<Slider>();
        if (fillImage == null && slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();
        
        UpdateBar(slider.value);
        slider.onValueChanged.AddListener(UpdateFillColor);
    }
    
    public void UpdateBar(float newValue)
    {
        slider.value = newValue;
        fillImage.fillAmount = newValue;
        UpdateFillColor(newValue);
    }
    
    void UpdateFillColor(float value)
    {
        Color newColor = Color.Lerp(emptyColor, fullColor, value);
        newColor.a = 1f;
        fillImage.color = newColor;
    }
}