using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RestX.BLL.Interfaces;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class ExceptionHandler : IExceptionHandler
    {
        private readonly ILogger logger;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ActiveTenant currentTenant = null;

        public ExceptionHandler(ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor = null, IEnumerable<ActiveTenant> tenants = null)
        {
            this.logger = loggerFactory.CreateLogger<ExceptionHandler>();
            this.httpContextAccessor = httpContextAccessor;
            this.currentTenant = tenants?.FirstOrDefault();
        }

        public void RaiseException(Exception ex, string customMessage = "")
        {
            logger.LogError(ex, customMessage);
        }
    }
}
