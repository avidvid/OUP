﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoveInside : MonoBehaviour
{
    private Vector3 _previousPosition = Vector3.zero;
    private BuildingInterior _building;

    // Update is called once per frame
    void Update()
    {
        Vector3 currentPos = transform.position;
        var targetTile = new Rect(currentPos, Vector2.one);
        _building = GameObject.Find("Building Interior").GetComponent<BuildingInterior>();
        if (_building.IsExiting(targetTile))
            SceneManager.LoadScene(SceneSettings.SceneIdForTerrainView);
        if (_building.IsBlocked(targetTile))
            transform.position = currentPos = _previousPosition;
        _previousPosition = currentPos;
    }
}
