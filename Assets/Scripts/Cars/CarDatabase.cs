using UnityEngine;

[CreateAssetMenu(fileName = "CarDatabase", menuName = "Racing/Car Database")]
public class CarDatabase : ScriptableObject
{
    public CarData[] cars;

    public CarData GetById(string id)
    {
        foreach (var c in cars)
            if (c.carId == id) return c;
        return null;
    }
}
