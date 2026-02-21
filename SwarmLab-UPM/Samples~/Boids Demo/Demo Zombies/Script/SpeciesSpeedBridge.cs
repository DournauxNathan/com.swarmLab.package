using SwarmLab.Core;
using UnityEngine;
using UnityEngine.UI;

public class SpeciesSpeedBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpeciesDefinition speciesData;
    [SerializeField] private Slider speedSlider;

    [Header("Settings")]
    [SerializeField] private float minSpeed = 0f;
    [SerializeField] private float maxSpeedLimit = 20f;

    private void Awake()
    {
        if (speciesData == null || speedSlider == null)
        {
            Debug.LogError("Missing References on SpeciesSpeedBridge!");
            return;
        }

        // Initialize slider limits
        speedSlider.minValue = minSpeed;
        speedSlider.maxValue = maxSpeedLimit;

        // Initialize slider value to match current SO value
        speedSlider.value = speciesData.maxSpeed;

        // Add listener for UI changes
        speedSlider.onValueChanged.AddListener(HandleSliderChanged);
    }

    private void Update()
    {
        // Sync SO -> Slider (if changed in Inspector or by other scripts)
        if (!Mathf.Approximately(speedSlider.value, speciesData.maxSpeed))
        {
            speedSlider.value = speciesData.maxSpeed;
        }
    }

    private void HandleSliderChanged(float newValue)
    {
        // Sync Slider -> SO
        speciesData.maxSpeed = newValue;
    }

    private void OnDestroy()
    {
        // Clean up listener
        speedSlider.onValueChanged.RemoveListener(HandleSliderChanged);
    }
}