/**
 * @file PositionReset.cs
 * @brief TODO
 * @author Takashi Kashiwagi
 * @date 2022/08/16
 * @version     1.0
 * @details 
 * --
 * License Type <MIT License>
 * --
 * Copyright 2022 Takashi Kashiwagi
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a 
 * copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the 
 * Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included 
 * in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 * DEALINGS IN THE SOFTWARE.
 *
 */

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class PositionReset : UdonSharpBehaviour
{
    [SerializeField] private GameObject[] targets;
    [UdonSynced(UdonSyncMode.None)] private Vector3[] defaultPos = null;
    [UdonSynced(UdonSyncMode.None)] private Quaternion[] defaultRot = null;
    private Rigidbody[] targetRigidbodies;

    private void Start()
    {
        if (defaultPos == null)
        {
            defaultPos = new Vector3[targets.Length];
        }
        if (defaultRot == null)
        {
            defaultRot = new Quaternion[targets.Length];
        }
        if(targetRigidbodies == null)
        {
            targetRigidbodies = new Rigidbody[targets.Length];
        }

        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            for (int i = 0; i < targets.Length; i++)
            {
                defaultPos[i] = targets[i].transform.localPosition;
                defaultRot[i] = targets[i].transform.localRotation;
            }
            RequestSerialization();
        }
        for (int i = 0; i < targets.Length; i++)
        {
            targetRigidbodies[i] = targets[i].GetComponent<Rigidbody>();
        }
    }
    public override void OnDeserialization()
    {
        
    }

    public override void Interact()
    {

        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            for (int i = 0; i < targets.Length; i++)
            {
                Networking.SetOwner(Networking.LocalPlayer, targets[i]);
            }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetOcjectsPosition");
        }
    }

    public void ResetOcjectsPosition()
    {
        if ((defaultPos != null) && (defaultRot != null) && (targetRigidbodies != null))
        {
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].transform.localPosition = defaultPos[i];
                targets[i].transform.localRotation = defaultRot[i];
                targetRigidbodies[i].velocity = Vector3.zero;
                targetRigidbodies[i].Sleep();
            }
        }
    }
}
