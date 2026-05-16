using UnityEngine;
public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress Instance { get; private set; }

    public int Gold { get; private set; }


    public string SelectedCarId { get; private set; }

    public int BestScore { get; private set; }

    const string KEY_GOLD       = "PlayerGold";
    const string KEY_CAR        = "SelectedCar";
    const string KEY_BESTSCORE  = "BestScore";
    const string KEY_UNLOCKED   = "Unlocked_";  
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    void Load()
    {
        Gold          = PlayerPrefs.GetInt(KEY_GOLD, 0);
        SelectedCarId = PlayerPrefs.GetString(KEY_CAR, "car_default");
        BestScore     = PlayerPrefs.GetInt(KEY_BESTSCORE, 0);
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        PlayerPrefs.SetInt(KEY_GOLD, Gold);
        PlayerPrefs.Save();
        OnGoldChanged?.Invoke(Gold);
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        PlayerPrefs.SetInt(KEY_GOLD, Gold);
        PlayerPrefs.Save();
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    public bool IsCarUnlocked(string carId)
    {
        return PlayerPrefs.GetInt(KEY_UNLOCKED + carId, 0) == 1;
    }

    public void UnlockCar(string carId)
    {
        PlayerPrefs.SetInt(KEY_UNLOCKED + carId, 1);
        PlayerPrefs.Save();
        OnCarUnlocked?.Invoke(carId);
    }

    public void SelectCar(string carId)
    {
        SelectedCarId = carId;
        PlayerPrefs.SetString(KEY_CAR, carId);
        PlayerPrefs.Save();
    }

    public void SubmitScore(int score)
    {
        if (score > BestScore)
        {
            BestScore = score;
            PlayerPrefs.SetInt(KEY_BESTSCORE, BestScore);
            PlayerPrefs.Save();
        }
    }

    public System.Action<int>    OnGoldChanged;
    public System.Action<string> OnCarUnlocked;

    [ContextMenu("Reset All Progress")]
    public void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        Load();
        Debug.Log("[PlayerProgress] All progress reset!");
    }
}
