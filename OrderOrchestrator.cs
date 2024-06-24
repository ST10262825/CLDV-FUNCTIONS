using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;


namespace CraftsFunctions
{
    public static class OrderOrchestrator
    {
        [FunctionName("OrderOrchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var orderData = context.GetInput<PurchaseHistory>();

            // Call activity functions in sequence
            await context.CallActivityAsync(nameof(UpdateInventory), orderData);
            await context.CallActivityAsync(nameof(ProcessPayment), orderData);
            await context.CallActivityAsync(nameof(UpdateOrderHistory), orderData);
            await context.CallActivityAsync(nameof(SendNotification), orderData);

            // Or you can call them in parallel using Task.WhenAll
            // var tasks = new List<Task>
            // {
            //     context.CallActivityAsync(nameof(UpdateInventory), orderData),
            //     context.CallActivityAsync(nameof(ProcessPayment), orderData),
            //     context.CallActivityAsync(nameof(UpdateOrderHistory), orderData),
            //     context.CallActivityAsync(nameof(SendNotification), orderData)
            // };
            // await Task.WhenAll(tasks);

            // Optionally return some output
            var outputs = new List<string>
            {
                "Inventory updated",
                "Payment processed",
                "Order history updated",
                "Notification sent"
            };

            return outputs;
        }

        [FunctionName(nameof(UpdateInventory))]
        public static async Task UpdateInventory([ActivityTrigger] PurchaseHistory orderData, ILogger log)
        {
            log.LogInformation($"Updating inventory for Order ID: {orderData.Id}");
            // Your logic to update inventory
        }

        [FunctionName(nameof(ProcessPayment))]
        public static async Task ProcessPayment([ActivityTrigger] PurchaseHistory orderData, ILogger log)
        {
            log.LogInformation($"Processing payment for Order ID: {orderData.Id}");
            // Your logic to process payment
        }

        [FunctionName(nameof(UpdateOrderHistory))]
        public static async Task UpdateOrderHistory([ActivityTrigger] PurchaseHistory orderData, ILogger log)
        {
            log.LogInformation($"Updating order history for Order ID: {orderData.Id}");
            // Your logic to update order history
        }

        [FunctionName(nameof(SendNotification))]
        public static async Task SendNotification([ActivityTrigger] PurchaseHistory orderData, ILogger log)
        {
            log.LogInformation($"Sending notification for Order ID: {orderData.Id}");
            // Your logic to send notification
        }

        [FunctionName("OrderOrchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Deserialize request body to get OrderData
            var orderData = await req.Content.ReadAsAsync<PurchaseHistory>();
            if (orderData == null)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Please provide order data in the request body."),
                };
            }

            // Start the orchestration
            string instanceId = await starter.StartNewAsync("OrderOrchestrator", orderData);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }

    public class PurchaseHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; }
        public DateTime PurchaseDate { get; set; }
        public bool IsConfirmed { get; set; }

        // Navigation property to User
        public IdentityUser User { get; set; }
    }
}
