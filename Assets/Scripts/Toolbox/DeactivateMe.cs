﻿using UnityEngine;

public class DeactivateMe : MonoBehaviour {
    void Start()
    {
        gameObject.SetActive(false);
        print("Deactivate "+ gameObject.name);
    }
}