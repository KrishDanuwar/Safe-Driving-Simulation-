using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuCanvas;
    public GameObject gameOverCanvas;
    public TMP_Text scoreText;
    public TMP_Text violationText;
    public TMP_Text speedometerText;

    [Header("Player References")]
    public PrometeoCarController playerCar;
    
    [Header("Simulation Settings")]
    public int drivingScore = 100;
    public bool isGameActive = false;

    [Header("Speeding Settings")]
    public float speedLimit = 50f;
    public float speedingThresholdTime = 2.0f; 
    private float speedingTimer = 0f;

    private float violationDisplayTimer = 0f;
    private float violationDuration = 3.0f;

    private void Start()
    {
        // Setup initial state
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
            
            // Disable the panel's raycast target so it doesn't block the button
            Image panelImage = menuCanvas.GetComponentInChildren<Image>();
            if (panelImage != null && panelImage.gameObject.name == "Panel")
            {
                panelImage.raycastTarget = false;
            }
        }

        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
        if (playerCar != null) playerCar.enabled = false;
        if (violationText != null) violationText.text = "";
        
        UpdateScoreUI();
    }

    private float smoothedSpeed = 0f;

    private void Update()
    {
        // Force cursor to be visible while menu is open
        if (!isGameActive)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        UpdateSpeedometer();
        CheckSpeeding();
        HandleViolationTimer();
    }

    void UpdateSpeedometer()
    {
        if (speedometerText != null && playerCar != null)
        {
            Rigidbody rb = playerCar.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Convert velocity to KM/H (m/s * 3.6)
                float currentSpeed = rb.linearVelocity.magnitude * 3.6f;
                // Smooth the speed so it doesn't flicker
                smoothedSpeed = Mathf.Lerp(smoothedSpeed, currentSpeed, Time.deltaTime * 5f);
                
                speedometerText.text = Mathf.RoundToInt(smoothedSpeed).ToString() + " KM/H";
                
                // Turn red if speeding
                speedometerText.color = (smoothedSpeed > speedLimit) ? Color.red : Color.white;
            }
        }
    }

    void CheckSpeeding()
    {
        if (playerCar == null) return;

        if (smoothedSpeed > speedLimit)
        {
            speedingTimer += Time.deltaTime;
            if (speedingTimer >= speedingThresholdTime)
            {
                AddPenalty(10, "Speed Limit Violation!");
                speedingTimer = 0f; 
            }
        }
        else
        {
            speedingTimer = 0f; 
        }
    }

    public void StartGame()
    {
        isGameActive = true;
        
        // Hide menu
        if (menuCanvas != null) menuCanvas.SetActive(false);
        
        // Enable car controls
        if (playerCar != null) playerCar.enabled = true;

        // Hide cursor for driving
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("Simulation Started!");
    }

    private float lastPenaltyTime = 0f;
    private float penaltyCooldown = 2.0f;

    // This function can be called by other scripts to penalize the player
    public void AddPenalty(int points, string reason)
    {
        if (!isGameActive || Time.time < lastPenaltyTime + penaltyCooldown) return;

        lastPenaltyTime = Time.time;
        drivingScore -= points;
        if (drivingScore < 0) drivingScore = 0;

        Debug.Log("PENALTY: -" + points + " Points for " + reason);
        
        UpdateScoreUI();
        ShowViolationWarning(reason);

        if (drivingScore <= 0)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        isGameActive = false;
        if (gameOverCanvas != null) gameOverCanvas.SetActive(true);
        if (playerCar != null) playerCar.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        Debug.Log("GAME OVER: Score Reached Zero!");
    }

    public void RestartGame()
    {
        // Reloads the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Safe Driving Score: " + drivingScore;
        }
    }

    void ShowViolationWarning(string message)
    {
        if (violationText != null)
        {
            violationText.text = "!!! " + message.ToUpper() + " !!!";
            violationDisplayTimer = violationDuration;
        }
    }

    void HandleViolationTimer()
    {
        if (violationDisplayTimer > 0)
        {
            violationDisplayTimer -= Time.deltaTime;
            if (violationDisplayTimer <= 0)
            {
                if (violationText != null) violationText.text = "";
            }
        }
    }
}
