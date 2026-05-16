using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class CarCard : MonoBehaviour
{
    [Header("UI References")]
    public Image             previewImage;
    public TextMeshProUGUI   carNameText;
    public TextMeshProUGUI   priceText;
    public GameObject        lockedOverlay;  
    public Image             selectedBorder;  

    private CarData       _car;
    private CarShopManager _shop;
    private Button        _button;

    public void Setup(CarData car, CarShopManager shop)
    {
        _car    = car;
        _shop   = shop;
        _button = GetComponent<Button>();

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => shop.ShowDetail(car));

        Refresh();
    }

    public void Refresh()
    {
        if (_car == null) return;

        bool owned    = _car.unlockedByDefault
                        || PlayerProgress.Instance.IsCarUnlocked(_car.carId);
        bool selected = PlayerProgress.Instance.SelectedCarId == _car.carId;

        if (previewImage)
            previewImage.sprite = _car.previewSprite;

   
        if (carNameText)
            carNameText.text = _car.displayName;


        if (priceText)
        {
            if (selected && owned)       priceText.text = "YOUR";
            else if (owned)              priceText.text = "UNLOCKED";
            else                         priceText.text = $"{_car.price}";

            priceText.color = selected  ? Color.yellow
                            : owned     ? Color.green
                            :             Color.white;
        }

        if (lockedOverlay)
            lockedOverlay.SetActive(!owned);

    
        if (selectedBorder)
            selectedBorder.enabled = selected;
    }
}
