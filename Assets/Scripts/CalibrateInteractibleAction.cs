// determines what happens when you click calibrate button
// Calibration plane is activated 
// Board plane in deactivated 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Academy
{
    public class CalibrateInteractibleAction : InteractibleAction
    {
        [SerializeField]
        private GameObject activateObject; // calibration plane
        [SerializeField]
        private GameObject deactivateObject; // board plane

        public override void PerformAction()
        {
            ////////////////////////////////////////////////////////////////////////
            activateObject.transform.position = deactivateObject.transform.position;
            activateObject.transform.rotation = deactivateObject.transform.rotation;
            ////////////////////////////////////////////////////////////////////////

            activateObject.SetActive(true);
            deactivateObject.SetActive(false);
            ////////////////////////////////////////////////////////////////////////
            activateObject.GetComponent<Calibration>().enabled = true;
            ////////////////////////////////////////////////////////////////////////

        }
    }
}
