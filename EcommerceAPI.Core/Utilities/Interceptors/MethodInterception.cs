
using Castle.DynamicProxy;
using System.Reflection;

namespace EcommerceAPI.Core.Utilities.Interceptors;

public abstract class MethodInterception : MethodInterceptionBaseAttribute
{
    protected virtual void OnBefore(IInvocation invocation) { }
    protected virtual void OnAfter(IInvocation invocation) { }
    protected virtual void OnException(IInvocation invocation, Exception e) { }
    protected virtual void OnSuccess(IInvocation invocation) { }

    public override void Intercept(IInvocation invocation)
    {
        var isAsync = IsAsyncMethod(invocation.Method);
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
        var isSuccess = true;
        OnBefore(invocation);
        try
        {
            invocation.Proceed();
        }
        catch (Exception e)
        {
            isSuccess = false;
            OnException(invocation, e);
            throw;
        }
        finally
        {
            if (isSuccess)
            {
                OnSuccess(invocation);
            }
        }
        OnAfter(invocation);
    }

    private void InterceptAsync(IInvocation invocation)
    {
        OnBefore(invocation);

        invocation.Proceed();

        if (invocation.Method.ReturnType.IsGenericType && invocation.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var returnType = invocation.Method.ReturnType.GetGenericArguments()[0];
            var handleMethod = typeof(MethodInterception).GetMethod(nameof(HandleAsyncWithResult), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(returnType);
            invocation.ReturnValue = handleMethod.Invoke(this, new object[] { (Task)invocation.ReturnValue, invocation });
        }
        else
        {
            invocation.ReturnValue = HandleAsync((Task)invocation.ReturnValue, invocation);
        }
    }

    private async Task HandleAsync(Task task, IInvocation invocation)
    {
        var isSuccess = true;
        try
        {
            await task;
        }
        catch (Exception e)
        {
            isSuccess = false;
            OnException(invocation, e);
            throw;
        }
        finally
        {
            if (isSuccess)
            {
                OnSuccess(invocation);
            }
            OnAfter(invocation);
        }
    }

    private async Task<T> HandleAsyncWithResult<T>(Task<T> task, IInvocation invocation)
    {
        var isSuccess = true;
        try
        {
            var result = await task;
            return result;
        }
        catch (Exception e)
        {
            isSuccess = false;
            OnException(invocation, e);
            throw;
        }
        finally
        {
            if (isSuccess)
            {
                OnSuccess(invocation);
            }
            OnAfter(invocation);
        }
    }

    private static bool IsAsyncMethod(MethodInfo method)
    {
        return (method.ReturnType == typeof(Task) ||
                (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)));
    }
}
