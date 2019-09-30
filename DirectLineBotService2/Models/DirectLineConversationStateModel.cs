using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DirectLineBotService2.Models
{
    public class DirectLineConversationStateModel
    {
        public string ConversationId { get; set; }
        public string Token { get; set; }
        public int? ExpiresIn { get; set; }
        public string StreamUrl { get; set; }
        public string ReferenceGrammarId { get; set; }
        public string ETag { get; set; }
        public string Watermark { get; set; }
    }
}