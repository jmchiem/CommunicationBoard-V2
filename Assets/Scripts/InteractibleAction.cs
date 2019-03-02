// class skeleton / declaration
// used for non looping interaction actions
// e.g. hitting a button to turn on tracking or calibration 

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Academy
{
    /// <summary>
    /// InteractibleAction performs custom actions when you tap on the holograms.
    /// </summary>
    public abstract class InteractibleAction : MonoBehaviour
    {
        public abstract void PerformAction();
    }
}