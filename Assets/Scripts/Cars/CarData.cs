using UnityEngine;
[CreateAssetMenu(fileName = "CarData", menuName = "Racing/Car Data")]
public class CarData : ScriptableObject
{
    [Header("Identifier")]
    public string carId;           
    public string displayName;     
    public string description;     

    [Header("Shop")]
    public int    price;           
    public Sprite previewSprite;   
    public GameObject carPrefab;  

    [Header("Statistics")]
    [Range(1, 10)] public int speed        = 5;
    [Range(1, 10)] public int acceleration = 5;
    [Range(1, 10)] public int handling     = 5;
    [Range(1, 10)] public int braking      = 5;

    [Header("Is car unlocked by default?")]
    public bool unlockedByDefault = false;
}
