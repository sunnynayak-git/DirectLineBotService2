using DirectLineBotService2.Models;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
namespace DirectLine.Controllers
{
    #region public class Chat
    public class Chat
    {
        public string ChatMessage { get; set; }
        public string ChatResponse { get; set; }
        public string watermark { get; set; }

        public string ConvertationId { get; set; }
    }
    #endregion
    public class HomeController : Controller
    {
        private static string DiretlineUrl
            = @"https://directline.botframework.com";
        private static string directLineSecret = ConfigurationManager.AppSettings["DirectLineSecret"];
        private static string botId = ConfigurationManager.AppSettings["BotId"];
        private static Dictionary<string, DirectLineConversationStateModel> activeConversationsState = new Dictionary<string, DirectLineConversationStateModel>();
        // to create a new message

        public async Task PostAsync(ConversationActivityModel conversationActivityModel)
        {
            await TalkToTheBot(conversationActivityModel);
        }

        public async Task<ActionResult> Index()
        {
            // Create an Instance of the Chat object
            Chat objChat = new Chat();
            // Only call Bot if logged in
            if (User.Identity.IsAuthenticated)
            {
                // Pass the message to the Bot 
                // and get the response
                //objChat = await TalkToTheBot("Hello");
            }
            else
            {
                objChat.ChatResponse = "Must be logged in";
            }
            // Return response
            return View(objChat);
        }


        private async Task TalkToTheBot(ConversationActivityModel conversationActivityModel)
        {
            // Connect to the DirectLine service
            DirectLineClient client = new DirectLineClient(directLineSecret);
            DirectLineConversationStateModel conversationState;
            if (!activeConversationsState.ContainsKey(conversationActivityModel.Id.ToString()))
            {
                Conversation conversation = await client.Conversations.StartConversationAsync();
                conversationState = ConversationActorStateModelMapper(conversation);
                activeConversationsState.Add(conversationActivityModel.Id.ToString(), conversationState);
                Thread thread = new Thread(new ThreadStart(async () => await ReadBotMessagesAsync(client, conversationState.ConversationId, botId)));
                thread.IsBackground = false;
                thread.Name = conversationState.ConversationId;
                thread.Start();
            }
            else
            {
                conversationState = activeConversationsState[conversationActivityModel.Id.ToString()];
            }
            // Use the text passed to the method (by the user)
            // to create a new message
            Activity userMessage = new Activity
            {
                From = new ChannelAccount(conversationActivityModel.UserIdentifier),
                Text = conversationActivityModel.MessageText,
                Type = ActivityTypes.Message
            };
            // Post the message to the Bot
            await client.Conversations.PostActivityAsync(conversationState.ConversationId, userMessage);
        }

        public async Task ReadBotMessagesAsync(DirectLineClient client, string conversationId, string botId, string watermark= null)
        {
            while (true)
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
                   //Update Watermark in dictionary
                   //Send Notification to User
                }
            }
            
        }

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