using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class End : MonoBehaviour
{
    public GameObject EndScreen;
    private void OnTriggerEnter(Collider other)
    {
        EndScreen.SetActive(true);
    }
}
