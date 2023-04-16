
namespace Mango.Services.ShoppingCartAPI.Messages
{
    public class SQSModel
    {
        public string Type { get; set; }
        public string MessageId { get; set; }
        public string TopicArn { get; set; }
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string SignatureVersion { get; set; }
        public string SigningCertURL { get; set; }
        public string UnsubscribeURL { get; set;}
        public Dictionary<string, MessageAttributeValueModel> MessageAttributes { get; set; }

    }

    public class MessageAttributeValueModel
    {
        public string Value { get; set; }
        public string Type { get; set; }
    }
}
