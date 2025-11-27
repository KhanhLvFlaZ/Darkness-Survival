using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LocalLightControl : MonoBehaviour
{
    [SerializeField] Light2D globalLight;
    [SerializeField] float transitionSpeed = 2f;
    Light2D spotLight;

    float maxIntensity;        // Maximum intensity for the spot light
    [SerializeField] float minIntensity = 0f;   // Minimum intensity for the spot light

    private void Awake()
    {
        spotLight = GetComponent<Light2D>();
        if (spotLight == null)
        {
            Debug.LogError("LocalLightControl requires a Light2D component on the same GameObject.", this);
            enabled = false;
            return;
        }

        maxIntensity = spotLight.intensity;
    }

    private void Start()
    {
        if (!enabled)
        {
            return;
        }

        if(globalLight == null)
        {
            GameObject globalLightObj = GameObject.FindGameObjectWithTag("GlobalLight");
            if (globalLightObj != null)
            {
                globalLight = globalLightObj.GetComponent<Light2D>();
                if (globalLight == null)
                {
                    Debug.LogWarning("LocalLightControl found 'GlobalLight' object but it lacks a Light2D component.", globalLightObj);
                }
            }
            else
            {
                Debug.LogWarning("LocalLightControl could not find an object tagged 'GlobalLight'.", this);
            }
        }

        if (globalLight == null)
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (!enabled)
        {
            return;
        }

        AdjustSpotLightIntensity(globalLight.intensity);
    }

    void AdjustSpotLightIntensity(float globalIntensity)
    {
        // Map the global intensity to the spot light's intensity range

        float mappedIntensity = Map(globalIntensity, 0f, 0.75f, maxIntensity, minIntensity);
        spotLight.intensity = Mathf.Lerp(spotLight.intensity, mappedIntensity, Time.deltaTime * transitionSpeed);
    }

    float Map(float value, float fromSource, float toSource, float fromTarget, float toTarget)
    {
        return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
    }
}