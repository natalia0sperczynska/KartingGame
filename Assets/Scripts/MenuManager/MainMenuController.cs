using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour{
    public string raceSceneName = "MainScene"; 
    public string shopSceneName = "CarShop";

    public void PlayGame()
    {
        SceneManager.LoadScene(raceSceneName);
    }

    public void OpenShop()
    {
        SceneManager.LoadScene(shopSceneName);
    }

    public void QuitGame()
    {   
        Debug.Log("Player Quit Game");
        Application.Quit(); 
    }
}
