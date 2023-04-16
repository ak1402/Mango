using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Mango.Common;
using Mango.Services.PaymentAPI.Messages;
using Newtonsoft.Json;
using System.Text;

namespace Mango.Services.PaymentAPI.Messaging
{
    public class AWSSQSConsumer : BackgroundService
    {
        private IAmazonSQS _sqs;
        private IConfiguration _config;
        private IAmazonSimpleNotificationService _sns;

        public AWSSQSConsumer(IAmazonSQS sqs, IConfiguration config, IAmazonSimpleNotificationService sns)
        {
            _sqs = sqs;
            _config = config;
            _sns = sns;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueUrl = _config.GetValue<string>("AWS:SQSARNPaymentRequest");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var recieveMessage = new ReceiveMessageRequest();
                    recieveMessage.VisibilityTimeout = 5;
                    recieveMessage.MaxNumberOfMessages = 5;
                    recieveMessage.QueueUrl = queueUrl;

                    var responseSQS = await _sqs.ReceiveMessageAsync(recieveMessage);
                    if (responseSQS.Messages.Count > 0)
                    {
                        foreach (var message in responseSQS.Messages)
                        {
                            var model = JsonConvert.DeserializeObject<SQSModel>(message.Body);
                            await ProcessPayments(model);
                            var deleteResponse = await _sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle);
                        }

                    }

                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task ProcessPayments(SQSModel model)
        {
            var message = model.Message;

            PaymentRequestMessage paymentRequestMessage = JsonConvert.DeserializeObject<PaymentRequestMessage>(message);

            UpdatePaymentResultMessage updatePaymentResultMessage = new()
            {
                Status = true,
                OrderId = paymentRequestMessage.OrderId,
                Email = paymentRequestMessage.Email
            };


            try
            {
                var topicArn = _config.GetValue<string>("AWS:SNSTopicARN");
                var publishRequest = new PublishRequest();
                publishRequest.Message = System.Text.Json.JsonSerializer.Serialize(updatePaymentResultMessage);
                publishRequest.MessageAttributes = new Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>
                {
                    { AWSSNSActions.Action, new Amazon.SimpleNotificationService.Model.MessageAttributeValue () {
                        DataType = AWSSNSActions.DataTypeString,
                        StringValue = AWSSNSActions.PaymentUpdate
                    } }
                };

                publishRequest.TopicArn = topicArn;

                var response = await _sns.PublishAsync(publishRequest);
            }
            catch (Exception e)
            {
                throw;
            }

        }
    }
}
