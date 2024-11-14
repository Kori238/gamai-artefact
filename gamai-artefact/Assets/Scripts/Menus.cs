using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menus : MonoBehaviour
{
    public static  Menus Instance;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            transform.GetChild(0).gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
