﻿
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScriptTextHandler : MonoBehaviour {
    
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetButtonDown("Fire1") )
	    {
	        SceneManager.LoadScene(SceneSettings.SceneIdForTerrainView);
        }
    }
}
