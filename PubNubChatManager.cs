using System;
using PubNubAPI;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum FirebaseState { Testing, Build };

#region Serialize Class
[Serializable]
public class ChatJsonData
{
    public string uuid;
    public string username;
    public string text;
    public string chatID;
    public long personalTimeToken;
    public List<MessageActions> messageActions;
}
[Serializable]
public class MessageActions
{
    public string actionType;
    public MessageActionsTypeValue messageActionsTypeValues;
}
[Serializable]
public class MessageActionsTypeValue
{
    public string actionValue;
    public List<MessageActionAttributes> attributes;
}
[Serializable]
public class MessageActionAttributes
{
    public string id;
    public long actionTimetoken;
}
#endregion

public class PubNubChatManager : MonoBehaviour
{
    public static PubNubChatManager instance;
    public PubNub pubnub;

    public FirebaseState firebaseState = FirebaseState.Testing;
    public PubNubStatus pubnubStatus = PubNubStatus.None;

    #region Delegate
    public static event Action<bool, ChatPrefabData_OvrCommunity> SubscribeToHybrid_Type1 = delegate { };    //success, user data
    public static event Action<bool, OVRCommunityFriendsPrefabData> SubscribeToHybrid_Type2 = delegate { }; //success, user data
    public static event Action<bool, string> UnsubscribeToHybrid = delegate { };                            //success, result

    public static event Action<string, string, bool, ushort> FetchMsgDelegate = delegate { };   //myUUID, otherUUID,isActionIncluded, history message count
    public static event Action<string, string, ushort> GetHistoryDelegate = delegate { };       //myUUID, otherUUID, history message count

    public static event Action<string, string, Dictionary<string, object>> SendMessageDelegate = delegate { };  //myUUID, otherUUID, message payload
    public static event Action<bool, bool, ChatJsonData, long, bool> MessageRecieve = delegate { };             //success, isSent, message payload, isHistory

    public static event Action<string, string> SignalDelegate = delegate { };   //otherUUID, typing state
    public static event Action<bool, object> SignalRecieved = delegate { };     //success, channel name, signal

    public static event Action<string, string, string, string, long> SendActionDelegate = delegate { }; //actionType, actionValue, myUUID, otherUUID, message time token
    public static event Action<bool, string, string, string, long> ActionRecieved = delegate { };       //success, sender UUID, action type, action value, message time token

    public static event Action<string, string, long> SetMembership = delegate { };  //myUUID, otherUUID, message time token
    public static event Action<string> GetMembership = delegate { };                //myUUID
    public static event Action<string, string> RemoveMembership = delegate { };     //myUUID, message time token

    public static event Action<bool, ChatJsonData> LocalNotificationTrigger = delegate { };
    #endregion

    private void Awake()
    {
        pubnubStatus = PubNubStatus.None;

        if (instance == null)
        {
            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
#if UNITY_ANDROID
        if (firebaseState == FirebaseState.Build)
            Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
#endif
        UserManager.UserManagerStatusChanged += UserManager_UserManagerStatusChanged;
        UserManager.UserDataUpdated += OnUserDataUpdated;
    }

    private void OnDisable()
    {
#if UNITY_ANDROID
        if (firebaseState == FirebaseState.Build)
            Firebase.Messaging.FirebaseMessaging.TokenReceived -= OnTokenReceived;
#endif
        UserManager.UserManagerStatusChanged -= UserManager_UserManagerStatusChanged;
        UserManager.UserDataUpdated -= OnUserDataUpdated;

        OVRCommunityRemoveFriend.instance.LastDataRecivedChanged -= DeleteChat;
        OVRCommunityDeclineRequest.instance.LastDataRecivedChanged -= DeleteChat;
    }

    private void Start()
    {
        if (pubnubStatus == PubNubStatus.None)
            UserManager_UserManagerStatusChanged(UserManager.main.Status);
    }

    private void UserManager_UserManagerStatusChanged(UserManagerStatus obj)
    {
        switch (obj)
        {
            case UserManagerStatus.LoggedIn:
                PubNubConfig();
                break;
            case UserManagerStatus.LogedOut:
                pubnubStatus = PubNubStatus.None;
                if (StaticDataManager_OvrCommunity.deviceToken != null)
                    RemovePushFromChannels(StaticDataManager_OvrCommunity.deviceToken);
                break;
        }
    }

    void OnUserDataUpdated()
    {
        UserData data = UserManager.main.UserData;

        if (data.ovrId != null)
        {
            //Debug.Log("name : " + UserManager.main.UserData.name);
            StaticDataManager_OvrCommunity.myName = data.DisplayName;
        }
    }

    public void PubNubConfig()
    {
        Debug.Log("PubNub Config");
        OVRCommunityRemoveFriend.instance.LastDataRecivedChanged += DeleteChat;
        OVRCommunityDeclineRequest.instance.LastDataRecivedChanged += DeleteChat;

        pubnubStatus = PubNubStatus.Connecting;
        StaticDataManager_OvrCommunity.myUUID = UserManager.main.UserData.ovrId;
        StaticDataManager_OvrCommunity.myName = UserManager.main.UserData.DisplayName;

        PNConfiguration pnConfiguration = new PNConfiguration
        {
            PublishKey = StaticDataManager_OvrCommunity.pubNubPublishKey,
            SubscribeKey = StaticDataManager_OvrCommunity.pubNubSubscribeKey,
            SecretKey = StaticDataManager_OvrCommunity.pubNubSecretKey,
            LogVerbosity = PNLogVerbosity.BODY,
            UUID = StaticDataManager_OvrCommunity.myUUID
        };
        pubnub = new PubNub(pnConfiguration, this.gameObject);

        if (pubnub == null)
            PubNubStateChangeCallback(false);
        else
            PubNubStateChangeCallback(true);

        SubscribingToMyself(UserManager.main.UserData.ovrId);

        pubnub.SubscribeCallback += SubscribeCallbackHandler;
    }

    private void PubNubStateChangeCallback(bool state)
    {
        if (!state)
        {
            OvrCanvasManager.main.ChangeCanvasStatus(OvrCanvasStatus.Home);
            Debug.LogError("Failed to configure PubNub...");

            StaticDataManager_OvrCommunity.leftBtnAction = () =>
            {
                PubNubConfig();
            };

            pubnubStatus = PubNubStatus.None;

            string communityServiceError = LocalizedStringsManager.main.GetString(OvrCommunityStringKeyData.GetOne(instance, OvrCommunityStringKey.CommunityServiceError));
            CanvasManagerBridge.SceneMainCanvas.OpenSimpleDoubleDialogPopUp(
                communityServiceError,
                StaticDataManager_OvrCommunity.ErrorRightBtnText,
                null,
                StaticDataManager_OvrCommunity.ErrorLeftBtnText,
                StaticDataManager_OvrCommunity.leftBtnAction);
        }
        else
        {
            pubnubStatus = PubNubStatus.Joined;
        }
    }

    void SubscribeCallbackHandler(object sender, EventArgs e)
    {
        SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;

        if (mea.Status != null)
        {
            switch (mea.Status.Category)
            {
                case PNStatusCategory.PNUnexpectedDisconnectCategory:
                case PNStatusCategory.PNTimeoutCategory:
                    // handle publish
                    break;
            }
        }

        if (mea.MessageResult != null)
        {
            ChatJsonData finalPayload = PayloadConverter.instance.PayloadChecker(mea.MessageResult.Payload);
            finalPayload.messageActions.Clear();

            //StartCoroutine(NewMessageRecieved(finalPayload, mea));

            if (ChatUiManager_OvrCommunity.instance != null)
            {
                //Debug.LogError("In here 0");
                if (ChatUiManager_OvrCommunity.instance.OtherUserCommunityInfo == null || ChatUiManager_OvrCommunity.instance.OtherUserCommunityInfo.public_name == null || ChatUiManager_OvrCommunity.instance.OtherUUID == null)
                {
                    //Debug.LogError("In here 1");
                    StartCoroutine(GetMessageCount(StaticDataManager_OvrCommunity.myUUID));
                    MessageRecieve(true, false, finalPayload, mea.MessageResult.Timetoken, false);

                    LocalNotificationTrigger(true, finalPayload);
                }
                else
                {
                    //Debug.LogError("In here 2");
                    if (mea.MessageResult.Channel == GetHybridChannelName(StaticDataManager_OvrCommunity.myUUID, ChatUiManager_OvrCommunity.instance.OtherUUID))
                    {
                        //Debug.LogError("In here 3");
                        SetMembershipData(
                            StaticDataManager_OvrCommunity.myUUID,
                            ChatUiManager_OvrCommunity.instance.OtherUserCommunityInfo.uuID,
                            mea.MessageResult.Timetoken);

                        if (finalPayload.uuid == ChatUiManager_OvrCommunity.instance.OtherUUID)
                            MessageRecieve(true, false, finalPayload, mea.MessageResult.Timetoken, false);
                    }
                    else
                    {
                        //Debug.LogError("In here 4");
                        if (finalPayload.uuid != ChatUiManager_OvrCommunity.instance.OtherUUID)
                        {
                            //Debug.LogError("In here 4b");
                            LocalNotificationTrigger(true, finalPayload);
                        }
                    }
                }
            }
            else
            {
                //Debug.LogError("In here 5");
                StartCoroutine(GetMessageCount(StaticDataManager_OvrCommunity.myUUID));

                LocalNotificationTrigger(true, finalPayload);
            }
        }

        if (mea.SignalEventResult != null)
        {
            SignalRecieved(true, mea.SignalEventResult.Payload);
        }

        if (mea.MessageActionsEventResult != null)
        {
            //Debug.Log("Action channel : " + mea.MessageActionsEventResult.Channel);
            if (mea.MessageActionsEventResult.Data != null)
            {
                if (ChatUiManager_OvrCommunity.instance.OtherUUID != null)
                {
                    if (mea.MessageActionsEventResult.Channel == GetHybridChannelName(StaticDataManager_OvrCommunity.myUUID, ChatUiManager_OvrCommunity.instance.OtherUUID))
                    {
                        PNMessageActionsResult data = mea.MessageActionsEventResult.Data;
                        ActionRecieved(true, data.UUID, data.ActionType, data.ActionValue, data.MessageTimetoken);
                    }
                }
            }
        }

        /*if (mea.PresenceEventResult != null)
        {
            Debug.Log("SubscribeCallback in presence" +
                mea.PresenceEventResult.Channel +
                mea.PresenceEventResult.Occupancy +
                mea.PresenceEventResult.Event);
        }*/
    }

    void SubscribingToMyself(string UUID)
    {
        pubnub.Subscribe()
              .Channels(new List<string> { GetChannelName(UUID) })
              .WithPresence()
              .Execute();

        if (firebaseState == FirebaseState.Testing)
            StaticDataManager_OvrCommunity.deviceToken = "dvKaZa_7SLm_sISchFAxfz:APA91bEJLJHziSXGN0t_Z5EO3VSM2pAco3XCy0XHsU2VWy0Yukw70UPpLuqAumEYxMNSI0rTTWHKox_Ss7Fchik41_WyKi5mDh8QRJXejaXV-lVUd45cjl5ypjWYCYxPJLi7zmWxG3R0";

        StartCoroutine(ConfigPush());

        GetMembership(UUID);
    }

    #region Create Push Notification
#if UNITY_ANDROID
    public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
    {
        Debug.Log("Firebase Token Recieved : " + token.Token);
        StaticDataManager_OvrCommunity.deviceToken = token.Token;
    }
#endif
    private IEnumerator ConfigPush()
    {
        Debug.Log("Token Recieved : " + StaticDataManager_OvrCommunity.deviceToken);

        while (StaticDataManager_OvrCommunity.deviceToken == "" || StaticDataManager_OvrCommunity.deviceToken == null)
            yield return new WaitForEndOfFrame();

        AddPushToChannels(StaticDataManager_OvrCommunity.myUUID, StaticDataManager_OvrCommunity.deviceToken);
    }
    private void AddPushToChannels(string UUID, string deviceID)
    {
        Debug.LogError("Setting up Push Notification");
        pubnub.AddPushNotificationsOnChannels()
#if UNITY_ANDROID
            .PushType(PNPushType.GCM)
#elif UNITY_IOS
                .PushType(PNPushType.APNS2)
                .Topic(OVRCommunityStaticDataManager.iosPushPayloadTopic)
                .Environment(PNPushEnvironment.production)
#endif
            .Channels(new List<string> { GetChannelName(UUID) })
            .DeviceID(deviceID)
            .Async((result, status) =>
            {
                if (status.Error)
                {
                    Debug.LogError(string.Format("AddPush Error: {0}, {1}, {2}",
                        status.StatusCode,
                        status.ErrorData.Info,
                        status.Category
                    ));
                }
         
#if UNITY_ANDROID
                    Debug.Log("Notification Configured for Android.");
#elif UNITY_IOS
                    Debug.Log("Notification Configured for IOS.");
#endif
            });
    }

    private void RemovePushFromChannels(string deviceID)
    {
        Debug.Log("Removing Push Notification");
        if (pubnub != null)
        {
            pubnub.RemoveAllPushNotifications()
#if UNITY_ANDROID
                .PushType(PNPushType.GCM)
#elif UNITY_IOS
                .PushType(PNPushType.APNS2)
                .Topic(OVRCommunityStaticDataManager.iosPushPayloadTopic)
                .Environment(PNPushEnvironment.production)
#endif
                .DeviceID(deviceID)
                .Async((result, status) =>
                {
                    if (status.Error)
                    {
                        Debug.LogError(string.Format("RemovePush Error: {0}, {1}, {2}",
                            status.StatusCode,
                            status.ErrorData.Info,
                            status.Category
                        ));
                    }
                    else
                    {
#if UNITY_ANDROID
                        Debug.Log("Notifications Removed from android.");
#elif UNITY_IOS
                        Debug.Log("Notifications Removed from iOS.");
#endif
                        CleanPubNubData();
                    }
                });
        }
    }
    private void CleanPubNubData()
    {
        if (pubnub != null)
        {
            pubnub.CleanUp();
            pubnub.SubscribeCallback -= SubscribeCallbackHandler;
            pubnub = null;
        }
        StaticDataManager_OvrCommunity.myName = null;
        StaticDataManager_OvrCommunity.myUUID = null;
    }
    #endregion

    #region Subscribe To Hybrid Channel
    public void SubscribeToHybridChannel(string UUID_1, string UUID_2, ChatPrefabData_OvrCommunity data)
    {
        pubnub.Subscribe()
            .Channels(new List<string> { GetHybridChannelName(UUID_1, UUID_2) })
            .Execute();

        SubscribeToHybrid_Type1(true, data);
    }
    public void SubscribeToHybridChannel(string UUID_1, string UUID_2, OVRCommunityFriendsPrefabData data)
    {
        pubnub.Subscribe()
            .Channels(new List<string> { GetHybridChannelName(UUID_1, UUID_2) })
            .Execute();

        SubscribeToHybrid_Type2(true, data);
    }
    #endregion

    #region Unsubscribe To Hybrid Channel
    public void UnsubscribeToHybridChannel(string UUID_1, string UUID_2)
    {
        if (pubnub != null)
        {
            pubnub.Unsubscribe()
            .Channels(new List<string> { GetHybridChannelName(UUID_1, UUID_2) })
            .Async((resut, status) =>
            {
                if (status.Error)
                {
                    Debug.LogError(string.Format("Error in Unsubcribing : {0}, {1}, {2}",
                        status.StatusCode,
                        status.ErrorData.Info,
                        status.Category
                    ));
                    UnsubscribeToHybrid(false, "");
                }
                else
                {
                    //Debug.Log("Unsubscribe Successfull.." + resut.Message);
                    UnsubscribeToHybrid(true, resut.Message);
                }
            });
        }
    }
    #endregion

    #region Generate Channel Names
    public string GetChannelName(string uuid)
    {
        return "channel_" + uuid;
    }

    public string GetHybridChannelName(string id1, string id2)
    {
        string hybridChnl = "";
        int i = 0;

        while (true)
        {
            if (id1.ToUpper()[i] != id2.ToUpper()[i])
            {
                if (id1.ToUpper()[i] < id2.ToUpper()[i])
                    hybridChnl = "channel_" + id1 + "_" + id2;
                else
                    hybridChnl = "channel_" + id2 + "_" + id1;
                break;
            }
            i++;
        }
        return hybridChnl;
    }
    #endregion

    public void FetchAllMsg(string UUID_1, string UUID_2, bool isActionIncluded, ushort numberOfMesseges)
    {
        FetchMsgDelegate(UUID_1, UUID_2, isActionIncluded, numberOfMesseges);
    }

    public void SendPushMessage(string UUID_1, string UUID_2, Dictionary<string, object> payload)
    {
        SendMessageDelegate(UUID_1, UUID_2, payload);
    }

    public void GetHistory(string UUID_1, string UUID_2, ushort historyMsgCount)
    {
        GetHistoryDelegate(UUID_1, UUID_2, historyMsgCount);
    }

    public void SetMembershipData(string UUID_1, string UUID_2, long timeToken)
    {
        SetMembership(UUID_1, UUID_2, timeToken);
    }

    public void GetUpdatedMessageCount(string UUID)
    {
        GetMembership(UUID);
    }

    private IEnumerator GetMessageCount(string UUID)
    {
        yield return new WaitForSeconds(1f);

        GetMembership(UUID);
    }

    public void SendSignal(string UUID, string state)
    {
        SignalDelegate(UUID, state);
    }

    public void AddMessageAction(string actionType, string actionValue, string UUID_1, string UUID_2, long timeToken)
    {
        SendActionDelegate(actionType, actionValue, UUID_1, UUID_2, timeToken);
    }

    #region Delete Message
    public void DeleteChat(bool state, string UUID)
    {
        if (state)
        {
            RemoveMembership(StaticDataManager_OvrCommunity.myUUID, UUID);

            pubnub.DeleteMessages()
                .Channel(GetHybridChannelName(StaticDataManager_OvrCommunity.myUUID, UUID))
                .Async((result, status) =>
                {
                    if (!status.Error)
                        Debug.Log("Chat Deleted.");
                    else
                    {
                        Debug.Log(status.Error);
                        Debug.Log(status.StatusCode);
                        Debug.Log(status.ErrorData.Info);
                    }
                });
        }
    }
    #endregion

    /*public bool deleteMessageFromAllChannels = false;
    private void Update()
    {
        if (deleteMessageFromAllChannels)
            DeleteAllChat();
    }
    private void DeleteAllChat()
    {
        if (deleteMessageFromAllChannels)
        {
            deleteMessageFromAllChannels = false;
            for (int i = 0; i < OVRCommunityStaticDataManager.subscriebedChannelsList.Count; i++)
            {
                pubnub.DeleteMessages()
                    .Channel(OVRCommunityStaticDataManager.subscriebedChannelsList[i])
                     .Async((result, status) =>
                     {
                         if (!status.Error)
                             Debug.Log("Chat Deleted.");
                         else
                         {
                             Debug.Log(status.Error);
                             Debug.Log(status.StatusCode);
                             Debug.Log(status.ErrorData.Info);
                         }
                     });
            }
        }
    }*/
}