
using Castle.DynamicProxy;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Utilities.Interceptors;
using Serilog;
using LogParameter = EcommerceAPI.Core.CrossCuttingConcerns.Logging.LogParameter;
using Newtonsoft.Json;

namespace EcommerceAPI.Core.Aspects.Autofac.Logging;

public class LogAspect : MethodInterception
{
    private ILogger _loggerService;

    public LogAspect()
    {
    }

    protected override void OnBefore(IInvocation invocation)
    {
        if (_loggerService == null)
        {
             _loggerService = Log.Logger;
             // Or better: ServiceTool.ServiceProvider.GetService<ILogger>();
        }

        var logDetail = GetLogDetail(invocation);
        _loggerService.Information("Running: {0} | Args: {1}", invocation.Method.Name, JsonConvert.SerializeObject(logDetail.LogParameters));
    }

    protected override void OnException(IInvocation invocation, Exception e)
    {
        if (_loggerService == null)
        {
             _loggerService = Log.Logger;
        }

        var logDetail = GetLogDetail(invocation);
        _loggerService.Error(e, "Exception in: {0} | Args: {1}", invocation.Method.Name, JsonConvert.SerializeObject(logDetail.LogParameters));
    }

    private LogDetail GetLogDetail(IInvocation invocation)
    {
        var logParameters = new List<LogParameter>();
        for (int i = 0; i < invocation.Arguments.Length; i++)
        {
            var parameterName = invocation.Method.GetParameters()[i].Name;
            var parameterValue = invocation.Arguments[i];
            logParameters.Add(new LogParameter
            {
                Name = parameterName,
                Value = parameterValue,
                Type = parameterValue?.GetType().Name
            });
        }

        var logDetail = new LogDetail
        {
            MethodName = invocation.Method.Name,
            LogParameters = logParameters
        };

        return logDetail;
    }
}
