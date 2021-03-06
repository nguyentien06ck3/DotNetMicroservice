﻿using DotNetMicroservice.Services.Interfaces;
using DotNetMicroservice.Services.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Namotion.Messaging;
using Namotion.Messaging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetMicroservice.Processes
{
    // ## Messaging: Receiver

    public class CompleteOrderMessageProcessor : BackgroundService
    {
        private readonly IMessageReceiver<CompleteOrderMessage> _messageReceiver;
        private readonly IOrderService _orderService;
        private readonly ILogger _logger;

        public CompleteOrderMessageProcessor(
            IMessageReceiver<CompleteOrderMessage> messageReceiver,
            IOrderService orderService,
            ILogger<CompleteOrderMessageProcessor> logger)
        {
            _messageReceiver = messageReceiver;
            _orderService = orderService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Now listening for {nameof(CompleteOrderMessage)} messages.");

            await _messageReceiver.ListenAndDeserializeJsonAsync(async (messages, ct) =>
            {
                foreach (var m in messages)
                {
                    try
                    {
                        var message = m.Object;
                        await _orderService.CompleteOrderAsync(message.Id, message.ParcelNumber);
                        await _messageReceiver.ConfirmAsync(m, ct);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Error while processing {nameof(CompleteOrderMessage)} message.");
                        await _messageReceiver.RejectAsync(m, ct);
                    }
                }
            }, stoppingToken);
        }
    }
}
