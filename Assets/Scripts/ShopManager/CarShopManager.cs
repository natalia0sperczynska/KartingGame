using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CarShopManager : MonoBehaviour
{
    [Header("Data")]
    public CarDatabase database;

    [Header("UI — header")]
    public TextMeshProUGUI goldText;

    [Header("UI — car grid")]
    public Transform carGridContent;     
    public GameObject carCardPrefab;    

    [Header("UI — detail panel")]
    public Image              detailPreview;
    public TextMeshProUGUI    detailName;
    public TextMeshProUGUI    detailDesc;
    public TextMeshProUGUI    detailPrice;
    public Button             actionButton;
    public TextMeshProUGUI    actionButtonText;

    [Header("UI — stats bars")]
    public Slider speedBar;
    public Slider accelBar;
    public Slider handlingBar;
    public Slider brakingBar;

    [Header("UI — toast message (optional)")]
    public GameObject toastPanel;
    public TextMeshProUGUI toastText;


    private CarData   _selectedCar;
    private CarCard[] _cards;
    void Start()
    {
        if (PlayerProgress.Instance == null)
        {
            var go = new GameObject("PlayerProgress");
            go.AddComponent<PlayerProgress>();
        }

        PlayerProgress.Instance.OnGoldChanged  += _ => RefreshGoldText();
        PlayerProgress.Instance.OnCarUnlocked  += _ => RefreshCards();

        BuildGrid();
        RefreshGoldText();

        string selectedId = PlayerProgress.Instance.SelectedCarId;
        CarData def = database.GetById(selectedId) ?? database.cars[0];
        ShowDetail(def);
    }

    void OnDestroy()
    {
        if (PlayerProgress.Instance == null) return;
        PlayerProgress.Instance.OnGoldChanged  -= _ => RefreshGoldText();
        PlayerProgress.Instance.OnCarUnlocked  -= _ => RefreshCards();
    }

    void BuildGrid()
    {

        foreach (Transform child in carGridContent)
            Destroy(child.gameObject);

        _cards = new CarCard[database.cars.Length];

        for (int i = 0; i < database.cars.Length; i++)
        {
            CarData car  = database.cars[i];
            var     go   = Instantiate(carCardPrefab, carGridContent);
            var     card = go.GetComponent<CarCard>();

            card.Setup(car, this);
            _cards[i] = card;
        }
    }

    void RefreshCards()
    {
        if (_cards == null) return;
        foreach (var c in _cards) c.Refresh();
    }

    public void ShowDetail(CarData car)
    {
        _selectedCar = car;

        if (detailPreview) detailPreview.sprite = car.previewSprite;
        if (detailName)    detailName.text      = car.displayName;
        if (detailDesc)    detailDesc.text      = car.description;

        SetBar(speedBar,    car.speed);
        SetBar(accelBar,    car.acceleration);
        SetBar(handlingBar, car.handling);
        SetBar(brakingBar,  car.braking);

        RefreshActionButton();
    }

    void SetBar(Slider bar, int value)
    {
        if (bar == null) return;
        bar.minValue = 0;
        bar.maxValue = 10;
        bar.value    = value;
    }

    void RefreshActionButton()
    {
        if (_selectedCar == null || actionButton == null) return;

        bool owned    = _selectedCar.unlockedByDefault
                        || PlayerProgress.Instance.IsCarUnlocked(_selectedCar.carId);
        bool selected = PlayerProgress.Instance.SelectedCarId == _selectedCar.carId;
        bool canAfford= PlayerProgress.Instance.Gold >= _selectedCar.price;

        if (selected && owned)
        {
            actionButtonText.text  = "CURRENT";
            actionButton.interactable = false;
        }
        else if (owned)
        {
            actionButtonText.text  = "CHOOSE";
            actionButton.interactable = true;
        }
        else
        {
            actionButtonText.text  = canAfford
                ? $"BUY {_selectedCar.price}"
                : $"LOCKED {_selectedCar.price}";
            actionButton.interactable = canAfford;
        }

        if (detailPrice)
            detailPrice.text = owned ? "UNLOCKED" : $" {_selectedCar.price}";
    }

    public void OnActionButtonClicked()
    {
        if (_selectedCar == null) return;

        bool owned = _selectedCar.unlockedByDefault
                     || PlayerProgress.Instance.IsCarUnlocked(_selectedCar.carId);

        if (owned)
        {
          
            PlayerProgress.Instance.SelectCar(_selectedCar.carId);
            ShowToast($"{_selectedCar.displayName} choosen!");
        }
        else
        {
          
            if (PlayerProgress.Instance.SpendGold(_selectedCar.price))
            {
                PlayerProgress.Instance.UnlockCar(_selectedCar.carId);
                ShowToast($"{_selectedCar.displayName} unlocked!");
            }
            else
            {
                ShowToast("Not enough gold! Go back racing you loser...");
            }
        }

        RefreshActionButton();
        RefreshCards();
        RefreshGoldText();
    }
    void RefreshGoldText()
    {
        if (goldText) goldText.text = $"  {PlayerProgress.Instance.Gold}";
    }

    void ShowToast(string message)
    {
        if (toastPanel == null) return;
        toastText.text = message;
        toastPanel.SetActive(true);
        CancelInvoke(nameof(HideToast));
        Invoke(nameof(HideToast), 2.5f);
    }
    void HideToast() { if (toastPanel) toastPanel.SetActive(false); }

    public void GoToMenu()  => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    public void GoToRace()  => UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
}
