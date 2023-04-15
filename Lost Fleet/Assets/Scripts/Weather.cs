using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weather : MonoBehaviour
{

    public static Weather Instance { get; private set; }


    public Vector3 globalWind = new();

    // Start is called before the first frame update
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
        }
        else
        {
            Instance = this;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
