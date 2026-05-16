using UnityEngine;
public class RaceSetup : MonoBehaviour
{
    public CarDatabase database;
    public Transform spawnPoint;    

    public RaceManager raceManager;
    public CameraFollow cameraFollow;   

    private GameObject _spawnedCar;

    void Start()
    {
        if (PlayerProgress.Instance == null)
        {
            var pp = new GameObject("PlayerProgress");
            pp.AddComponent<PlayerProgress>();
        }

        SpawnSelectedCar();
    }

    void SpawnSelectedCar()
    {
        string selectedId = PlayerProgress.Instance.SelectedCarId;

        CarData carData = database.GetById(selectedId);
        if (carData == null || carData.carPrefab == null)
        {
            Debug.LogWarning($"[RaceSetup] Car not found '{selectedId}', using default.");
            carData = database.cars[0];
        }

        Vector3    pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        _spawnedCar = Instantiate(carData.carPrefab, pos, rot);
        _spawnedCar.name = "PlayerCar_" + carData.carId;

        _spawnedCar.tag = "Player";


        if (cameraFollow != null)
        {
            cameraFollow.target = _spawnedCar.transform;
        }
        else
        {
      
        var cam = FindObjectOfType<CameraFollow>();
        if (cam) cam.target = _spawnedCar.transform;
        }
        var sensor = _spawnedCar.GetComponentInChildren<CarSensor>();
        if (sensor != null && raceManager != null)
            sensor.raceManager = raceManager;
            
        var surfaceDetector = _spawnedCar.GetComponent<SurfaceDetector>();
        if (surfaceDetector != null && raceManager != null)
            surfaceDetector.raceManager = raceManager;

        var hud = FindObjectOfType<TelemetryHUD>();
        var controller = _spawnedCar.GetComponent<CarController>();
        if (hud != null && controller != null)
        {
            hud.Initialize(controller); 
        }

        Debug.Log($"[RaceSetup] Spawn: {carData.displayName}");
    }
}
