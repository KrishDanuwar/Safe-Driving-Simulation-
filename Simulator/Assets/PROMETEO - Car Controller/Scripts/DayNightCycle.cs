using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Settings")]
    public float dayDurationInMinutes = 2f; 
    public float startDayDelay = 45f; 
    public Light sun;
    
    [Header("Intensity Settings")]
    public float maxIntensity = 1.2f;
    public float minIntensity = 0.05f;

    [Header("Color Settings")]
    public Gradient sunColor; // You can edit this in the Inspector!
    
    private float rotationSpeed;
    private float timer = 0f;

    void Start()
    {
        if (sun == null) sun = GetComponent<Light>();
        
        // Setup a default sunset gradient if none is set
        if (sunColor == null || sunColor.colorKeys.Length <= 1)
        {
            sunColor = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.white, 0.0f);        // Noon
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0.5f); // Sunset (Orange/Yellow)
            colorKeys[2] = new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 1.0f); // Night (Dark Blue)
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
            
            sunColor.SetKeys(colorKeys, alphaKeys);
        }

        transform.rotation = Quaternion.Euler(90, 0, 0);
        sun.intensity = maxIntensity;
        
        rotationSpeed = 360f / (dayDurationInMinutes * 60f);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer > startDayDelay)
        {
            transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);

            float sunAngle = transform.eulerAngles.x;
            
            if (sunAngle > 0 && sunAngle < 180)
            {
                // Calculate factor (0 at noon, 1 at sunset)
                float sunsetFactor = Mathf.Abs(90f - sunAngle) / 90f;
                
                // Set the color and intensity
                sun.color = sunColor.Evaluate(sunsetFactor);
                sun.intensity = Mathf.Lerp(maxIntensity, minIntensity, sunsetFactor);
            }
            else
            {
                sun.intensity = minIntensity;
                sun.color = sunColor.Evaluate(1.0f); // Stay night color
            }
        }
        else
        {
            sun.intensity = maxIntensity;
            sun.color = sunColor.Evaluate(0.0f); // Stay noon white
        }
    }
}
