using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Mango.Services.ShoppingCartAPI.Messages;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;

namespace Mango.Services.ShoppingCartAPI.Messaging
{
    public class AWSSQSConsumer : BackgroundService
    {
        private IAmazonSQS _sqs;
        private IConfiguration _config;

        public AWSSQSConsumer(IAmazonSQS sqs, IConfiguration config)
        {
            _sqs = sqs;
            _config = config;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var topicArn = _config.GetValue<string>("AWS:SQSARN");

            while (!stoppingToken.IsCancellationRequested)
            {
                var recieveMessage = new ReceiveMessageRequest();
                recieveMessage.VisibilityTimeout = 5;
                recieveMessage.MaxNumberOfMessages = 5;
                recieveMessage.QueueUrl = topicArn;

                var responseSQS = await _sqs.ReceiveMessageAsync(recieveMessage);
                if(responseSQS.Messages.Count > 0)
                {
                    foreach(var message in responseSQS.Messages)
                    {
                        var model = JsonConvert.DeserializeObject<SQSModel>(message.Body);
                        var checkoutHeaderDto = JsonConvert.DeserializeObject<CheckoutHeaderDto>(model.Message);

                        var deleteResponse = await _sqs.DeleteMessageAsync(topicArn, message.ReceiptHandle);                        
                    }
                    
                }                

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }
}
