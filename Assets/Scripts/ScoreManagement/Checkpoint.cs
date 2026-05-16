using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public RaceManager raceManager;

    public float bonusPoints = 500f;
 
    public ParticleSystem collectEffect;
    public AudioClip collectSound;
    private AudioSource   _audio;
    
 
    void Start() => _audio = GetComponent<AudioSource>();
 
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
 
        raceManager.AddCheckpointPoints(bonusPoints);
 
        if (collectEffect) collectEffect.Play();
        if (collectSound && _audio) _audio.PlayOneShot(collectSound);

        GetComponent<Renderer>()?.gameObject.SetActive(false);
        Invoke(nameof(Deactivate), 0.5f);
    }
 
    void Deactivate() => gameObject.SetActive(false);
}