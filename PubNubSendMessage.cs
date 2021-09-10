using System;
using UnityEngine;
using System.Collections.Generic;

public class PubNubSendMessage : MonoBehaviour
{
    public static event Action<bool, bool, ChatJsonData, long, bool> MessageSent = delegate { };    //success, isSent, chat payload, isHistory

    private void OnEnable()
    {
        PubNubChatManager.SendMessageDelegate += SendMessage;
    }

    private void OnDisable()
    {
        PubNubChatManager.SendMessageDelegate -= SendMessage;
    }

    private void SendMessage(string UUID_1, string UUID_2, Dictionary<string, object> payload)
    {
        Debug.Log("Sending message with Push Notification.");

        PubNubChatManager.instance.pubnub.Publish()
            .Channel(PubNubChatManager.instance.GetChannelName(UUID_2))
            .Message(payload)
            .Async((result, status) =>
            {
                if (status.Error)
                {
                    Debug.LogError(string.Format("Publish Error: {0}, {1}, {2}",
                        status.StatusCode,
                        status.ErrorData.Info,
                        status.Category));
                    MessageSent(false, false, null, 0, false);
                }
                else
                {
                    //Debug.Log("Message Time token 1 : " + result.Timetoken);
                    ((ChatJsonData)payload["payload"]).personalTimeToken = result.Timetoken;
                    PublishToHybrid(UUID_1, UUID_2, payload);
                }
            });
    }
    private void PublishToHybrid(string UUID_1, string UUID_2, Dictionary<string, object> payload)
    {
        PubNubChatManager.instance.pubnub.Publish()
            .Channel(PubNubChatManager.instance.GetHybridChannelName(UUID_1, UUID_2))
            .Message(payload)
            .Async((result, status) =>
            {
                if (status.Error)
                {
                    Debug.LogError(string.Format("Publish Hybrid Error: {0}, {1}, {2}",
                        status.StatusCode,
                        status.ErrorData.Info,
                        status.Category));
                }
                else
                {
                    /*AddMessageAction(
                        MessageActionTypeEnum.reaction.ToString() + "_" + StaticDataManager.myUUID,
                        "none",
                        StaticDataManager.myUUID,
                        StaticDataManager.currentUserUUID,
                        result.Timetoken);*/

                    //Debug.Log("Message Time token 2 : " + result.Timetoken);

                    ChatJsonData data = (ChatJsonData)payload["payload"];
                    MessageSent(true, true, data, result.Timetoken, false);
                }
            });

        ChatUiManager_OvrCommunity.instance.chatInputField.text = "";
    }
}