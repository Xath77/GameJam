using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Restart : MonoBehaviour
{
  

public void GameRestart()
    {
        SceneManager.LoadScene(0);
    }
}
