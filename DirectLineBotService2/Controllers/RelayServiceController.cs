using DirectLineBotService2.Models;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
namespace DirectLine.Controllers
{
    public class RelayServiceController : Controller
    {
        private static string directLineSecret = ConfigurationManager.AppSettings["DirectLineSecret"];
        private static string botId = ConfigurationManager.AppSettings["BotId"];
        private static Dictionary<string, DirectLineConversationStateModel> activeConversationsState = new Dictionary<string, DirectLineConversationStateModel>();

        /// <summary>
        /// API call to send message to Direct Line BOT
        /// </summary>
        /// <param name="conversationActivityModel"></param>
        /// <returns></returns>
        public async Task PostActivityAsync(ConversationActivityModel conversationActivityModel)
        {
            await TalkToTheBot(conversationActivityModel);
        }

        /// <summary>
        /// Start Conversation with BOT
        /// </summary>
        /// <param name="conversationActivityModel"></param>
        /// <returns></returns>
        private async Task TalkToTheBot(ConversationActivityModel conversationActivityModel)
        {
            // Connect to the DirectLine service
            DirectLineClient client = new DirectLineClient(directLineSecret);

            DirectLineConversationStateModel conversationState;
            bool wasConnectionExisted;
            if (!activeConversationsState.ContainsKey(conversationActivityModel.Id.ToString()))
            {
                Conversation conversation = await client.Conversations.StartConversationAsync();
                conversationState = ConversationActorStateModelMapper(conversation);
                activeConversationsState.Add(conversationActivityModel.Id.ToString(), conversationState);
                wasConnectionExisted = false;
            }
            else
            {
                conversationState = activeConversationsState[conversationActivityModel.Id];
                client.Conversations.ReconnectToConversation(conversationState.ConversationId);
                wasConnectionExisted = true;
            }

            await SendActivityToBotAsync(client, conversationState.ConversationId, conversationActivityModel, wasConnectionExisted);

        }

        /// <summary>
        /// Posts the given activity to the bot using Direct Line client and Start Polling the activities from BOT
        /// </summary>
        /// <param name="client"></param>
        /// <param name="conversationId"></param>
        /// <param name="conversationActivityModel"></param>
        /// <param name="wasConnectionExisted"></param>
        /// <returns></returns>
        private static async Task SendActivityToBotAsync(DirectLineClient client, string conversationId, ConversationActivityModel conversationActivityModel, bool wasConnectionExisted)
        {
            // Start the bot message reader in a separate thread.
            if (!wasConnectionExisted)
            {
                new Thread(async () => await PollActivitiesAsync(client, conversationId, conversationActivityModel.Id)).Start();
            }

            Activity userMessage = new Activity
            {
                From = new ChannelAccount(conversationActivityModel.UserIdentifier),
                Text = conversationActivityModel.MessageText,
                Type = ActivityTypes.Message
            };

            await client.Conversations.PostActivityAsync(conversationId, userMessage);

        }

        /// <summary>
        /// Start Polling the activities from BOT
        /// </summary>
        /// <param name="client"></param>
        /// <param name="conversationId"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static async Task PollActivitiesAsync(DirectLineClient client, string conversationId, string Id)
        {
            string watermark = null;
            while (true)
            {
                try
                {
                    // Retrieve the activity set from the bot.
                    var activitySet = await client.Conversations.GetActivitiesAsync(conversationId, watermark);
                    // Set the watermark to the message received
                    watermark = activitySet?.Watermark;
                    // Extract the activies sent from our bot.
                    var activities = (from Activity in activitySet.Activities
                                      where Activity.From.Id == botId
                                      select Activity).ToList();
                    // Analyze each activity in the activity set.
                    foreach (Activity activity in activities)
                    {
                        if (!string.IsNullOrEmpty(activity.Text.Trim()))
                        {
                            Trace.TraceInformation($"ConversationId: {activity.Conversation.Id} Activity Message: {activity.Text}");
                        }
                        if (activity.Type == "endOfConversation")
                        {
                            activeConversationsState.Remove(Id);
                            Thread.CurrentThread.Abort();
                        }
                    }
                }
                catch (ThreadAbortException ex)
                {
                    Trace.TraceError(ex.StackTrace);
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

        }

        /// <summary>
        /// Mapper to map Conversation model to custom DirectLineConversationStateModel
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="waterMark"></param>
        /// <returns></returns>
        private DirectLineConversationStateModel ConversationActorStateModelMapper(Conversation conversation, string waterMark = null)
        {
            DirectLineConversationStateModel conversationState = null;
            if (conversation != null)
            {
                conversationState = new DirectLineConversationStateModel()
                {
                    ConversationId = conversation.ConversationId,
                    ETag = conversation.ETag,
                    ExpiresIn = conversation.ExpiresIn,
                    ReferenceGrammarId = conversation.ReferenceGrammarId,
                    StreamUrl = conversation.StreamUrl,
                    Token = conversation.Token,
                    Watermark = waterMark
                };

            }
            return conversationState;
        }
    }


}