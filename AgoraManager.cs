using System;
using System.Collections.Generic;
using agora_gaming_rtc;
using UnityEngine;

public enum AgoraManagerStatus { None, Ready, Joined }

public enum AudioQualityTypeAndCategory { NormalBradcast, MusicMono, MusicStereo, HQMono, HQStereo }

public class AgoraManager : MonoBehaviour
{
    public static AgoraManager main;

    private IRtcEngine mRtcEngine;
    public IRtcEngine IRtcEngine { get { return mRtcEngine; } }
    [Header("AgoraManager")]
    [SerializeField]
    protected string appId;

    public AgoraManagerStatus _agoraManagerStatus;
    public AgoraManagerStatus Status { get { return _agoraManagerStatus; } set { _agoraManagerStatus = value; AgoraManagerStatusChange(); AgoraManagerStatusChanged(_agoraManagerStatus); } }
    public static event Action<AgoraManagerStatus> AgoraManagerStatusChanged = delegate { };

    [SerializeField]
    protected bool loadOnAwake = true;
    [SerializeField]
    protected bool loadOnStart = true;
    [SerializeField]
    protected bool loadOnJoin = true;

    [Header("AgoraBehaviours")]
    [SerializeField]
    protected AgoraBehaviour _actualAgoraBehaviour;
    public AgoraBehaviour ActualAgoraBehaviour { get { return _actualAgoraBehaviour; } set { _actualAgoraBehaviour = value; } }


    [Header("Join")]
    public string channelId;
    public string myHexIdToSet;
    [SerializeField]
    protected uint mId;
    public uint MyId { get { return mId; } }
    [SerializeField]
    protected bool joinNow;
    [SerializeField]
    protected bool leaveNow;

    public bool enableAudioOnJoin = false;

    public bool enableVideoOnJoin = true;
    public bool enableVideoObserverOnJoin = true;

    public static event OnJoinChannelSuccessHandler JoinChannelSuccess = delegate { };
    public static event OnUserJoinedHandler UserJoined = delegate { };
    public static event OnUserOfflineHandler UserOffline = delegate { };

    [Header("ChannelProfile")]
    public CHANNEL_PROFILE channelProfile = CHANNEL_PROFILE.CHANNEL_PROFILE_COMMUNICATION;
    public CLIENT_ROLE channelBroadcastingRole = CLIENT_ROLE.AUDIENCE;

    [Header("Join")]
    public AudioQualityTypeAndCategory audioQualityTypeAndCategory = AudioQualityTypeAndCategory.NormalBradcast;

    [Header("Stream")]
    public bool useStream;
    public bool streamReliable;
    public bool streamOrdered;
    [SerializeField]
    protected int streamID;
    [SerializeField]
    protected bool testUpdateTextStream;
    int i = 0;
    [SerializeField]
    protected bool debugStream;

    public static event OnStreamMessageHandler StreamMessage = delegate { };
    public static event OnStreamPublishedHandler StreamPublished = delegate { };
    public static event OnStreamUnpublishedHandler StreamUnpublished = delegate { };
    public static event OnStreamMessageErrorHandler StreamMessageError = delegate { };

    [Header("Utils")]
    public bool muteOnPause;
    [SerializeField]
    protected bool _amIMute;
    public bool AmIMute { get { return _amIMute; } protected set { _amIMute = value; MuteToggled(_amIMute); } }
    public static event Action<bool> MuteToggled = delegate { };
    [Space(10)]
    public bool muteAllOnPause;
    [SerializeField]
    protected bool _isAllMute;
    public bool IsAllMute { get { return _isAllMute; } protected set { _isAllMute = value; MuteAllToggled(_isAllMute); } }
    public static event Action<bool> MuteAllToggled = delegate { };

    [Space(10)]
    [SerializeField]
    protected bool _eco;
    public bool Eco { get { return _eco; } protected set { _eco = value; EcoToggled(_eco); } }
    public static event Action<bool> EcoToggled = delegate { };


    [Header("Fixs")]
    public bool useIosFix = true;
    
    [Header("Audio Editor")]
    [SerializeField]
    protected bool enableAudio;
    [SerializeField]
    protected bool disableAudio;  

    [Header("Video Editor")]
    [SerializeField]
    protected bool enableVideo;
    [SerializeField]
    protected bool disableVideo;

    [Header("Reco Volume")]
    public int recoVolume = 100;
    [SerializeField]
    protected bool _amIRecoMute;
    public bool AmIRecoMute { get { return _amIRecoMute; } protected set { _amIRecoMute = value; RecoMuteToggled(_amIMute); } }
    public static event Action<bool> RecoMuteToggled = delegate { };

    [Header("Stats")]
    public bool statsTemp;
    public static event OnRemoteAudioStatsHandler RemoteAudioStatsUpdated = delegate { };   


    [Header("Devices")]
    [SerializeField]
    protected AgoraAudioDeviceData actualAudioRecordingDevice;
    public AgoraAudioDeviceData ActualAudioRecordingDevice { get { return actualAudioRecordingDevice; } }
    [SerializeField]
    protected bool changeRecordingDevice;
    [SerializeField]
    protected List<AgoraAudioDeviceData> lastAgoraAudioRecordingDeviceData = new List<AgoraAudioDeviceData>();
    [SerializeField]
    protected bool updateAgoraAudioRecordingDeviceData;   
    [Space(10)]
    [SerializeField]
    protected AgoraAudioDeviceData actualAudioPlaybackDevice;
    [SerializeField]
    protected bool changePlaybackDevice;
    [SerializeField]
    protected List<AgoraAudioDeviceData> lastAgoraAudioPlaybackDeviceData = new List<AgoraAudioDeviceData>();
    [SerializeField]
    protected bool updateAgoraAudioPlaybackDeviceData;  
    [Space(10)]
    [SerializeField]
    protected AgoraVideoDeviceData actualVideoDevice;
    [SerializeField]
    protected bool changeVideoDevice;
    [SerializeField]
    protected List<AgoraVideoDeviceData> lastAgoraVideoDeviceData = new List<AgoraVideoDeviceData>();
    [SerializeField]
    protected bool updateAgoraVideoDeviceData;


    [Header("Debug")]
    public bool simulatePause;

    //Editor
    private void OnValidate()
    {
        if (joinNow)
        {
            Join(channelId, myHexIdToSet);
            joinNow = false;
        }
        if (leaveNow)
        {
            Leave();
            leaveNow = false;
        }

        if (enableVideo)
        {
            EnableVideo(true);
            enableVideo = false;
        }

        if (disableVideo)
        {
            EnableVideo(false);
            disableVideo = false;
        }

        if (enableAudio)
        {
            EnableAudio(true);
            enableAudio = false;
        }

        if (disableAudio)
        {
            EnableAudio(false);
            disableAudio = false;
        }

        if (changeRecordingDevice)
        {
            ChangeRecordingDevice();
            changeRecordingDevice = false;
        }
        if (updateAgoraAudioRecordingDeviceData)
        {
            GetAudioRecordingDeviceList();
            updateAgoraAudioRecordingDeviceData = false;
        }
        if (changePlaybackDevice)
        {
            ChangePlaybackDevice();
            changePlaybackDevice = false;
        }
        if (updateAgoraAudioPlaybackDeviceData)
        {
            GetAudioPlaybackDeviceList();
            updateAgoraAudioPlaybackDeviceData = false;
        }
        if (changeVideoDevice)
        {
            ChangeVideoDevice();
            changeVideoDevice = false;
        }
        if (updateAgoraVideoDeviceData)
        {
            GetVideoDeviceList();
            updateAgoraVideoDeviceData = false;
        }
    }

    //Mono 
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            if (muteOnPause) MuteLocalAudioStream(true);
        }
    }
    private void OnApplicationFocus(bool focus)
    {
        if (Application.isEditor)
        {
            if (simulatePause)
            {
                OnApplicationPause(!focus);
            }
        }
    }

    private void Awake()
    {
        main = this;

        if (loadOnAwake)
        {
            LoadEngine(appId);
        }
    }
    private void OnDestroy()
    {
        Leave();
        UnloadEngine();
    }
    private void OnApplicationQuit()
    {
        Leave();
        UnloadEngine();
    }

    private void Start()
    {
        //Boh();

        if (loadOnStart)
        {
            if (Status == AgoraManagerStatus.None) LoadEngine(appId);
        }
    }

    private void Update()
    {
        i++;
        if (testUpdateTextStream)
        {
            SendStreamMessage("TESTTESTOTESTTESTOTESTTESTOTESTTESTO  + " + i);
        }
    }

    //Inner
    protected void AgoraManagerStatusChange()
    {
        switch (Status)
        {
            case AgoraManagerStatus.None:
                break;
            case AgoraManagerStatus.Ready:
                UpdateCurrentRecordingDevice();
                break;
            case AgoraManagerStatus.Joined:
                UpdateCurrentRecordingDevice();
                break;
            default:
                break;
        }

    }


    //Main
    public void LoadEngine(string appId = null)
    {
        if (appId == null) appId = this.appId;
        // start sdk
        Debug.Log("initializeEngine");

        if (mRtcEngine != null)
        {
            Debug.Log("Engine exists. Please unload it first!");
            return;
        }

        // init engine
        mRtcEngine = IRtcEngine.GetEngine(appId);

        // enable log
        mRtcEngine.SetLogFilter(LOG_FILTER.DEBUG | LOG_FILTER.INFO | LOG_FILTER.WARNING | LOG_FILTER.ERROR | LOG_FILTER.CRITICAL);


        // set callbacks (optional)
        mRtcEngine.OnJoinChannelSuccess = OnJoinChannelSuccess;
        mRtcEngine.OnUserJoined = OnUserJoined;
        mRtcEngine.OnUserOffline = OnUserOffline;


        mRtcEngine.OnClientRoleChanged = OnClientRoleChanged;

        if (useStream)
        {
            mRtcEngine.OnStreamMessage = OnStreamMessage;
            mRtcEngine.OnStreamPublished = OnStreamPublished;
            mRtcEngine.OnStreamUnpublished = OnStreamUnpublished;
            mRtcEngine.OnStreamMessageError = OnStreamMessageError;
        }

        //Mute Audio
        mRtcEngine.OnUserMutedAudio = OnUserMuteAudio;

        mRtcEngine.OnAudioRouteChanged = OnAudioRouteChanged;

        //Stats
        mRtcEngine.OnRemoteAudioStats = OnRemoteAudioStats;

        Status = AgoraManagerStatus.Ready;
    }

    // unload agora engine
    public void UnloadEngine()
    {
        Debug.Log("calling unloadEngine");

        /*
        IVideoDeviceManager videoDeviceManager = mRtcEngine.GetVideoDeviceManager();
        videoDeviceManager.ReleaseAVideoDeviceManager();
        */

        // delete
        if (mRtcEngine != null)
        {
            IRtcEngine.Destroy();  // Place this call in ApplicationQuit
            mRtcEngine = null;
        }



        Status = AgoraManagerStatus.None;

    }

    public void Join(string channel, string hexId, bool forceRejoin = true) //IN FUTURO DIVIDI QUESTO CRIPT E METTI UN MANAGER PER CHANNEL E PER UTENTI .. e opzioni su cosa faree JOIN
    {
        Join(channel, forceRejoin, hexId);
    }

    public void Join(string channel = "", bool forceRejoin = true, string hexId = "") //IN FUTURO DIVIDI QUESTO CRIPT E METTI UN MANAGER PER CHANNEL E PER UTENTI .. e opzioni su cosa faree JOIN
    {
        Debug.Log("calling join (channel = " + channel + ")");

        if (channel == "")
            channel = channelId;

        if (string.IsNullOrEmpty(hexId))
            hexId = myHexIdToSet;


        uint uintId = 0;
        if (!string.IsNullOrEmpty(hexId))
        {
            hexId = "";

            //Parser non va al momento
            /*
            byte[] bytes = Encoding.ASCII.GetBytes(hexId);

            uintId = BitConverter.ToUInt32(bytes, 0);

            //hexId = hexId.Replace("-", "");
            //uintId = Convert.ToUInt32(hexId, 16);
            Debug.Log(uintId);

            byte[] bytes2 = BitConverter.GetBytes(uintId);
            string s = System.Text.Encoding.ASCII.GetString(bytes2);

            Debug.Log("carlo " + s);
          

            int intValue = 145354545;
            // Convert integer 182 as a hex in a string variable
            string hexValue = intValue.ToString("X");
            // Convert the hex string back to the number
            int intAgain = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);

            Debug.Log(intValue);
            Debug.Log(hexValue);
            Debug.Log(intAgain);
            
            hexId = hexId.Replace("-", "").ToUpper();

            Debug.Log(hexId);

            long intdssdfsdfsdfdf = long.Parse(hexId, System.Globalization.NumberStyles.HexNumber);
            // Store integer 182
            ///uintId =  (uint)intAgain;

            //int intValue = (int) uintId;

            // Convert integer 182 as a hex in a string variable
            //string hexValue = intValue.ToString("X");
           
            Debug.Log(intdssdfsdfsdfdf);
            */
        }

        if (loadOnJoin)
        {
            if (Status == AgoraManagerStatus.None) LoadEngine(appId);
        }

        if (Status == AgoraManagerStatus.None)
        {
            return;
        }
        else
        {

            if (Status == AgoraManagerStatus.Joined)
            {
                if (forceRejoin)
                {
                    Debug.Log("ForceRejoin");
                    Leave();
                }
                else
                {
                    Debug.Log("AlreadyJoined");
                    return;
                }
            }

            if (mRtcEngine == null)
                return;


#if UNITY_IOS
            if (useIosFix)
            {
                mRtcEngine.SetParameters("{\"che.audio.keep.audiosession\":true}");
            }
#endif

            int result = mRtcEngine.SetChannelProfile(channelProfile);
            if (result < 0)
            {
                Debug.LogError("SetChannelProfile ERROR" + result);
                return;
            }

            if (channelProfile == CHANNEL_PROFILE.CHANNEL_PROFILE_LIVE_BROADCASTING)
            {
                mRtcEngine.SetClientRole(channelBroadcastingRole);
            }


            // enable video
            if (enableVideoOnJoin) EnableVideo(true);

            // enable audio
            if (enableAudioOnJoin) EnableAudio(true);

            // allow camera output callback
            if (enableVideoObserverOnJoin) EnableVideoObserver(true);


            //Dopo enable audio. per sicurezza
            if (channelProfile == CHANNEL_PROFILE.CHANNEL_PROFILE_LIVE_BROADCASTING)
            {
                switch (audioQualityTypeAndCategory)
                {
                    case AudioQualityTypeAndCategory.NormalBradcast:
                        mRtcEngine.SetAudioProfile(AUDIO_PROFILE_TYPE.AUDIO_PROFILE_MUSIC_STANDARD, AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_EDUCATION);
                        break;
                    case AudioQualityTypeAndCategory.MusicMono:
                        mRtcEngine.SetAudioProfile(AUDIO_PROFILE_TYPE.AUDIO_PROFILE_MUSIC_STANDARD, AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_GAME_STREAMING);
                        break;
                    case AudioQualityTypeAndCategory.MusicStereo:
                        mRtcEngine.SetAudioProfile(AUDIO_PROFILE_TYPE.AUDIO_PROFILE_MUSIC_STANDARD_STEREO, AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_GAME_STREAMING);
                        break;
                    case AudioQualityTypeAndCategory.HQMono:
                        mRtcEngine.SetAudioProfile(AUDIO_PROFILE_TYPE.AUDIO_PROFILE_MUSIC_HIGH_QUALITY, AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_GAME_STREAMING);
                        break;
                    case AudioQualityTypeAndCategory.HQStereo:
                        mRtcEngine.SetAudioProfile(AUDIO_PROFILE_TYPE.AUDIO_PROFILE_MUSIC_HIGH_QUALITY_STEREO, AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_GAME_STREAMING);
                        break;
                }
            }





            // join channel
            result = mRtcEngine.JoinChannel(channel, null, uintId);
            if (result < 0)
            {
                Debug.LogError("Join ERROR " + result);
                return;
            }

            channelId = channel;
            myHexIdToSet = hexId;

            if (useStream) //TO DO ///|CARL FINISCI
            {
                // Optional: if a data stream is required, here is a good place to create it
                streamID = mRtcEngine.CreateDataStream(streamReliable, streamOrdered);
                Debug.Log("initializeEngine done, data stream id = " + streamID);
                if (streamID < 0)
                {
                    Debug.LogError("CreateDataStream ERROR " + streamID);
                }
            }

        }

    }

    public void Leave()
    {
        Debug.Log("calling leave");

        if (mRtcEngine == null)
            return;

        //EnableAudio(false);
        //EnableVideo(false);

        // leave channel
        int result = mRtcEngine.LeaveChannel();
        if (result < 0)
        {
            Debug.LogError("LeaveChannel ERROR" + result);
        }

        // deregister video frame observers in native-c code
        EnableVideoObserver(false);

        Status = AgoraManagerStatus.Ready;
    }


    //Callbacks Generic
    private void OnJoinChannelSuccess(string channelName, uint uid, int elapsed)
    {
        Debug.Log("JoinChannelSuccessHandler: uid = " + uid);

        mId = uid;

        JoinChannelSuccess(channelName, uid, elapsed);

        Status = AgoraManagerStatus.Joined;

        //Debug.Log(" mRtcEngine.GetCallId();: uid = " + mRtcEngine.GetCallId());


        /*
        GameObject textVersionGameObject = GameObject.Find("VersionText");
        textVersionGameObject.GetComponent<Text>().text = "SDK Version : " + getSdkVersion();
        */

    }



    // When a remote user joined, this delegate will be called. Typically
    // create a GameObject to render video on it
    private void OnUserJoined(uint uid, int elapsed)
    {
        Debug.Log("onUserJoined: uid = " + uid + " elapsed = " + elapsed);
        // this is called in main thread

        UserJoined(uid, elapsed);

        //////////////////////////////////////////////////////////////////////////////////////// FAI MANAGER CHE GESTISCE COSA E' ENTRATO

        /*
        // find a game object to render video stream from 'uid'
        GameObject go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            return; // reuse
        }

        // create a GameObject and assign to this new user
        VideoSurface videoSurface = makeImageSurface(uid.ToString());
        if (!ReferenceEquals(videoSurface, null))
        {
            // configure videoSurface
            videoSurface.SetForUser(uid);
            videoSurface.SetEnable(true);
            videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
            videoSurface.SetGameFps(30);
        }*/

    }


    // When remote user is offline, this delegate will be called. Typically
    // delete the GameObject for this user
    private void OnUserOffline(uint uid, USER_OFFLINE_REASON reason)
    {
        // remove video stream
        Debug.Log("onUserOffline: uid = " + uid + " reason = " + reason);


        UserOffline(uid, reason);
        /*// this is called in main thread
        GameObject go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            Object.Destroy(go);
        }*/
    }

    //ChannelProfile
    public void ChannelRole(CLIENT_ROLE clientRole)
    {
        mRtcEngine.SetClientRole(clientRole);
        channelBroadcastingRole = clientRole;
    }
    protected void OnClientRoleChanged(CLIENT_ROLE_TYPE oldRole, CLIENT_ROLE_TYPE newRole)
    {
        //In the Live Broadcast profile, when a user switches user roles after joining a channel, a successful setClientRole method call triggers the following callbacks

        //The local client: OnClientRoleChangedHandler
        //The remote client: OnUserJoinedHandler or OnUserOfflineHandler(BECOME_AUDIENCE)

        Debug.Log("oldRole " + oldRole + " newRole " + newRole);

    }

    //Callbacks Stream
    protected void OnStreamMessageError(uint userId, int streamId, int code, int missed, int cached) // CREARE UNA SEZIONE DI GESTIONE DEi messaggi ????
    {
        if (debugStream) Debug.LogError("OnStreamMessageError userId " + userId + " streamId " + streamId + " code " + code + " missed " + missed + " cached " + cached);

        StreamMessageError(userId, streamId, code, missed, cached);

    }
    protected void OnStreamMessage(uint userId, int streamId, string data, int length) // CREARE UNA SEZIONE DI GESTIONE DEi messaggi ????
    {
        //* @param userId The user ID of the remote user sending the message.
        //* @param streamId The stream ID.
        //* @param data The data received by the local user.
        //* @param length The length of the data in bytes.

        if (debugStream) Debug.Log("OnStreamMessage userId " + userId + " streamId " + streamId + " data " + data + " length " + length);

        StreamMessage(userId, streamId, data, length);
    }
    protected void OnStreamPublished(string url, int error) // CREARE UNA SEZIONE DI GESTIONE DEi messaggi ????
    {
        if (debugStream) Debug.Log("OnStreamPublished url " + url + " error " + error);

        StreamPublished(url, error);
    }
    protected void OnStreamUnpublished(string url) // CREARE UNA SEZIONE DI GESTIONE DEi messaggi ????
    {
        if (debugStream) Debug.Log("OnStreamUnpublished url " + url);
        StreamUnpublished(url);
    }

    //Audio
    public void AdjustRecordingSignalVolume(int volume)
    {
        if (!AmIRecoMute)
        {
            if (mRtcEngine != null)
            {
                int result = mRtcEngine.AdjustRecordingSignalVolume(volume);
                if (result < 0)
                {
                    Debug.LogError("AdjustRecordingSignalVolume ERROR" + result);
                }
            }
        }
        else
        {
            if (mRtcEngine != null)
            {
                int result = mRtcEngine.AdjustRecordingSignalVolume(0); //Risettto sempre a zero pe rsicurezza
                if (result < 0)
                {
                    Debug.LogError("AdjustRecordingSignalVolume ERROR" + result);
                }
            }
        }


        if (volume != 0)
        {
            recoVolume = volume;
        }
    }
    public void MuteReco(bool mute) //Per differenziare in mixing
    {
        if (mute)
        {
            AdjustRecordingSignalVolume(0);
            AmIRecoMute = mute;
        }
        else
        {
            AmIRecoMute = mute;
            AdjustRecordingSignalVolume(recoVolume);
        }
    }
    public void AdjustPlaybackSignalVolume(int volume)
    {
        int result = mRtcEngine.AdjustPlaybackSignalVolume(volume);
        if (result < 0)
        {
            Debug.LogError("AdjustPlaybackSignalVolume ERROR" + result);
        }
    }
    public void AdjustUserPlaybackSignalVolume(int volume)
    {
        Debug.LogError("AdjustUserPlaybackSignalVolume ERROR");
        //mRtcEngine.AdjustPlaybackSignalVolume();
    }
    public void MuteVideo(bool mute)
    {
        int result = mRtcEngine.MuteAllRemoteVideoStreams(mute);
        if (result < 0)
        {
            Debug.LogError("Mute ERROR" + result);
        }
    }
    public void MuteAudio(bool mute)
    {
        int result = mRtcEngine.MuteAllRemoteAudioStreams(mute);
        if (result < 0)
        {
            Debug.LogError("Mute ERROR" + result);
        }
    }
    public void MuteLocalAudioStream(bool mute)
    {
        if (mRtcEngine != null)
        {
            int result = mRtcEngine.MuteLocalAudioStream(mute);
            if (result < 0)
            {
                Debug.LogError("Mute ERROR" + result);

                IsAllMute = false;
            }
        }

        IsAllMute = mute;
    }
    public void MuteAllRemoteAudioStreams(bool mute)
    {
        int result = mRtcEngine.MuteAllRemoteAudioStreams(mute);
        if (result < 0)
        {
            Debug.LogError("Mute ERROR" + result);
        }
    }

    public void MuteAll(bool mute)
    {
        MuteAudio(mute);
        MuteVideo(mute);
    }
    public int EnableAudio(bool enable)
    {
        int result = -1;
        if (mRtcEngine != null)
        {
            if (enable)
            {
                result = mRtcEngine.EnableAudio();
                if (result < 0)
                {
                    Debug.LogError("EnableAudio ERROR" + result);
                }
            }
            else
            {
                result = mRtcEngine.DisableAudio();
                if (result < 0)
                {
                    Debug.LogError("DisableAudio ERROR" + result);
                }
            }
        }

        return result;
    }

    public int SetExternalAudio(bool enabled, int sampleRate, int channels = 1)//@param sampleRate Sets the sample rate(Hz) of the external audio source, which can be set as 8000, 16000, 32000, 44100, or 48000 Hz.
    {
        return mRtcEngine.SetExternalAudioSource(enabled, sampleRate, channels);
    }
    public int PushAudioFrame(AudioFrame audioFrame)
    {
        return mRtcEngine.PushAudioFrame(audioFrame);
    }

    //Eco
    AudioRecordingDeviceManager audioRecordingDeviceManagerEco = null;
    public int StartEchoTest()
    {
        int result = -1;
        if (mRtcEngine != null)
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                //https://docs.agora.io/en/Voice/test_switch_device_unity?platform=Unity
                //ONLO FOR IOS and ANDROID????
                result = mRtcEngine.StartEchoTest(3);
                if (result < 0)
                {
                    Eco = false;
                    Debug.LogError("StartEchoTest ERROR" + result);
                }
                else
                {
                    Eco = true;
                }
            }
            else
            {
                audioRecordingDeviceManagerEco = (AudioRecordingDeviceManager)mRtcEngine.GetAudioRecordingDeviceManager();
                mRtcEngine.OnVolumeIndication = OnVolumeIndicationHandler;
                audioRecordingDeviceManagerEco.CreateAAudioRecordingDeviceManager();
                mRtcEngine.EnableAudioVolumeIndication(300, 3, true);
                result = audioRecordingDeviceManagerEco.StartAudioRecordingDeviceTest(300);
                if (result < 0)
                {
                    Eco = false;
                    Debug.LogError("StartEchoTest ERROR" + result);
                }
                else
                {
                    Eco = true;
                }


                //audioRecordingDeviceManager.ReleaseAAudioRecordingDeviceManager();

                /*
                
                // Initializes the IRtcEngine.
                mRtcEngine = IRtcEngine.GetEngine(appId);
                mRtcEngine.OnVolumeIndication = OnVolumeIndicationHandler;
                // Retrieves the AudioRecordingDeviceManager object.
                AudioRecordingDeviceManager audioRecordingDeviceManager = (AudioRecordingDeviceManager)mRtcEngine.GetAudioRecordingDeviceManager();
                // Creates an AudioRecordingDeviceManager instance.
                audioRecordingDeviceManager.CreateAAudioRecordingDeviceManager();
                // Retrieves the total number of the indexed audio recording devices in the system.
                int count = audioRecordingDeviceManager.GetAudioRecordingDeviceCount();
                // Retrieves the device ID of the target audio recording device. The value of index should not more than the number retrieved from GetAudioRecordingDeviceCount.
                audioRecordingDeviceManager.GetAudioRecordingDevice(0, ref deviceNameA, ref deviceIdA);
                // Sets the audio recording device using the device ID.
                audioRecordingDeviceManager.SetAudioRecordingDevice(deviceIdA);
                // Enables the audio volume callback.
                mRtcEngine.EnableAudioVolumeIndication(300, 3, true);
                // Starts the audio recording device test.
                audioRecordingDeviceManager.StartAudioRecordingDeviceTest(300);
                // Stops the audio recording device test.
                audioRecordingDeviceManager.StopAudioRecordingDeviceTest();
                // Releases AudioRecordingDeviceManager instance.
                audioRecordingDeviceManager.ReleaseAAudioRecordingDeviceManager();
                */
            }
        }
        else
        {
            Eco = false;
        }
        return result;
    }
    public int StopEchoTest()
    {
        int result = -1;
        if (mRtcEngine != null)
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                result = mRtcEngine.StopEchoTest();
                if (result < 0)
                {
                    Debug.LogError("StopEchoTest ERROR" + result);
                }
            }
            else
            {
                audioRecordingDeviceManagerEco = (AudioRecordingDeviceManager)mRtcEngine.GetAudioRecordingDeviceManager();
                mRtcEngine.OnVolumeIndication = OnVolumeIndicationHandler;
                audioRecordingDeviceManagerEco.CreateAAudioRecordingDeviceManager();


                result = audioRecordingDeviceManagerEco.StopAudioRecordingDeviceTest();
                if (result < 0)
                {
                    Debug.LogError("StopEchoTest ERROR" + result);
                }

                audioRecordingDeviceManagerEco.ReleaseAAudioRecordingDeviceManager();

            }
        }

        Eco = false;

        return result;
    }

    //InVolumeInicator
    public static Action<AudioVolumeInfo[], int, int> OnVolumeIndicationUpdate = delegate { };
    protected void OnVolumeIndicationHandler(AudioVolumeInfo[] speakers, int speakerNumber, int totalVolume)
    {
        //Debug.Log("speakerNumber " + speakerNumber + " totalVolume " + totalVolume);
        OnVolumeIndicationUpdate(speakers, speakerNumber, totalVolume);
    }

    //Velume
    public void StartGetMicVolumeIndication()
    {
        // Initializes the IRtcEngine.
        if (mRtcEngine != null)
        {
            mRtcEngine.OnVolumeIndication = OnVolumeIndicationHandler;
            mRtcEngine.EnableAudioVolumeIndication(50, 3, true);
        }       
    }

    public void StopMicVolumeIndication()
    {
        if (mRtcEngine != null)
        {
            mRtcEngine.EnableAudioVolumeIndication(-1, 3, true);
        }
    }

    //Audio Device
    public List<AgoraAudioDeviceData> GetAudioRecordingDeviceList()
    {
        Debug.Log("CARLO 1");

        lastAgoraAudioRecordingDeviceData = new List<AgoraAudioDeviceData>();

        if (mRtcEngine != null)
        {

            Debug.Log("CARLO 2");
            AudioRecordingDeviceManager audioDeviceManager = AudioRecordingDeviceManager.GetInstance(mRtcEngine);

            if (audioDeviceManager.CreateAAudioRecordingDeviceManager())
            {
                Debug.Log("CARLO 3");

                Debug.Log("audioDeviceManager.GetAudioDeviceCount() 2" + audioDeviceManager.GetAudioRecordingDeviceCount());

                for (int i = 0; i < audioDeviceManager.GetAudioRecordingDeviceCount(); i++)
                {
                    string deviceId = "";
                    string deviceName = "";

                    audioDeviceManager.GetAudioRecordingDevice(i, ref deviceName, ref deviceId);

                    lastAgoraAudioRecordingDeviceData.Add(new AgoraAudioDeviceData(deviceId, deviceName));
                }


                //Release
                int result = audioDeviceManager.ReleaseAAudioRecordingDeviceManager();
                if (result < 0)
                {
                    Debug.LogError("ReleaseAAudioRecordingDeviceManager ERROR" + result);
                }
            }
        }

        return lastAgoraAudioRecordingDeviceData;
    }
    public void UpdateCurrentRecordingDevice()
    {
        if (mRtcEngine != null)
        {
            AudioRecordingDeviceManager audioDeviceManager = AudioRecordingDeviceManager.GetInstance(mRtcEngine);

            if (audioDeviceManager.CreateAAudioRecordingDeviceManager())
            {
                string deviceId = "";
                string deviceName = "";
                audioDeviceManager.GetCurrentRecordingDeviceInfo(ref deviceName, ref deviceId);

                AgoraAudioDeviceData agoraAudioDeviceData = new AgoraAudioDeviceData();
                agoraAudioDeviceData.deviceId = deviceId;
                agoraAudioDeviceData.deviceName = deviceName;
                actualAudioRecordingDevice = agoraAudioDeviceData;

                //Release
                int result = audioDeviceManager.ReleaseAAudioRecordingDeviceManager();
                if (result < 0)
                {
                    Debug.LogError("ReleaseAAudioRecordingDeviceManager ERROR" + result);
                }
            }
        }
    }
    public void SetAudioRecordingDevice(AgoraAudioDeviceData agoraAudioDeviceData)
    {
        if (mRtcEngine != null)
        {
            AudioRecordingDeviceManager audioDeviceManager = AudioRecordingDeviceManager.GetInstance(mRtcEngine);

            Debug.Log("SetAudioDevice audioDeviceManager + " + audioDeviceManager);
            if (audioDeviceManager.CreateAAudioRecordingDeviceManager())
            {
                Debug.Log("SetAudioDevice audioDeviceManager 2 + " + audioDeviceManager);
                audioDeviceManager.SetAudioRecordingDevice(agoraAudioDeviceData.deviceId);

                //Release
                int result = audioDeviceManager.ReleaseAAudioRecordingDeviceManager();
                if (result < 0)
                {
                    Debug.LogError("ReleaseAAudioRecordingDeviceManager ERROR" + result);
                }
                actualAudioRecordingDevice = agoraAudioDeviceData;
            }
        }
    }
    public void ChangeRecordingDevice()
    {
        List<AgoraAudioDeviceData> agoraAudioRecordingDeviceData = GetAudioRecordingDeviceList();

        if (agoraAudioRecordingDeviceData.Count > 0)
        {
            bool done = false;
            int i = 0;
            foreach (AgoraAudioDeviceData agoraAudioDeviceData in agoraAudioRecordingDeviceData)
            {
                if (agoraAudioDeviceData.deviceId == actualAudioRecordingDevice.deviceId)
                {
                    done = true;
                    break;
                }
                i++;
            }

            if (!done)
            {
                i = 0;
            }
            else
            {
                i++;
                i %= agoraAudioRecordingDeviceData.Count;
            }

            SetAudioRecordingDevice(agoraAudioRecordingDeviceData[i]);
        }
    }
    public List<AgoraAudioDeviceData> GetAudioPlaybackDeviceList()
    {
        Debug.Log("CARLO 1");

        lastAgoraAudioPlaybackDeviceData = new List<AgoraAudioDeviceData>();

        if (mRtcEngine != null)
        {

            Debug.Log("CARLO 2");
            AudioPlaybackDeviceManager audioDeviceManager = AudioPlaybackDeviceManager.GetInstance(mRtcEngine);

            if (audioDeviceManager.CreateAAudioPlaybackDeviceManager())
            {
                Debug.Log("CARLO 3");

                Debug.Log("audioDeviceManager.GetAudioDeviceCount() 2" + audioDeviceManager.GetAudioPlaybackDeviceCount());

                for (int i = 0; i < audioDeviceManager.GetAudioPlaybackDeviceCount(); i++)
                {
                    string deviceId = "";
                    string deviceName = "";

                    audioDeviceManager.GetAudioPlaybackDevice(i, ref deviceName, ref deviceId);

                    lastAgoraAudioPlaybackDeviceData.Add(new AgoraAudioDeviceData(deviceId, deviceName));
                }


                //Release
                int result = audioDeviceManager.ReleaseAAudioPlaybackDeviceManager();
                if (result < 0)
                {
                    Debug.LogError("ReleaseAAudioPlaybackDeviceManager ERROR" + result);
                }
            }
        }

        return lastAgoraAudioPlaybackDeviceData;
    }
    public void SetAudioPlaybackDevice(AgoraAudioDeviceData agoraAudioDeviceData)
    {
        if (mRtcEngine != null)
        {
            AudioPlaybackDeviceManager audioDeviceManager = AudioPlaybackDeviceManager.GetInstance(mRtcEngine);

            Debug.Log("SetAudioDevice audioDeviceManager + " + audioDeviceManager);
            if (audioDeviceManager.CreateAAudioPlaybackDeviceManager())
            {
                Debug.Log("SetAudioDevice audioDeviceManager 2 + " + audioDeviceManager);
                audioDeviceManager.SetAudioPlaybackDevice(agoraAudioDeviceData.deviceId);

                //Release
                int result = audioDeviceManager.ReleaseAAudioPlaybackDeviceManager();
                if (result < 0)
                {
                    Debug.LogError("ReleaseAAudioPlaybackDeviceManager ERROR" + result);
                }

            }
        }
    }
    public void ChangePlaybackDevice()
    {

    }

    //Audio Callbacks
    protected void OnUserMuteAudio(uint uid, bool muted)
    {
        if (uid == mId)
        {
            AmIMute = muted;
        }
    }
    protected void OnAudioRouteChanged(AUDIO_ROUTE aUDIO_ROUTE)
    {
        Debug.Log("AUDIO ROOT " + aUDIO_ROUTE);
    }

    //Video
    public int EnableVideo(bool enable)
    {
        int result = -1;
        if (mRtcEngine != null)
        {
            if (enable)
            {
                result = mRtcEngine.EnableVideo();
                if (result < 0)
                {
                    Debug.LogError("EnableVideo ERROR" + result);
                }
            }
            else
            {
                result = mRtcEngine.DisableVideo();
                if (result < 0)
                {
                    Debug.LogError("DisableVideo ERROR" + result);
                }
            }
        }

        return result;
    }
    public int EnableLocalVideo(bool enable)
    {
        int result = -1;
        if (mRtcEngine != null)
        {
            result = mRtcEngine.EnableLocalVideo(enable);
            if (result < 0)
            {
                Debug.LogError("EnableLocalVideo ERROR" + result);
            }
        }

        return result;
    }

    public int SetExternalVideo(bool value)
    {
        return mRtcEngine.SetExternalVideoSource(value, false);
    }
    public int PushVideoFrame(ExternalVideoFrame externalVideoFrame)
    {
        return mRtcEngine.PushVideoFrame(externalVideoFrame);
    }
    public int SetVideoEncoderConfiguration(VideoEncoderConfiguration videoEncoderConfiguration)
    {
        return mRtcEngine.SetVideoEncoderConfiguration(videoEncoderConfiguration);
    }

    public List<AgoraVideoDeviceData> GetVideoDeviceList()
    {
        Debug.Log("CARLO 1");

        lastAgoraVideoDeviceData = new List<AgoraVideoDeviceData>();

        if (mRtcEngine != null)
        {

            Debug.Log("CARLO 2");
            VideoDeviceManager videoDeviceManager = VideoDeviceManager.GetInstance(mRtcEngine);

            if (videoDeviceManager.CreateAVideoDeviceManager())
            {
                Debug.Log("CARLO 3");

                Debug.Log("videoDeviceManager.GetVideoDeviceCount() 2" + videoDeviceManager.GetVideoDeviceCount());

                for (int i = 0; i < videoDeviceManager.GetVideoDeviceCount(); i++)
                {
                    string deviceId = "";
                    string deviceName = "";

                    videoDeviceManager.GetVideoDevice(i, ref deviceName, ref deviceId);

                    lastAgoraVideoDeviceData.Add(new AgoraVideoDeviceData(deviceId, deviceName));
                }


                //Release
                int result = videoDeviceManager.ReleaseAVideoDeviceManager();
                if (result < 0)
                {
                    Debug.LogError("ReleaseAVideoDeviceManager ERROR" + result);
                }
            }
        }

        return lastAgoraVideoDeviceData;
    }

    public void SetVideoDevice(AgoraVideoDeviceData agoraVideoDeviceData)
    {
        if (mRtcEngine != null)
        {
            VideoDeviceManager videoDeviceManager = VideoDeviceManager.GetInstance(mRtcEngine);

            Debug.Log("SetVideoDevice videoDeviceManager + " + videoDeviceManager);
            if (videoDeviceManager.CreateAVideoDeviceManager())
            {
                Debug.Log("SetVideoDevice videoDeviceManager 2 + " + videoDeviceManager);
                videoDeviceManager.SetVideoDevice(agoraVideoDeviceData.deviceId);

                //Release
                int result = videoDeviceManager.ReleaseAVideoDeviceManager();
                if (result < 0)
                {
                    Debug.LogError("ReleaseAVideoDeviceManager ERROR" + result);
                }

            }
        }
    }
    public void ChangeVideoDevice()
    {

    }


    //Video Observer
    public int EnableVideoObserver(bool enable)
    {
        int result = -1;
        if (mRtcEngine != null)
        {
            if (enable)
            {
                result = mRtcEngine.EnableVideoObserver();
                if (result < 0)
                {
                    Debug.LogError("EnableVideoObserver ERROR" + result);
                }
            }
            else
            {
                result = mRtcEngine.DisableVideoObserver();
                if (result < 0)
                {
                    Debug.LogWarning("DisableVideoObserver ERROR" + result);
                }
            }
        }
        return result;
    }

    //Stream
    public void SendStreamMessage(string message)
    {
        int result = mRtcEngine.SendStreamMessage(streamID, message);
        if (result < 0)
        {
            if (debugStream) Debug.LogError("SendStreamMessage ERROR" + result); ;
        }
        else
        {
            if (debugStream) Debug.Log("SendStreamMessage " + message);
        }
    }

    //Stats
    protected void OnRemoteAudioStats(RemoteAudioStats remoteAudioStats)
    {
        if (mRtcEngine != null)
        {
            RemoteAudioStatsUpdated(remoteAudioStats);
        }
    }

    //Utils
    public string GetSdkVersion()
    {
        string ver = IRtcEngine.GetSdkVersion();
        if (ver == "2.9.1.45")
        {
            ver = "2.9.2";  // A conversion for the current internal version#
        }
        return ver;
    }

}


[Serializable]
public struct AgoraVideoDeviceData
{
    public string deviceId;
    public string deviceName;

    public AgoraVideoDeviceData(string deviceId, string deviceName)
    {
        this.deviceId = deviceId;
        this.deviceName = deviceName;
    }
}

[Serializable]
public struct AgoraAudioDeviceData
{
    public string deviceId;
    public string deviceName;

    public AgoraAudioDeviceData(string deviceId, string deviceName)
    {
        this.deviceId = deviceId;
        this.deviceName = deviceName;
    }
}




/*
   // accessing GameObject in Scnene1
   // set video transform delegate for statically created GameObject
   public void Boh()
   {
       // Attach the SDK Script VideoSurface for video rendering
       GameObject quad = GameObject.Find("Quad");
       if (ReferenceEquals(quad, null))
       {
           Debug.Log("BBBB: failed to find Quad");
           return;
       }
       else
       {
           quad.AddComponent<VideoSurface>();
       }

       GameObject cube = GameObject.Find("Cube");
       if (ReferenceEquals(cube, null))
       {
           Debug.Log("BBBB: failed to find Cube");
           return;
       }
       else
       {
           cube.AddComponent<VideoSurface>();
       }
   }    
   public VideoSurface makePlaneSurface(string goName)
   {
       GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);

       if (go == null)
       {
           return null;
       }
       go.name = goName;
       // set up transform
       go.transform.Rotate(-90.0f, 0.0f, 0.0f);
       float yPos = Random.Range(3.0f, 5.0f);
       float xPos = Random.Range(-2.0f, 2.0f);
       go.transform.position = new Vector3(xPos, yPos, 0f);
       go.transform.localScale = new Vector3(0.25f, 0.5f, .5f);

       // configure videoSurface
       VideoSurface videoSurface = go.AddComponent<VideoSurface>();
       return videoSurface;
   }

   private const float Offset = 100;

   public VideoSurface makeImageSurface(string goName)
   {
       GameObject go = new GameObject();

       if (go == null)
       {
           return null;
       }

       go.name = goName;

       // to be renderered onto
       go.AddComponent<RawImage>();

       // make the object draggable
       go.AddComponent<UIElementDragger>();
       GameObject canvas = GameObject.Find("Canvas");
       if (canvas != null)
       {
           go.transform.parent = canvas.transform;
       }
       // set up transform
       go.transform.Rotate(0f, 0.0f, 180.0f);
       float xPos = Random.Range(Offset - Screen.width / 2f, Screen.width / 2f - Offset);
       float yPos = Random.Range(Offset, Screen.height / 2f - Offset);
       go.transform.localPosition = new Vector3(xPos, yPos, 0f);
       go.transform.localScale = new Vector3(3f, 4f, 1f);

       // configure videoSurface
       VideoSurface videoSurface = go.AddComponent<VideoSurface>();
       return videoSurface;
   }


   */


//Stats  https://docs.agora.io/en/Voice/in-call_quality_unity?platform=Unity
/*
networkTransportDelay The network delay from the sender to the receiver. 	Stages 2 + 3 + 4 in the figure above
jitterBufferDelay 	The network delay from the receiver to the network jitter buffer. 	Stage 5 in the figure above
audioLossRate 	The frame loss rate of the received remote audio streams in the reported interval. 	
Stages 2 + 3 + 4 + 5 in the figure above
In a reported interval, audio freeze occurs when the audio frame loss rate reaches 4%.
receivedSampleRate 	The sample rate of the received remote audio streams in the reported interval. 	
receivedBitrate 	The average bitrate of the received remote audio streams in the reported interval. 	
totalFrozenTime 	The total freeze time (ms) of the remote audio streams after the remote user joins the channel. 	
Agora defines totalFrozenTime = The number of times the audio freezes × 2 × 1000 (ms).
The total time is the cumulative duration after the remote user joins the channel.
frozenRate 	The total audio freeze time as a percentage of the total time when the audio is available. 	When the remote user/host neither stops sending the audio stream nordisables the audio module after joining the channel, the audio is available.
*/