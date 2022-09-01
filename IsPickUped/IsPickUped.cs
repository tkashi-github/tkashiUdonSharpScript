/**
 * @file IsPickUped.cs
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

public class IsPickUped : UdonSharpBehaviour
{
    private Rigidbody m_thisRigid;

    [UdonSynced, FieldChangeCallback(nameof(SyncedpickUped))] public bool pickUped = false;
    [UdonSynced, FieldChangeCallback(nameof(SyncedLastPos))] private Vector3 LastPos;
    [UdonSynced, FieldChangeCallback(nameof(SyncedLastRotation))] private Quaternion LastRotation;

    public Vector3 SyncedLastPos
    {
        set
        {
            LastPos = value;
            m_thisRigid.position = LastPos;
        }
        get => LastPos;
    }
    public Quaternion SyncedLastRotation
    {
        set
        {
            LastRotation = value;
            m_thisRigid.rotation = LastRotation;
        }
        get => LastRotation;
    }
    public bool SyncedpickUped
    {
        set
        {
            pickUped = value;
        }
        get => pickUped;
    }

    void Start()
    {
        m_thisRigid = GetComponent<Rigidbody>();

        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            pickUped = false;
        }
        
    }
    private void FixedUpdate()
    {
        if(pickUped != false)
        {
            LastPos = m_thisRigid.position;
            LastRotation = m_thisRigid.rotation;
        }
    }

    public override void OnDrop()
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        pickUped = false;
    }
    public override void OnPickup()
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        pickUped = true;
    }
}
