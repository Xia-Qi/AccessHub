using Domain.Base;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessHub.Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly ICurrentUserService _currentUserService;

        public LoggingBehavior(
            ILogger<LoggingBehavior<TRequest, TResponse>> logger,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId ?? string.Empty;
            var userName = string.Empty;

            _logger.LogInformation(
                "Handling {RequestName} - User: {UserName} ({UserId})",
                requestName, userName, userId);

            var timer = Stopwatch.StartNew();
            try
            {
                var response = await next();

                timer.Stop();
                _logger.LogInformation(
                    "Handled {RequestName} - User: {UserName} ({UserId}) - Took: {ElapsedMilliseconds}ms",
                    requestName, userName, userId, timer.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                timer.Stop();
                _logger.LogError(
                    ex,
                    "Error handling {RequestName} - User: {UserName} ({UserId}) - Took: {ElapsedMilliseconds}ms",
                    requestName, userName, userId, timer.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
