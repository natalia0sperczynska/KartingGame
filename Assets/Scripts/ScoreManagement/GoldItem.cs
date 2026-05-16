using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldItem : MonoBehaviour
{
    public int value = 10;
 
    public float rotationSpeed = 100f;
    public float bobAmplitude  = 0.15f; 
    public float bobSpeed     = 2f;
    public AudioClip collectSound;
    private AudioSource   _audio;
    public ParticleSystem collectEffect;
 
    private RaceManager _raceManager;
    private Vector3     _startPos;
 
    void Start()
    {
        _raceManager = FindObjectOfType<RaceManager>();
        _startPos    = transform.position;
    }
 
    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
 

        float y = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = new Vector3(_startPos.x, y, _startPos.z);
    }
 
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_raceManager != null)
            _raceManager.AddGold(value);
        if (collectEffect) collectEffect.Play();
        if (collectSound && _audio) _audio.PlayOneShot(collectSound);
        Destroy(gameObject);
    }
}
