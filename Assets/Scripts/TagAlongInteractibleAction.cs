using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
using UnityEngine;

namespace Academy
{
    public class TagAlongInteractibleAction : InteractibleAction
    {
        [SerializeField]
        [Tooltip("Drag the Tagalong prefab asset you want to display.")]
        private GameObject objectToTrack;

        private bool trackEnabled = false;
        private Vector3 orignalVector;
        private Quaternion orignalRotation;

        public override void PerformAction()
        {
            // Recommend having only one tagalong.
            if (objectToTrack == null)
            {
                return;
            }
            if (trackEnabled)
            {
                //////////////////////////////////////////////////////////
                orignalVector = objectToTrack.transform.position;
                orignalRotation = objectToTrack.transform.rotation;

                objectToTrack.GetComponent<SphereBasedTagalong>().enabled = false;
                objectToTrack.transform.rotation = orignalRotation;
                objectToTrack.transform.position = orignalVector;
                //////////////////////////////////////////////////////////
                
                objectToTrack.GetComponent<Billboard>().enabled = false;
                trackEnabled = false;
            }
            else
            {
                //////////////////////////////////////////////////////////
                objectToTrack.GetComponent<SphereBasedTagalong>().enabled = true;
                //////////////////////////////////////////////////////////
                
                objectToTrack.GetComponent<Billboard>().enabled = true;
                trackEnabled = true;
            }
        }
    }
}
