using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour {
    
    [Header("Scene References")]
    public GameObject pauseMenuPanel;
    
    // Singleton of UiManager
    private static UIManager instance;

    public static UIManager Get()
    {
        if (instance == null)
            instance = (UIManager)FindObjectOfType(typeof(UIManager));

        return instance;
    }
    
    private void Update() {
        
        if (Input.GetKeyDown(KeyCode.Escape) && !pauseMenuPanel.activeSelf) {
            PauseGame();
        } else if (Input.GetKeyDown(KeyCode.Escape) && pauseMenuPanel.activeSelf) {
            ResumeGame();
        }
    }
    
    private void PauseGame() {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }
    
    public void ResumeGame() {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }
    
    public void QuitGame() {
        Application.Quit();
    }
    
  
}