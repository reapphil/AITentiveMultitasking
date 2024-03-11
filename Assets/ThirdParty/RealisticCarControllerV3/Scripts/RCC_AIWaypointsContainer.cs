//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Used for holding a list for waypoints, and drawing gizmos for all of them.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/AI/RCC AI Waypoints Container")]
public class RCC_AIWaypointsContainer : MonoBehaviour {

    public List<RCC_Waypoint> waypoints = new List<RCC_Waypoint>();     //  All waypoints.

    private void Start() {

        RCC_Waypoint[] childWaypoints = GetComponentsInChildren<RCC_Waypoint>();
        waypoints = childWaypoints.ToList();

    }

    /// <summary>
    /// Used for drawing gizmos on Editor.
    /// </summary>
    private void OnDrawGizmos() {

        //  If waypoints list is null, return.
        if (waypoints == null)
            return;

        //  Counting all waypoints.
        for (int i = 0; i < waypoints.Count; i++) {

            //  If current waypoint is not null, continue.
            if (waypoints[i] != null) {

                //  Drawing gizmos.
                Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 0.3f);
                Gizmos.DrawSphere(waypoints[i].transform.position, 2);
                Gizmos.DrawWireSphere(waypoints[i].transform.position, waypoints[i].radius);

                //  If current waypoint is not last waypoint...
                if (i < waypoints.Count - 1) {

                    //  if current waypoint has next waypoint...
                    if (waypoints[i] && waypoints[i + 1]) {

                        Gizmos.color = Color.green;

                        if (i < waypoints.Count - 1)
                            Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
                        if (i < waypoints.Count - 2)
                            Gizmos.DrawLine(waypoints[waypoints.Count - 1].transform.position, waypoints[0].transform.position);

                    }

                }

            }

        }

    }

}
