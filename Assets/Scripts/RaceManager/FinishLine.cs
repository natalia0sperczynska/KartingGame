using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishLine : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RaceManager manager = FindObjectOfType<RaceManager>();
            if (manager != null)
            {
                manager.EndRace(); 
            }
        }
    }
}
