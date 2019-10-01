using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DirectLineBotService2.Models
{
    public class ConversationActivityModel
    {
        public string Id { get; set; }

        public string UserIdentifier { get; set; }

        public string Source { get; set; }

        public string ActivityType { get; set; }

        public string MessageText { get; set; }
    }
}