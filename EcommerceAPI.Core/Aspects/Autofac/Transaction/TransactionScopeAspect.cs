
using System.Transactions;
using Castle.DynamicProxy;
using EcommerceAPI.Core.Utilities.Interceptors;
using System.Reflection;

namespace EcommerceAPI.Core.Aspects.Autofac.Transaction;

public class TransactionScopeAspect : MethodInterception
{
    public override void Intercept(IInvocation invocation)
    {
        // Determine if async
        var isAsync = (invocation.Method.ReturnType == typeof(Task) ||
                       (invocation.Method.ReturnType.IsGenericType && invocation.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)));

        if (isAsync)
        {
             InterceptAsync(invocation);
        }
        else
        {
             InterceptSync(invocation);
        }
    }

    private void InterceptSync(IInvocation invocation)
    {
        using (TransactionScope transactionScope = new TransactionScope())
        {
            try
            {
                invocation.Proceed();
                transactionScope.Complete();
            }
            catch (Exception)
            {
                transactionScope.Dispose();
                throw;
            }
        }
    }

    private void InterceptAsync(IInvocation invocation)
    {


        if (invocation.Method.ReturnType.IsGenericType && invocation.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var returnType = invocation.Method.ReturnType.GetGenericArguments()[0];
            var method = GetType().GetMethod(nameof(HandleAsyncWithResult), BindingFlags.NonPublic | BindingFlags.Instance)
                 ?.MakeGenericMethod(returnType)
                 ?? throw new InvalidOperationException("HandleAsyncWithResult metodu bulunamadı.");

            invocation.Proceed();
            if (invocation.ReturnValue is not Task task)
            {
                throw new InvalidOperationException("Beklenen Task sonucu elde edilemedi.");
            }

            invocation.ReturnValue = method.Invoke(this, new object[] { task }) ??
                throw new InvalidOperationException("Async transaction wrapper çağrısı başarısız.");
        }
        else
        {
            invocation.Proceed();
            if (invocation.ReturnValue is not Task task)
            {
                throw new InvalidOperationException("Beklenen Task sonucu elde edilemedi.");
            }

            invocation.ReturnValue = HandleAsync(task);
        }
    }

    private async Task HandleAsync(Task task)
    {
        using (var scope = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
        {
            try
            {
                await task;
                scope.Complete();
            }
            catch (Exception)
            {
                scope.Dispose();
                throw;
            }
        }
    }

    private async Task<T> HandleAsyncWithResult<T>(Task<T> task)
    {
        using (var scope = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
        {
            try
            {
                var result = await task;
                scope.Complete();
                return result;
            }
            catch (Exception)
            {
                scope.Dispose();
                throw;
            }
        }
    }
}
