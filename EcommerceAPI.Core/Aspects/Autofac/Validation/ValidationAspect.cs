
using Castle.DynamicProxy;
using EcommerceAPI.Core.CrossCuttingConcerns.Validation;
using EcommerceAPI.Core.Utilities.Interceptors;
using FluentValidation;
using EcommerceAPI.Core.Utilities.IoC;

namespace EcommerceAPI.Core.Aspects.Autofac.Validation;

public class ValidationAspect : MethodInterception
{
    private Type _validatorType;
    public ValidationAspect(Type validatorType)
    {
        if (!typeof(IValidator).IsAssignableFrom(validatorType))
        {
            throw new Exception("Wrong Validation Type");
        }

        _validatorType = validatorType;
    }

    protected override void OnBefore(IInvocation invocation)
    {
        var validator = (IValidator?)ServiceTool.ServiceProvider.GetService(_validatorType);
        if (validator == null)
        {
             validator = (IValidator)Activator.CreateInstance(_validatorType)!;
        }

        var entityType = _validatorType.BaseType!.GetGenericArguments()[0];
        var entities = invocation.Arguments
            .Where(t => t is not null && entityType.IsAssignableFrom(t.GetType()));
        foreach (var entity in entities)
        {
            ValidationTool.Validate(validator, entity);
        }
    }
}
