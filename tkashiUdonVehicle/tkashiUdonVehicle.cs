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
    [Header("Accelerator opening refers to AxleGameObject.transform.localEulerAngles.x")]
    [SerializeField] private GameObject AxleGameObject;
    [SerializeField] private IsPickUped AxlePickup;

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
    [SerializeField] [Range(1.0f, 2000.0f)] private float maxMotorTorque = 1000.0f;

    [Header("------------------------------------------------------------------")]
    [Header("handle responsiveness")]
    [SerializeField] [Range(0.1f, 10.0f)] private float handleResponse = 0.3f;
    [Header("Max Steering Angle")]
    [SerializeField] [Range(0.0f, 90.0f)] private float MaxSteerAngle = 30.0f;

    [Header("------------------------------------------------------------------")]
    [Header("Axle responsiveness")]
    [SerializeField] [Range(0.1f, 10.0f)] private float axleResponse = 1.0f;

    
   

    [Header("------------------------------------------------------------------")]
    [Header("Debug Mode for Unity Play Mode")]
    [SerializeField] private bool bDebugMode = false;

    [Header("------------------------------------------------------------------")]
    [Header("Audio Source Object")]

    [SerializeField] private AudioSource audioSrcEngineStart;
    [SerializeField] private AudioSource audioSrcEngineStop;
    [SerializeField] private AudioSource audioSrcEngineIdle;
    [SerializeField] private AudioSource audioSrcRoadNoise;

    [Header("------------------------------------------------------------------")]
    [SerializeField] private TextMeshProUGUI SpeedMeter;
    [Header("Default = 300 km/h")]
    [SerializeField] [Range(0.0f, 500.0f)] private float downForceSpeedTh = 300.0f;
    [Header("Default = 2.0")]
    [SerializeField] [Range(0.0f, 10.0f)] private float downForceRate = 2.0f;

    [SerializeField] private Vector3 m_defaultPosHandle = new Vector3(-0.35f, 0.55f, 0.45f);
    [SerializeField] private Vector3 m_defaultPosAxle = new Vector3(0.35f, 0.55f, 0.45f);

    [SerializeField] private TextMeshProUGUI degubWindow;

    
    private Rigidbody m_thisRigid;
    private Rigidbody m_HandleRigid;
    private Rigidbody m_AxleRigid;
    private float Speedkmh = 0.0f;
    private WheelCollider wcFrontL;
    private WheelCollider wcFrontR;
    private WheelCollider wcRearL;
    private WheelCollider wcRearR;

    // Sync Parameters

    [UdonSynced, FieldChangeCallback(nameof(SyncedLastSpeed))] private float _LastSpeed;
    [UdonSynced, FieldChangeCallback(nameof(SyncedTireSteerAngle))] private float TireSteerAngle;
    [UdonSynced, FieldChangeCallback(nameof(SyncedLastPos))] private Vector3 LastPos;
    [UdonSynced, FieldChangeCallback(nameof(SyncedLastRotation))] private Quaternion LastRotation;

    [UdonSynced, FieldChangeCallback(nameof(SyncedEngineIdlePitch))] private float EngineIdlePitch;
    [UdonSynced, FieldChangeCallback(nameof(SyncedRoadNoiseVolume))] private float RoadNoiseVolume;
    [UdonSynced, FieldChangeCallback(nameof(SyncedHandleAngle))] private Vector3 HandleAngle;
    [UdonSynced, FieldChangeCallback(nameof(SyncedAxleAngle))] private Vector3 AxleAngle;
    [UdonSynced, FieldChangeCallback(nameof(SyncedHandleRigidPos))] private Vector3 HandleRigidPos;
    [UdonSynced, FieldChangeCallback(nameof(SyncedAxleRigidPos))] private Vector3 AxleRigidPos;
	[UdonSynced, FieldChangeCallback(nameof(SyncedIsRideOn))] private bool IsRideOn;

    [UdonSynced, FieldChangeCallback(nameof(SyncedFLTireMeshPosition))] private Vector3 FLTireMeshPosition;
    [UdonSynced, FieldChangeCallback(nameof(SyncedFLTireMeshRotation))] private Quaternion FLTireMeshRotation;
    [UdonSynced, FieldChangeCallback(nameof(SyncedFRTireMeshPosition))] private Vector3 FRTireMeshPosition;
    [UdonSynced, FieldChangeCallback(nameof(SyncedFRTireMeshRotation))] private Quaternion FRTireMeshRotation;
    [UdonSynced, FieldChangeCallback(nameof(SyncedRLTireMeshPosition))] private Vector3 RLTireMeshPosition;
    [UdonSynced, FieldChangeCallback(nameof(SyncedRLTireMeshRotation))] private Quaternion RLTireMeshRotation;
    [UdonSynced, FieldChangeCallback(nameof(SyncedRRTireMeshPosition))] private Vector3 RRTireMeshPosition;
    [UdonSynced, FieldChangeCallback(nameof(SyncedRRTireMeshRotation))] private Quaternion RRTireMeshRotation;
    [UdonSynced, FieldChangeCallback(nameof(SyncedbrakeTorque))] private float brakeTorque;
    [UdonSynced, FieldChangeCallback(nameof(SyncedmotoTorque))] private float motorTorque;
    [UdonSynced, FieldChangeCallback(nameof(SyncedBrakeOn))] private bool BrakeOn;

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

    public Vector3 SyncedFLTireMeshPosition
    {
        set
        {
            FLTireMeshPosition = value;
            FrontTire_L.transform.position = value;
        
        }
        get => FLTireMeshPosition;
    }
    public Quaternion SyncedFLTireMeshRotation
    {
        set
        {
            FLTireMeshRotation = value;
            FrontTire_L.transform.rotation = value;
        }
        get => FLTireMeshRotation;
    }
    public Vector3 SyncedFRTireMeshPosition
    {
        set
        {
            FRTireMeshPosition = value;
            FrontTire_R.transform.position = value;
        
        }
        get => FRTireMeshPosition;
    }
    public Quaternion SyncedFRTireMeshRotation
    {
        set
        {
            FRTireMeshRotation = value;
            FrontTire_R.transform.rotation = value;
        }
        get => FRTireMeshRotation;
    }
    public Vector3 SyncedRLTireMeshPosition
    {
        set
        {
            RLTireMeshPosition = value;
            RearTire_L.transform.position = value;
        
        }
        get => RLTireMeshPosition;
    }
    public Quaternion SyncedRLTireMeshRotation
    {
        set
        {
            RLTireMeshRotation = value;
            RearTire_L.transform.rotation = value;
        }
        get => RLTireMeshRotation;
    }
    public Vector3 SyncedRRTireMeshPosition
    {
        set
        {
            RRTireMeshPosition = value;
            RearTire_R.transform.position = value;
        
        }
        get => RRTireMeshPosition;
    }
    public Quaternion SyncedRRTireMeshRotation
    {
        set
        {
            RRTireMeshRotation = value;
            RearTire_R.transform.rotation = value;
        }
        get => RRTireMeshRotation;
    }

	public bool SyncedIsRideOn
    {
        set
        {
            IsRideOn = value;
        }
        get => IsRideOn;
    }

    public Vector3 SyncedLastPos
    {
        set
        {
            LastPos = value;
            m_thisRigid.MovePosition(LastPos);
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
    public float SyncedEngineIdlePitch
    {
        set
        {
            EngineIdlePitch = value;
            audioSrcEngineIdle.pitch = EngineIdlePitch;
        }
        get => EngineIdlePitch;
    }
    public float SyncedRoadNoiseVolume
    {
        set
        {
            RoadNoiseVolume = value;
            audioSrcRoadNoise.volume = RoadNoiseVolume;
        }
        get => RoadNoiseVolume;
    }
    
    public float SyncedTireSteerAngle
    {
        set
        {
            TireSteerAngle = value;
            updateWheelAngle(TireSteerAngle);
        }
        get => TireSteerAngle;
    }

    public Vector3 SyncedHandleAngle
    {
        set
        {
            HandleAngle = value;
            HandleGameobject.transform.localEulerAngles = HandleAngle;
        }
        get => HandleAngle;
    }
    public Vector3 SyncedAxleAngle
    {
        set
        {
            AxleAngle = value;
            AxleGameObject.transform.localEulerAngles = AxleAngle;
        }
        get => AxleAngle;
    }

    public float SyncedLastSpeed
    {
        set
        {   
            _LastSpeed = value;
            updateMotorTorque(_LastSpeed);
        }
        get => _LastSpeed;
    }

    public Vector3 SyncedHandleRigidPos
    {
        set
        {
            HandleRigidPos = value;
            m_HandleRigid.MovePosition(HandleRigidPos);
        }
        get => HandleRigidPos;
    }
    public Vector3 SyncedAxleRigidPos
    {
        set
        {
            AxleRigidPos = value;
            m_AxleRigid.MovePosition(AxleRigidPos);
        }
        get => AxleRigidPos;
    }
    
    // Start is called before the first frame update
    private void setupComponent()
    {
        m_thisRigid = GetComponent<Rigidbody>();
        m_HandleRigid = HandleGameobject.GetComponent<Rigidbody>();
        m_AxleRigid = AxleGameObject.GetComponent<Rigidbody>();
        wcFrontL = FrontWheel_L.GetComponent<WheelCollider>();
        wcFrontR = FrontWheel_R.GetComponent<WheelCollider>();
        wcRearL = RearWheel_L.GetComponent<WheelCollider>();
        wcRearR = RearWheel_R.GetComponent<WheelCollider>();
        
    }
    void Start()
    {
        setupComponent();

        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            _LastSpeed = 0u;
            if(bDebugMode == false)
            {
                IsRideOn = false;
            }
            else
            {
                IsRideOn = true;
            }
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
    }

    private void resetHande_Axle_Position()
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
                Vector3 EulerAngles = HandleGameobject.transform.localEulerAngles.z * Vector3.forward;
                HandleAngle = EulerAngles;
                HandleGameobject.transform.localEulerAngles = HandleAngle;
            }
            if(m_AxleRigid != null)
            {
                if(AxlePickup.pickUped == false)
                {
                    // TransformPoint : ローカル空間からワールド空間へ position を変換します。
                    AxleRigidPos = this.transform.TransformPoint(m_defaultPosAxle);
                    m_AxleRigid.MovePosition(AxleRigidPos);
                }
                Vector3 EulerAngles = AxleGameObject.transform.localEulerAngles.x * Vector3.right;
                AxleAngle = EulerAngles;
                AxleGameObject.transform.localEulerAngles = AxleAngle;
            }
        }
    }
    private void updateTireMeshByWheelCollider()
    {
        wcFrontL.GetWorldPose(out FLTireMeshPosition, out FLTireMeshRotation);

        FrontTire_L.transform.position = FLTireMeshPosition;
        FrontTire_L.transform.rotation = FLTireMeshRotation;

        wcFrontR.GetWorldPose(out FRTireMeshPosition, out FRTireMeshRotation);

        FrontTire_R.transform.position = FRTireMeshPosition;
        FrontTire_R.transform.rotation = FRTireMeshRotation;

        wcRearL.GetWorldPose(out RLTireMeshPosition, out RLTireMeshRotation);

        RearTire_L.transform.position = RLTireMeshPosition;
        RearTire_L.transform.rotation = RLTireMeshRotation;

        wcRearR.GetWorldPose(out RRTireMeshPosition, out RRTireMeshRotation);

        RearTire_R.transform.position = RRTireMeshPosition;
        RearTire_R.transform.rotation = RRTireMeshRotation;
    }

    private void updateWheelAngle(float angle)
    {
        wcFrontL.steerAngle = angle;
        wcFrontR.steerAngle = angle;
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

    private void LateUpdate()
    {
        if((IsRideOn != false) && (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)))
        {           
            if(Networking.IsOwner(Networking.LocalPlayer, this.gameObject) == true)
            {
                if(audioSrcEngineIdle != null)
                {
                    // Update engine idle sound pitch
                    float fp = Mathf.Abs(_LastSpeed)/ maxMotorTorque;
                    fp = Mathf.Clamp(4 * fp, 1.0f, 4.0f);
                    EngineIdlePitch = fp;
					audioSrcEngineIdle.pitch = EngineIdlePitch;
                }

                if(audioSrcRoadNoise != null)
                {
                    // Update road noise sound volume
                    float fp = Mathf.Abs(_LastSpeed)/ maxMotorTorque;
                    RoadNoiseVolume = fp;
					audioSrcRoadNoise.volume = RoadNoiseVolume;
                }
            }

            // Update speed meter
            int temp = Convert.ToInt32(Speedkmh);
            SpeedMeter.text = temp.ToString();
        }
    }
    private void FixedUpdate()
    {
        if((IsRideOn != false) && (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)))
        {
            Speedkmh = m_thisRigid.velocity.magnitude * 3.6f;
            resetHande_Axle_Position();
            updateTireMeshByWheelCollider();
            
            LastPos = m_thisRigid.position;
            LastRotation = m_thisRigid.rotation;

            TireSteerAngle = -getSteerAngle();

            // ホイールへの回転角の適用
            updateWheelAngle(TireSteerAngle);

            float EulerAnglesZ = - AxleGameObject.transform.localEulerAngles.x;
            if(EulerAnglesZ != 0)
            {   // アクセル回転角のmotorTorqueへの適用
                float radians = EulerAnglesZ / 180 * Mathf.PI; // ラジアンに変換
                float Speed = maxMotorTorque * Mathf.Clamp(axleResponse * Mathf.Sin(radians), -1.0f, 1.0f);
                _LastSpeed = Speed;

                updateMotorTorque(_LastSpeed);
                
                //Debug.Log ("Speed = " + Speed);
            }
            RequestSerialization();
        }
    }

    public void updateMotorTorque(float Torque)
    {
        if(BrakeOn == false)
        {
            wcFrontL.motorTorque = Torque;
            wcFrontR.motorTorque = Torque;
            wcRearL.motorTorque = Torque;
            wcRearR.motorTorque = Torque;
            
            
            float fp = 1.0f + downForceRate*(Speedkmh/downForceSpeedTh);

            WheelFrictionCurve wfc = wcFrontL.forwardFriction;
            wfc.stiffness = fp;
            wcFrontL.forwardFriction = wfc;

            wfc = wcFrontR.forwardFriction;
            wfc.stiffness = fp;
            wcFrontR.forwardFriction = wfc;

            wfc = wcRearL.forwardFriction;
            wfc.stiffness = fp;
            wcRearL.forwardFriction = wfc;

            wfc = wcRearR.forwardFriction;
            wfc.stiffness = fp;
            wcRearR.forwardFriction = wfc;

        }
    }
    public void updateBrakeTorque(float Torque)
    {
        wcFrontL.brakeTorque = Torque;
        wcFrontR.brakeTorque = Torque;
        wcRearL.brakeTorque = Torque;
        wcRearR.brakeTorque = Torque;
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
        if((IsRideOn != false) && (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)))
        {
            BrakeOn = value;
            if(value == false)
            {
                brakeTorque = 0.0f;
            }
            else
            {
                brakeTorque = maxMotorTorque*2.0f;
                updateMotorTorque(0.0f);
            }
            updateBrakeTorque(brakeTorque);
        }
    }
    public override void OnStationEntered()
    {   // OnStationEnteredは全プレイヤーから呼び出される
        m_HandleRigid.rotation = m_thisRigid.rotation;
        m_AxleRigid.rotation = m_thisRigid.rotation;
        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            resetHande_Axle_Position();
			_LastSpeed = 0.0f;
            IsRideOn = true;
        }
        m_thisRigid.velocity = Vector3.zero;
        HandleGameobject.transform.localEulerAngles = Vector3.zero;
        AxleGameObject.transform.localEulerAngles = Vector3.zero;
        wcFrontL.motorTorque  = 0.0f;
        wcFrontR.motorTorque  = 0.0f;
        wcRearL.motorTorque  = 0.0f;
        wcRearR.motorTorque  = 0.0f;
        wcFrontL.steerAngle = 0.0f;
        wcFrontR.steerAngle = 0.0f;

        StationEnterSound();

        RequestSerialization();
    }

    public override void OnStationExited()
    {   // OnStationExitedは全プレイヤーから呼び出される
        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            IsRideOn = false;
            
            resetHande_Axle_Position();
            _LastSpeed = 0.0f;
        }
        
        HandleGameobject.transform.localEulerAngles = Vector3.zero;
        AxleGameObject.transform.localEulerAngles = Vector3.zero;
        m_HandleRigid.rotation = m_thisRigid.rotation;
        m_AxleRigid.rotation = m_thisRigid.rotation;
        m_thisRigid.velocity = Vector3.zero;
        
        wcFrontL.motorTorque  = 0.0f;
        wcFrontR.motorTorque  = 0.0f;
        wcRearL.motorTorque  = 0.0f;
        wcRearR.motorTorque  = 0.0f;
        wcFrontL.steerAngle = 0.0f;
        wcFrontR.steerAngle = 0.0f;
        StationExitSound();
        RequestSerialization();
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
