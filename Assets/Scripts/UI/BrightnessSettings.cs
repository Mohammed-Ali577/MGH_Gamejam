using UnityEngine;
using UnityEngine.UI;

public class BrightnessSettings : MonoBehaviour
{
    public Slider brightnessSlider;
    public Image brightnessOverlay;

    private void Start()
    {
        float brightness = PlayerPrefs.GetFloat("Brightness", 1f);

        brightnessSlider.value = brightness;
        SetBrightness(brightness);
    }
    public void SetBrightness(float brightness)
    {
        Debug.Log("Brightness: " + brightness);

        Color color = brightnessOverlay.color;
        color.a = 1f - brightness;
        brightnessOverlay.color = color;

        PlayerPrefs.SetFloat("Brightness", brightness);
    }
}