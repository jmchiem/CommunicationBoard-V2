using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Academy
{
    public class HostIpInputInteractibleAction : InteractibleAction
    {
        UnityEngine.TouchScreenKeyboard keyboard;

        public override void PerformAction()
        {
            // Single-line textbox with title
            keyboard = TouchScreenKeyboard.Open(GlobalVars.remoteIP, TouchScreenKeyboardType.Default, false, false, false, false, "Host IP:");
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (TouchScreenKeyboard.visible == false && keyboard != null)
            {
                if (keyboard.status == TouchScreenKeyboard.Status.Done)
                {
                    GlobalVars.remoteIP = keyboard.text;
                    keyboard = null;
                }
            }
        }
    }
}

