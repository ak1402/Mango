using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Mango.Common;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Repository;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace Mango.Services.OrderAPI.Messaging
{
    public class AWSSQSConsumer : BackgroundService
    {
        private IAmazonSQS _sqs;
        private IConfiguration _config;
        private readonly OrderRepository _orderRepository;
        protected readonly IAmazonSimpleNotificationService _sns;

        public AWSSQSConsumer(IAmazonSQS sqs, IConfiguration config, OrderRepository orderRepository, IAmazonSimpleNotificationService sns)
        {
            _sqs = sqs;
            _config = config;
            _orderRepository = orderRepository;
            _sns = sns;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueUrlCheckout = _config.GetValue<string>("AWS:SQSARN");
            var queueUrlPayment = _config.GetValue<string>("AWS:SQSARNPaymentUpdate");

            while (!stoppingToken.IsCancellationRequested)
            {
                var recieveMessage = new ReceiveMessageRequest();
                recieveMessage.VisibilityTimeout = 5;
                recieveMessage.MaxNumberOfMessages = 5;
                recieveMessage.QueueUrl = queueUrlCheckout;

                var responseSQS = await _sqs.ReceiveMessageAsync(recieveMessage);
                if (responseSQS.Messages.Count > 0)
                {
                    foreach (var message in responseSQS.Messages)
                    {
                        var sqsModel = JsonConvert.DeserializeObject<SQSModel>(message.Body);

                        await OnCheckOutMessageReceived(sqsModel);

                        await _sqs.DeleteMessageAsync(queueUrlCheckout, message.ReceiptHandle);
                    }

                }

                var recieveMessageNew = new ReceiveMessageRequest();
                recieveMessageNew.VisibilityTimeout = 5;
                recieveMessageNew.MaxNumberOfMessages = 5;
                recieveMessageNew.QueueUrl = queueUrlPayment;
                var responseSQSPayment = await _sqs.ReceiveMessageAsync(recieveMessageNew);
                if (responseSQSPayment.Messages.Count > 0)
                {
                    foreach (var message in responseSQSPayment.Messages)
                    {
                        var sqsModel = JsonConvert.DeserializeObject<SQSModel>(message.Body);

                        await OnOrderPaymentUpdateReceived(sqsModel);

                        await _sqs.DeleteMessageAsync(queueUrlPayment, message.ReceiptHandle);
                    }

                }



                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        private async Task OnCheckOutMessageReceived(SQSModel args)
        {
            

            CheckoutHeaderDto checkoutHeaderDto = JsonConvert.DeserializeObject<CheckoutHeaderDto>(args.Message);

            OrderHeader orderHeader = new()
            {
                UserId = checkoutHeaderDto.UserId,
                FirstName = checkoutHeaderDto.FirstName,
                LastName = checkoutHeaderDto.LastName,
                OrderDetails = new List<OrderDetails>(),
                CardNumber = checkoutHeaderDto.CardNumber,
                CouponCode = checkoutHeaderDto.CouponCode,
                CVV = checkoutHeaderDto.CVV,
                DiscountTotal = checkoutHeaderDto.DiscountTotal,
                Email = checkoutHeaderDto.Email,
                ExpiryMonthYear = checkoutHeaderDto.ExpiryMonthYear,
                OrderTime = DateTime.Now,
                OrderTotal = checkoutHeaderDto.OrderTotal,
                PaymentStatus = false,
                Phone = checkoutHeaderDto.Phone,
                PickupDateTime = checkoutHeaderDto.PickupDateTime
            };
            foreach (var detailList in checkoutHeaderDto.CartDetails)
            {
                OrderDetails orderDetails = new()
                {
                    ProductId = detailList.ProductId,
                    ProductName = detailList.Product.Name,
                    Price = detailList.Product.Price,
                    Count = detailList.Count
                };
                orderHeader.CartTotalItems += detailList.Count;
                orderHeader.OrderDetails.Add(orderDetails);
            }

            await _orderRepository.AddOrder(orderHeader);


            PaymentRequestMessage paymentRequestMessage = new()
            {
                Name = orderHeader.FirstName + " " + orderHeader.LastName,
                CardNumber = orderHeader.CardNumber,
                CVV = orderHeader.CVV,
                ExpiryMonthYear = orderHeader.ExpiryMonthYear,
                OrderId = orderHeader.OrderHeaderId,
                OrderTotal = orderHeader.OrderTotal,
                Email = orderHeader.Email
            };

            try
            {
                var topicArn = _config.GetValue<string>("AWS:SNSTopicARN");
                var publishRequest = new PublishRequest();
                publishRequest.Message = System.Text.Json.JsonSerializer.Serialize(paymentRequestMessage);
                publishRequest.MessageAttributes = new Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>
                {
                    { AWSSNSActions.Action, new Amazon.SimpleNotificationService.Model.MessageAttributeValue () {
                        DataType = AWSSNSActions.DataTypeString,
                        StringValue = AWSSNSActions.PaymentRequest
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

        private async Task OnOrderPaymentUpdateReceived(SQSModel args)
        {
            var message = args.Message;

            UpdatePaymentResultMessage paymentResultMessage = JsonConvert.DeserializeObject<UpdatePaymentResultMessage>(message);

            await _orderRepository.UpdateOrderPaymentStatus(paymentResultMessage.OrderId, paymentResultMessage.Status);
        }
    }
}
