using System.Collections;
using UnityEngine;

public class TrafficController : MonoBehaviour
{
    [Header("Light Objects")]
    public GameObject redLight;
    public GameObject greenLight;

    [Header("Settings")]
    public float redDuration = 5f;
    public float greenDuration = 5f;

    protected bool isRed = true;

    void Start()
    {
        // Randomly choose the initial state when the game is played
        isRed = Random.Range(0, 2) == 0;

        // Apply the initial state
        UpdateLights();

        // Start the automatic cycle
        StartCoroutine(CycleLights());
    }

    IEnumerator CycleLights()
    {
        while (true)
        {
            yield return new WaitForSeconds(isRed ? redDuration : greenDuration);
            isRed = !isRed;
            UpdateLights();
        }
    }

    protected virtual void UpdateLights()
    {
        if (redLight != null) redLight.SetActive(isRed);
        if (greenLight != null) greenLight.SetActive(!isRed);
    }

    public bool IsRed()
    {
        return isRed;
    }
}
