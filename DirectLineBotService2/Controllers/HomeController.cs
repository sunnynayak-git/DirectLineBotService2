using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
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
        // to create a new message


        public async Task<ActionResult> Index()
        {
            // Create an Instance of the Chat object
            Chat objChat = new Chat();
            // Only call Bot if logged in
            if (User.Identity.IsAuthenticated)
            {
                // Pass the message to the Bot 
                // and get the response
                objChat = await TalkToTheBot("Hello");
            }
            else
            {
                objChat.ChatResponse = "Must be logged in";
            }
            // Return response
            return View(objChat);
        }


        private async Task<Chat> TalkToTheBot(string paramMessage)
        {
            // Connect to the DirectLine service
            DirectLineClient client = new DirectLineClient(directLineSecret);
            // Try to get the existing Conversation
            Conversation conversation =
                System.Web.HttpContext.Current.Session["conversation"] as Conversation;
            // Try to get an existing watermark 
            // the watermark marks the last message we received
            string watermark =
                System.Web.HttpContext.Current.Session["watermark"] as string;
            if (conversation == null)
            {
                // There is no existing conversation
                // start a new one
                conversation = await client.Conversations.StartConversationAsync();
            }
            // Use the text passed to the method (by the user)
            // to create a new message
            Activity userMessage = new Activity
            {
                From = new ChannelAccount(User.Identity.Name),
                Text = paramMessage,
                Type = ActivityTypes.Message
            };
            // Post the message to the Bot
            await client.Conversations.PostActivityAsync(conversation.ConversationId, userMessage);
            // Get the response as a Chat object
            Chat objChat =
                await ReadBotMessagesAsync(client, conversation.ConversationId, watermark);
            // Save values
            System.Web.HttpContext.Current.Session["conversation"] = conversation;
            System.Web.HttpContext.Current.Session["watermark"] = objChat.watermark;
            objChat.ConvertationId = conversation.ConversationId;
            // Return the response as a Chat object
            return objChat;
        }



        private async Task<Chat> ReadBotMessagesAsync(
            DirectLineClient client, string conversationId, string watermark)
        {
            // Create an Instance of the Chat object
            Chat objChat = new Chat();
            // We want to keep waiting until a message is received
            bool messageReceived = false;
            while (!messageReceived)
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
                    // Set the text response
                    // to the message text
                    objChat.ChatResponse
                        += " "
                        + activity.Text.Replace("\n\n", "<br />");
                    // Are there any attachments?
                    if (activity.Attachments != null)
                    {
                        // Extract each attachment from the activity.
                        foreach (Attachment attachment in activity.Attachments)
                        {
                            switch (attachment.ContentType)
                            {
                                case "image/png":
                                    // Set the text response as an HTML link
                                    // to the image
                                    objChat.ChatResponse
                                        += " "
                                        + attachment.ContentUrl;
                                    break;
                            }
                        }
                    }
                }
                // Mark messageReceived so we can break 
                // out of the loop
                messageReceived = true;
            }
            // Set watermark on the Chat object that will be 
            // returned
            objChat.watermark = watermark;
            // Return a response as a Chat object
            return objChat;
        }

        #region public async Task<ActionResult> Index(Chat model)
        [HttpPost]
        public async Task<ActionResult> Index(Chat model)
        {
            // Create an Instance of the Chat object
            Chat objChat = new Chat();
            // Only call Bot if logged in
            if (User.Identity.IsAuthenticated)
            {
                // Pass the message to the Bot 
                // and get the response
                objChat = await TalkToTheBot(model.ChatMessage);
            }
            else
            {
                objChat.ChatResponse = "Must be logged in";
            }
            // Return response
            return View(objChat);
        }
        #endregion
    }


}