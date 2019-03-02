// turn action 'off' 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
namespace Academy
{
    public class ExitInteractibleAction : InteractibleAction
    {
        public override void PerformAction()
        {
#if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
            ///////////////////////////////////////////////////
            Windows.UI.Xaml.Application.Current.Exit();
            ///////////////////////////////////////////////////

#endif
        }
    }
}

