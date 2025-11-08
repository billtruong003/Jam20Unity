using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UNI_EnableAfterDelay : MonoBehaviour
{
    public GameObject gameObjectToEnable;
    public float delay = 4.0f;

    public void Start_Timer()
    {
        StartCoroutine(EnableGO());
    }

    IEnumerator EnableGO()
    {
        yield return new WaitForSeconds(delay);
        gameObjectToEnable.SetActive(true);
    }

}
