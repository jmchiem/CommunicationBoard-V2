using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public class GlobalVars : MonoBehaviour {
    [SerializeField]
    private GameObject point1;
    [SerializeField]
    private GameObject point2;
    [SerializeField]
    private GameObject point3;
    [SerializeField]
    private GameObject point4;

    public static float p1x, p1z;
    public static float p2x, p2z;
    public static float p3x, p3z;
    public static float p4x, p4z;
    public static float k01, k02, k03, k04, k05, k06;
    public static float k11, k12, k13, k14, k15, k16;
    public static string remoteIP = "10.16.15.132";
    //public static string remoteIP = "10.16.9.72";
    public static string remotePort = "5005";
    // Use this for initialization
    void Start () {
        k01 = 1; k02 = 0; k03 = 0; k04 = 0; k05 = 1; k06 = 0;
        k11 = 1; k12 = 0; k13 = 0; k14 = 0; k15 = 1; k16 = 0;
        p1x = point1.transform.localPosition.x;
        p2x = point2.transform.localPosition.x;
        p3x = point3.transform.localPosition.x;
        p4x = point4.transform.localPosition.x;
        p1z = point1.transform.localPosition.z;
        p2z = point2.transform.localPosition.z;
        p3z = point3.transform.localPosition.z;
        p4z = point4.transform.localPosition.z;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

}
