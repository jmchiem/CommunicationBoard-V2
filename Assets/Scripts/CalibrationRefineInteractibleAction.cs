// refine initial position of pupil objects
// ???

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Academy
{
    public class CalibrationRefineInteractibleAction : InteractibleAction
    {
        [SerializeField]
        private GameObject pupil0;
        [SerializeField]
        private GameObject pupil1;

        public override void PerformAction()
        {
            GlobalVars.k03 -= pupil0.transform.localPosition.x;
            GlobalVars.k06 -= pupil0.transform.localPosition.z;
            GlobalVars.k13 -= pupil1.transform.localPosition.x;
            GlobalVars.k16 -= pupil1.transform.localPosition.z;
        }
    }
}


