/**
 * @file PickupUseDownToggle.cs
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
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class PickupUseDownToggle : UdonSharpBehaviour
{
    public AudioSource AudioObj;
    
    [Header("RefreshRate[1Hz ~ 120Hz]")]
    [SerializeField] [Range(1.0f, 120.0f)] private float fpRefreshRate;

    [UdonSynced, FieldChangeCallback(nameof(SyncedLastPos))] private Vector3 LastPos;
    [UdonSynced, FieldChangeCallback(nameof(SyncedLastRotation))] private Quaternion LastRotation;
    [UdonSynced, FieldChangeCallback(nameof(SyncedLastVelocity))] private Vector3 LastVelocity;
    [UdonSynced, FieldChangeCallback(nameof(SyncedUseGravity))] private bool bUseGravity;
    [UdonSynced, FieldChangeCallback(nameof(SyncedIsKinematic))] private bool bIsKinematic;
    [UdonSynced, FieldChangeCallback(nameof(SyncedDefaultUseGravity))] private bool defaultUseGravity;
    [UdonSynced, FieldChangeCallback(nameof(SyncedDefaultKinematic))] private bool defaultKinematic;
    private float fpRefreshT;
    private float LastUpdateTime;
    private Rigidbody m_RigidBody;

    public bool SyncedDefaultUseGravity
    {
        set
        {
            defaultUseGravity = value;
        }
        get => defaultUseGravity;
    }
    public bool SyncedDefaultKinematic
    {
        set
        {
            defaultKinematic = value;
        }
        get => defaultKinematic;
    }
    public Vector3 SyncedLastPos
    {
        set
        {
            LastPos = value;
            this.gameObject.transform.position = LastPos;
        }
        get => LastPos;
    }
    public Quaternion SyncedLastRotation
    {
        set
        {
            LastRotation = value;
            this.gameObject.transform.rotation = LastRotation;
        }
        get => LastRotation;
    }
    public Vector3 SyncedLastVelocity
    {
        set
        {
            LastVelocity = value;
            if(m_RigidBody.isKinematic)
            {
                m_RigidBody.velocity = Vector3.zero;
                
            }
            else
            {
                m_RigidBody.velocity = LastVelocity;
            }
        }
        get => LastVelocity;
    }
    public bool SyncedUseGravity
    {
        set
        {
            bUseGravity = value;
            m_RigidBody.useGravity = bUseGravity;
        }
        get => bUseGravity;
    }
    public bool SyncedIsKinematic
    {
        set
        {
            bIsKinematic = value;
            m_RigidBody.isKinematic = bIsKinematic;
        }
        get => bIsKinematic;
    }


    void Start()
    {
        m_RigidBody = GetComponent<Rigidbody>();
        defaultUseGravity = m_RigidBody.useGravity;
        defaultKinematic = m_RigidBody.isKinematic;
        LastUpdateTime = 0.0f;
        if(fpRefreshRate < 1.0f)
        {
            fpRefreshRate = 1.0f;
        }
        fpRefreshT = 1.0f/fpRefreshRate;

        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            bUseGravity = defaultUseGravity;
            LastPos = this.gameObject.transform.position;
            LastRotation = this.gameObject.transform.rotation;
            LastVelocity = Vector3.zero;
        }
        m_RigidBody.useGravity = bUseGravity;
        this.gameObject.transform.position = LastPos;
        this.gameObject.transform.rotation = LastRotation;
        m_RigidBody.velocity = LastVelocity;
    }

    public override void OnPickupUseDown()
    {
        if (!Networking.IsOwner(gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

        if(AudioObj.isPlaying)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "stopAudio");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "playAudio");
        }
    }

    public void playAudio()
    {
        AudioObj.Play();
    }
    public void stopAudio()
    {
        AudioObj.Stop();
    }
    
    private void FixedUpdate()
    {
        if((Time.time - LastUpdateTime) < fpRefreshT)
        {
            return;
        }
        if(this.gameObject.transform.position == LastPos)
        {
            return;
        }
        if(Networking.IsOwner(Networking.LocalPlayer, this.gameObject) == true)
        {
            LastUpdateTime = Time.time;
            LastVelocity = m_RigidBody.velocity;
            LastPos = this.gameObject.transform.position;
            LastRotation = this.gameObject.transform.rotation;
            RequestSerialization();
            
        }
    }
    
    public override void OnDrop()
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            bUseGravity = defaultUseGravity;
            m_RigidBody.useGravity = bUseGravity;
            bIsKinematic = defaultKinematic;
            m_RigidBody.isKinematic = bIsKinematic;
            LastVelocity = m_RigidBody.velocity;

            RequestSerialization();

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "stopAudio");
        }
    }
    public override void OnPickup()
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            LastVelocity = Vector3.zero;
            LastPos = this.gameObject.transform.position;
            LastRotation = this.gameObject.transform.rotation;
            bUseGravity = false;
            m_RigidBody.useGravity = bUseGravity;
            bIsKinematic = true;
            m_RigidBody.isKinematic = bIsKinematic;
            RequestSerialization();
        }
    }
}
