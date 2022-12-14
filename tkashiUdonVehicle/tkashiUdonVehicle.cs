/**
 * @file tkashiUdonVehicle.cs
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
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class tkashiUdonVehicle : UdonSharpBehaviour
{
    [Header("------------------------------------------------------------------")]
    [Header("a game object has VRC pickup for Handle")]
    [Header("Handle angles refer to HandleGameobject.transform.localEulerAngles.z")]
    [SerializeField] private GameObject HandleGameobject;
    [SerializeField] private IsPickUped HandlePickup;

    [Header("------------------------------------------------------------------")]
    [Header("a game object has VRC pickup for accelerator")]
    [Header("Accelerator opening refers to AcceleratorGameObject.transform.localEulerAngles.x")]
    [SerializeField] private GameObject AcceleratorGameObject;
    [SerializeField] private IsPickUped AcceleratorPickup;

    [Header("------------------------------------------------------------------")]
    [Header("a game Object with wheel collider")]
    [SerializeField] private GameObject FrontWheel_L;
    [SerializeField] private GameObject FrontWheel_R;
    [SerializeField] private GameObject RearWheel_L;
    [SerializeField] private GameObject RearWheel_R;

    [Header("------------------------------------------------------------------")]
    [Header("a game object that has tire mesh")]
    [SerializeField] private GameObject FrontTire_L;
    [SerializeField] private GameObject FrontTire_R;
    [SerializeField] private GameObject RearTire_L;
    [SerializeField] private GameObject RearTire_R;

    [Header("------------------------------------------------------------------")]
    [Header("Max MotorTorque for wheel Collider")]
    [SerializeField] [Range(1.0f, 2000.0f)] private float maxMotorTorque = 500.0f;

    [Header("------------------------------------------------------------------")]
    [Header("handle responsiveness")]
    [SerializeField] [Range(0.1f, 10.0f)] private float handleResponse = 0.3f;
    [Header("Max Steering Angle")]
    [SerializeField] [Range(0.0f, 90.0f)] private float MaxSteerAngle = 30.0f;

    [Header("------------------------------------------------------------------")]
    [Header("Accelerator responsiveness")]
    [SerializeField] [Range(0.1f, 10.0f)] private float AcceleratorResponse = 2.0f;

    [Header("------------------------------------------------------------------")]
    [Header("Audio Source Object")]

    [SerializeField] private AudioSource audioSrcEngineStart;
    [SerializeField] private AudioSource audioSrcEngineStop;
    [SerializeField] private AudioSource audioSrcEngineIdle;
    [SerializeField] private AudioSource audioSrcRoadNoise;

    [Header("------------------------------------------------------------------")]
    [SerializeField] private TextMeshProUGUI SpeedMeter;
    [Header("Default = 300 km/h")]
    [SerializeField] [Range(0.0f, 500.0f)] private float downForceSpeedTh = 200.0f;
    [Header("Default = 2.0")]
    [SerializeField] [Range(0.0f, 100.0f)] private float downForceRate = 2.0f;

    [SerializeField] private Vector3 m_defaultPosHandle = new Vector3(-0.35f, 0.55f, 0.45f);
    [SerializeField] private Vector3 m_defaultPosAccelerator = new Vector3(0.35f, 0.55f, 0.45f);
    [SerializeField] private Vector3 defaultCenterOfMass = new Vector3(0.0f, 0.0f, 0.0f);
    [SerializeField] [Range(0.0f, 10000.0f)] private float maxBreakTorque = 10000.0f;
    [SerializeField] private bool bDebugMode=false;
    private Rigidbody m_thisRigid;
    private Rigidbody m_HandleRigid;
    private Rigidbody m_AcceleratorRigid;
    private WheelCollider wcFrontL;
    private WheelCollider wcFrontR;
    private WheelCollider wcRearL;
    private WheelCollider wcRearR;

    // Sync Parameters

    [UdonSynced, FieldChangeCallback(nameof(SyncedTireSteerAngle))] private float TireSteerAngle;
    [UdonSynced, FieldChangeCallback(nameof(SyncedLastPos))] private Vector3 LastPos;
    [UdonSynced, FieldChangeCallback(nameof(SyncedLastRotation))] private Quaternion LastRotation;
    [UdonSynced, FieldChangeCallback(nameof(SyncedHandleRigidPos))] private Vector3 HandleRigidPos;
    [UdonSynced, FieldChangeCallback(nameof(SyncedAcceleratorRigidPos))] private Vector3 AcceleratorRigidPos;

    [UdonSynced, FieldChangeCallback(nameof(SyncedbrakeTorque))] private float brakeTorque;
    [UdonSynced, FieldChangeCallback(nameof(SyncedmotoTorque))] private float motorTorque;
    [UdonSynced, FieldChangeCallback(nameof(SyncedBrakeOn))] private bool BrakeOn;
    [UdonSynced, FieldChangeCallback(nameof(SyncedSpeedkmh))] private float Speedkmh;

	[UdonSynced, FieldChangeCallback(nameof(SyncedbDrive))] private bool bDriving;
    private bool bDrivingLocal = false;

    public bool SyncedbDrive
    {
        set
        {
            bDriving = value;
            bDrivingLocal = bDriving;
        }
        get => bDriving;
    }

    public float SyncedSpeedkmh
    {
        set
        {
            Speedkmh = value;
        }
        get => Speedkmh;
    }

    public bool SyncedBrakeOn
    {
        set
        {
            BrakeOn = value;        
        }
        get => BrakeOn;
    }
    public float SyncedmotoTorque
    {
        set
        {
            motorTorque = value;
        }
        get => motorTorque;
    }
    public float SyncedbrakeTorque
    {
        set
        {
            brakeTorque = value;
        }
        get => brakeTorque;
    }

    public Vector3 SyncedLastPos
    {
        set
        {
            LastPos = value;
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
    
    public float SyncedTireSteerAngle
    {
        set
        {
            TireSteerAngle = value;
        }
        get => TireSteerAngle;
    }

    public Vector3 SyncedHandleRigidPos
    {
        set
        {
            HandleRigidPos = value;
        }
        get => HandleRigidPos;
    }
    public Vector3 SyncedAcceleratorRigidPos
    {
        set
        {
            AcceleratorRigidPos = value;
        }
        get => AcceleratorRigidPos;
    }
    
    // Start is called before the first frame update
    private void setupComponent()
    {
        m_thisRigid = GetComponent<Rigidbody>();
        m_HandleRigid = HandleGameobject.GetComponent<Rigidbody>();
        m_AcceleratorRigid = AcceleratorGameObject.GetComponent<Rigidbody>();
        wcFrontL = FrontWheel_L.GetComponent<WheelCollider>();
        wcFrontR = FrontWheel_R.GetComponent<WheelCollider>();
        wcRearL = RearWheel_L.GetComponent<WheelCollider>();
        wcRearR = RearWheel_R.GetComponent<WheelCollider>();
        
    }
    void Start()
    {
        bDrivingLocal = bDebugMode;
        setupComponent();

        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            motorTorque = 0u;
            Speedkmh = 0.0f;
            bDriving = false;
        }
        

        if(audioSrcEngineStop != null)
        {
            if(audioSrcEngineStop.clip != null)
            {
                audioSrcEngineStop.Stop();
            }
        }
        if(audioSrcEngineIdle != null)
        {
            if(audioSrcEngineIdle.clip != null)
            {
                audioSrcEngineIdle.Stop();
            }
        }
        if(audioSrcRoadNoise != null)
        {
            if(audioSrcRoadNoise.clip != null)
            {
                audioSrcRoadNoise.Stop();
            }
        }
        if(audioSrcEngineStart != null)
        {
            if(audioSrcEngineStart.clip != null)
            {
                audioSrcEngineStart.Stop();
            }
        }
        m_thisRigid.centerOfMass = defaultCenterOfMass;
    }

    private void resetHande_Accelerator_Position()
    {
        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            if(m_HandleRigid != null)
            {
                if(HandlePickup.pickUped == false)
                {   // TransformPoint : ローカル空間からワールド空間へ position を変換します。
                    HandleRigidPos = this.transform.TransformPoint(m_defaultPosHandle);
                    m_HandleRigid.MovePosition(HandleRigidPos);
                }
            }
            if(m_AcceleratorRigid != null)
            {
                if(AcceleratorPickup.pickUped == false)
                {
                    // TransformPoint : ローカル空間からワールド空間へ position を変換します。
                    AcceleratorRigidPos = this.transform.TransformPoint(m_defaultPosAccelerator);
                    m_AcceleratorRigid.MovePosition(AcceleratorRigidPos);
                }
            }
        }
    }
    
    private float getSteerAngle()
    {
        float angle = HandleGameobject.transform.localEulerAngles.z; // どうもrigid bodyにはlocalRationは無いようだ
        angle %= 360;
        if(angle > 180f)
        {
            angle = -(360 - angle);
        }
        
        angle = Mathf.Clamp(angle * handleResponse, -MaxSteerAngle, MaxSteerAngle);
        return angle;
    }
    private void Update()
    {
        if (bDrivingLocal)
        {
            if(!Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
            {
                m_thisRigid.MovePosition(LastPos);
                m_HandleRigid.MovePosition(HandleRigidPos);
                m_AcceleratorRigid.MovePosition(AcceleratorRigidPos);
            }
        }
    }
    private void LateUpdate()
    {
        if (bDrivingLocal)
        {
            if(audioSrcEngineIdle != null)
            {
                audioSrcEngineIdle.pitch = 1.0f + Mathf.Abs(motorTorque)/ maxMotorTorque;
            }

            if(audioSrcRoadNoise != null)
            {
                audioSrcRoadNoise.volume = Mathf.Abs(Speedkmh)/ downForceSpeedTh;
            }
            if(Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
            {
                RequestSerialization();
            }
            // Update speed meter
            int temp = Convert.ToInt32(Speedkmh);
            SpeedMeter.text = temp.ToString();
            
        }
    }
    private void FixedUpdate()
    {
        if (bDrivingLocal)
        {
            if(Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
            {
                resetHande_Accelerator_Position();

                Speedkmh = Mathf.Abs(Vector3.Dot(m_thisRigid.velocity, this.transform.forward)) * 3.6f;                        
                LastPos = m_thisRigid.position;
                LastRotation = m_thisRigid.rotation;
                TireSteerAngle = -getSteerAngle();

                float EulerAnglesZ = - AcceleratorGameObject.transform.localEulerAngles.x;
                float radians = EulerAnglesZ / 180 * Mathf.PI; // ラジアンに変換
                motorTorque = maxMotorTorque * Mathf.Clamp(AcceleratorResponse * Mathf.Sin(radians), -1.0f, 1.0f);
            }
            updateDownForce(Speedkmh);
            updateMotorBreakTorque(motorTorque, brakeTorque);
            updateWheelAngle(TireSteerAngle);
            updateTireMeshByWheelCollider();
        }
    }
    private void updateTireMeshByWheelCollider()
    {
        if (bDrivingLocal)
        {
            Vector3 pos;
            Quaternion rot;
            wcFrontL.GetWorldPose(out pos, out rot);
            FrontTire_L.transform.position = pos;
            FrontTire_L.transform.rotation = rot;

            wcFrontR.GetWorldPose(out pos, out rot);
            FrontTire_R.transform.position = pos;
            FrontTire_R.transform.rotation = rot;

            wcRearL.GetWorldPose(out pos, out rot);
            RearTire_L.transform.position = pos;
            RearTire_L.transform.rotation = rot;

            wcRearR.GetWorldPose(out pos, out rot);
            RearTire_R.transform.position = pos;
            RearTire_R.transform.rotation = rot;
        }
    }

    private void updateWheelAngle(float angle)
    {
        if (bDrivingLocal)
        {
            wcFrontL.steerAngle = angle;
            wcFrontR.steerAngle = angle;
        }
    }

    public void updateDownForce(float val)
    {
        if (bDrivingLocal)
        {
            float fp = downForceRate*(Speedkmh/downForceSpeedTh);
            m_thisRigid.AddForce(this.transform.TransformPoint(Vector3.up * fp * (-9.8f)), ForceMode.Force);
        }
    }
    public void updateMotorBreakTorque(float mtrTq, float brkTq)
    {
        if (bDrivingLocal)
        {
            //Vector3 v = this.transform.TransformPoint(Vector3.right);
            //Vector3 vn = v.normalized;
            float VSO = Mathf.Abs(Vector3.Dot(m_thisRigid.velocity, this.transform.forward));    // m/s
            Debug.Log("Speed = " + m_thisRigid.velocity.magnitude + ", VSO = " + VSO);
            //Debug.Log("v = " + v + ", vn = " + vn);

            if(BrakeOn == false)
            {

                if(isTireSlipping(VSO, Mathf.Abs(wcFrontL.rpm), wcFrontL.radius))
                {   // tires are spinning
                    wcFrontL.motorTorque = 0.0f;
                }
                else
                {
                    wcFrontL.motorTorque = mtrTq;
                }

                if(isTireSlipping(VSO, Mathf.Abs(wcFrontR.rpm), wcFrontR.radius))
                {   // tires are spinning
                    wcFrontR.motorTorque = 0.0f;
                }
                else
                {
                    wcFrontR.motorTorque = mtrTq;
                }

                if(isTireSlipping(VSO, Mathf.Abs(wcRearL.rpm), wcRearL.radius))
                {   // tires are spinning
                    wcRearL.motorTorque = 0.0f;
                }
                else
                {
                    wcRearL.motorTorque = mtrTq;
                }

                if(isTireSlipping(VSO, Mathf.Abs(wcRearR.rpm), wcRearR.radius))
                {   // tires are spinning
                    wcRearR.motorTorque = 0.0f;
                }
                else
                {
                    wcRearR.motorTorque = mtrTq;
                }
                wcFrontL.brakeTorque = 0.0f;
                wcFrontR.brakeTorque = 0.0f;
                wcRearL.brakeTorque = 0.0f;
                wcRearR.brakeTorque = 0.0f;
            }
            else
            {
                wcFrontL.motorTorque = 0.0f;
                wcFrontR.motorTorque = 0.0f;
                wcRearL.motorTorque = 0.0f;
                wcRearR.motorTorque = 0.0f;

                if((Mathf.Abs(wcFrontR.rpm) <= 0.1f) && (VSO >= 1.0f))
                {   // tires are spinning
                    wcFrontL.brakeTorque = 0.0f;
                }
                else
                {
                    wcFrontL.brakeTorque = brkTq;
                }

                if((Mathf.Abs(wcFrontR.rpm) <= 0.1f) && (VSO >= 1.0f))
                {   // tires are spinning
                    wcFrontR.brakeTorque = 0.0f;
                }
                else
                {
                    wcFrontR.brakeTorque = brkTq;
                }

                if((Mathf.Abs(wcRearL.rpm) <= 0.1f) && (VSO >= 1.0f))
                {   // tires are spinning
                    wcRearL.brakeTorque = 0.0f;
                }
                else
                {
                    wcRearL.brakeTorque = brkTq;
                }

                if((Mathf.Abs(wcRearR.rpm) <= 0.1f) && (VSO >= 1.0f))
                {   // tires are spinning
                    wcRearR.brakeTorque = 0.0f;
                }
                else
                {
                    wcRearR.brakeTorque = brkTq;
                }
            }
        }
    }
    private bool isTireSlipping(float VSO, float tireRpm, float tireRadius)
    {
        bool bSlipping = false;

        
        float tireSpeed = Mathf.Abs(0.1047197f * tireRadius * tireRpm);    // 3.141592 * 2 * tireRadius / 60

        //Debug.Log("m_thisRigid.velocity.magnitude = " + m_thisRigid.velocity.magnitude);
        //Debug.Log("Mathf.Abs(m_thisRigid.velocity.z) = " + Mathf.Abs(m_thisRigid.velocity.z));
        //vector3 tvec3 = new vector3(Vector3.Normalize(this.transform.TransformPoint(Vector3.right)));
        //Debug.Log("tvec3 = " + Vector3.Normalize(this.transform.TransformPoint(Vector3.right)));
        //Debug.Log("Mathf.Abs(Vector3.Dot(m_thisRigid.velocity, tvec3))= " + Mathf.Abs(Vector3.Dot(m_thisRigid.velocity, Vector3.Normalize(this.transform.TransformPoint(Vector3.right)))));
        
        //Debug.Log("RPM = " + tireRpm + ", tireRadius = " + tireRadius + ", tireSpeed = " + tireSpeed);

        if((tireSpeed > (VSO *1.2f)) && (VSO >= 3.0f))
        {
            bSlipping = true;
        }
        return bSlipping;
    }
    public override void Interact()
    {
        
        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        Networking.LocalPlayer.UseAttachedStation();
        
    }
    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        if (bDrivingLocal)
        {
            if(Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
            {
                BrakeOn = value;
                if(value == false)
                {
                    brakeTorque = 0.0f;
                }
                else
                {
                    brakeTorque = maxBreakTorque;
                }
                updateMotorBreakTorque(motorTorque, brakeTorque);  
            }
        }
    }

    public void initVehicle()
    {
        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            resetHande_Accelerator_Position();
            motorTorque = 0.0f;
            brakeTorque = 0.0f;
        }
        HandleGameobject.transform.localEulerAngles = Vector3.zero;
        AcceleratorGameObject.transform.localEulerAngles = Vector3.zero;
        m_HandleRigid.rotation = m_thisRigid.rotation;
        m_AcceleratorRigid.rotation = m_thisRigid.rotation;
        
        
        updateMotorBreakTorque(0.0f, 0.0f); 
        updateWheelAngle(0.0f);
        updateTireMeshByWheelCollider();
        m_thisRigid.velocity = Vector3.zero;
    }
    public override void OnStationEntered(VRCPlayerApi player)
    {   // OnStationEnteredは全プレイヤーから呼び出される
        initVehicle();
        StationEnterSound();
        bDrivingLocal = true;
        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            bDriving = true;
            RequestSerialization();
        }
    }

    public override void OnStationExited(VRCPlayerApi player)
    {   // OnStationExitedは全プレイヤーから呼び出される
        initVehicle();
        StationExitSound();
        bDrivingLocal = false;
        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            bDriving = false;
            RequestSerialization();
        }
    }

    public void StationEnterSound()
    {
        if(audioSrcEngineStart != null)
        {
            if(audioSrcEngineStart.clip != null)
            {
                audioSrcEngineStart.Play();
            }
        }
        if(audioSrcEngineIdle != null)
        {
            if(audioSrcEngineIdle.clip != null)
            {
                audioSrcEngineIdle.loop = true;
                audioSrcEngineIdle.Play();
            }
        }
        if(audioSrcRoadNoise != null)
        {
            if(audioSrcRoadNoise.clip != null)
            {
                audioSrcRoadNoise.loop = true;
                audioSrcRoadNoise.volume = 0.0f;
                audioSrcRoadNoise.Play();
            }
        }
    }

    public void StationExitSound()
    {
        if(audioSrcEngineStop != null)
        {
            if(audioSrcEngineStop.clip != null)
            {
                audioSrcEngineStop.Play();
            }
        }
        if(audioSrcEngineIdle != null)
        {
            if(audioSrcEngineIdle.clip != null)
            {
                audioSrcEngineIdle.Stop();
            }
        }
        if(audioSrcRoadNoise != null)
        {
            if(audioSrcRoadNoise.clip != null)
            {
                audioSrcRoadNoise.Stop();
            }
        }
    }
}
