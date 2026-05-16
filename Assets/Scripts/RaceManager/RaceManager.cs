using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class RaceManager : MonoBehaviour
{
    public int   TotalScore    { get; private set; }
    public float Multiplier    { get; private set; } = 1f;
    public int   Gold          => PlayerProgress.Instance.Gold;

    public float comboTier2Time = 5f;    
    public float comboTier3Time = 10f;  
    public float comboTier4Time = 20f;   


    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI comboTimerText;  

    [Header("End Screen")]
    public GameObject endScreenPanel;     
    public TextMeshProUGUI endScoreText;
    public TextMeshProUGUI endGoldText;
    public TextMeshProUGUI endBestText;
    public GameObject pausePanel;

   
    private float _cleanDrivingTimer = 0f;
    private float _prevMultiplier    = 1f;
    private bool  _raceEnded         = false;
    private bool _isPaused = false;

    private int _goldThisRun = 0;

    void Awake()
    {
        if (PlayerProgress.Instance == null)
        {
            var go = new GameObject("PlayerProgress");
            go.AddComponent<PlayerProgress>();
        }

        if (endScreenPanel) endScreenPanel.SetActive(false);
    }

    void Update()
    {
        if (_raceEnded) return;

         if (Input.GetKeyDown(KeyCode.R))
    {
        RestartRace();
        return;
    }
    if (Input.GetKeyDown(KeyCode.M))
    {
        GoToMenu();
        return;
    }
 
  if (Input.GetKeyDown(KeyCode.Escape))
    {
        TogglePause();
        return;
    }
    if (_isPaused) return; 
        
    _cleanDrivingTimer += Time.deltaTime;

        float newMultiplier = _cleanDrivingTimer switch
        {
            >= 20f => 4f,
            >= 10f => 3f,
            >= 5f  => 2f,
            _      => 1f
        };

        if (newMultiplier != _prevMultiplier)
        {
            Multiplier      = newMultiplier;
            _prevMultiplier = newMultiplier;
            UpdateUI();  
        }

        UpdateComboTimer();
    }

    public void AddCheckpointPoints(float bonusBase = 500f)
    {
        int points = Mathf.RoundToInt(bonusBase * Multiplier);
        TotalScore += points;


        Debug.Log($"[Race] Checkpoint! +{points} pkt (x{Multiplier})");
        UpdateUI();
    }

    public void AddGold(int amount)
    {
        int bonusGold = Mathf.RoundToInt(amount * Multiplier);  
        _goldThisRun += bonusGold;
        PlayerProgress.Instance.AddGold(bonusGold);
        UpdateUI();
    }

    
    public void ResetMultiplier()
    {
        Debug.Log($"[Race] ResetMultiplier called, current multiplier: {Multiplier}");
        if (Multiplier <= 1f) return;   
        _cleanDrivingTimer = 0f;
        Multiplier         = 1f;
        _prevMultiplier    = 1f;
        UpdateUI();
        Debug.Log("[Race] Combo reset — you are off track!");
    }
    void TogglePause()
{
    _isPaused = !_isPaused;
    Time.timeScale = _isPaused ? 0f : 1f;
    if (pausePanel) pausePanel.SetActive(_isPaused);
}

    public void EndRace()
    {
        if (_raceEnded) return;
        _raceEnded = true;

        PlayerProgress.Instance.SubmitScore(TotalScore);

        if (endScreenPanel)
        {
            endScreenPanel.SetActive(true);
            if (endScoreText) endScoreText.text = $"SCORE: {TotalScore}";
            if (endGoldText)  endGoldText.text  = $"+{_goldThisRun}";
            if (endBestText)
            {
                bool isNew = TotalScore >= PlayerProgress.Instance.BestScore;
                endBestText.text  = isNew
                    ? "NEW RECORD!"
                    : $"RECORD: {PlayerProgress.Instance.BestScore}";
                endBestText.color = isNew ? Color.yellow : Color.white;
            }
        }
    }


    public void GoToShop()   => UnityEngine.SceneManagement.SceneManager.LoadScene("CarShop");
    public void RestartRace(){
    Time.timeScale = 1f; 
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);}
    public void GoToMenu()   => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");

    
  void UpdateUI()
    {
        if (scoreText)      
        {

            scoreText.text = $"<size=12>SCORE</size>\n<size=26><b>{TotalScore:N0}</b></size>";
        }
        
        if (multiplierText) 
        {

            multiplierText.text = $"<size=12>COMBO</size>\n<size=26><b>x{Multiplier:F0}</b></size>";
        }
        
       if (goldText) 
        {
            goldText.text = $"<size=12>GOLD</size>\n<size=26><b>{Gold:N0}</b></size>";
        }
    }

    void UpdateComboTimer()
    {
        if (comboTimerText == null) return;
        if (_cleanDrivingTimer < comboTier2Time)
        {
            float t = _cleanDrivingTimer / comboTier2Time;
            comboTimerText.text = $"x2 for {comboTier2Time - _cleanDrivingTimer:F1}s";
        }
        else if (_cleanDrivingTimer < comboTier3Time)
        {
            comboTimerText.text = $"x3 for {comboTier3Time - _cleanDrivingTimer:F1}s";
        }
        else if (_cleanDrivingTimer < comboTier4Time)
        {
            comboTimerText.text = $"x4 for {comboTier4Time - _cleanDrivingTimer:F1}s";
        }
        else
        {
            comboTimerText.text = "MAX COMBO ★";
        }
    }
}