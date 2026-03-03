using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;

namespace ProductManagementSystem.Api.Controllers.ModelBinders;

public class ArrayModelBinders : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if(!bindingContext.ModelMetadata.IsEnumerableType)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var providedValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).ToString();
        providedValue = providedValue.Trim('(', ')');

        if (string.IsNullOrEmpty(providedValue))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        var objectArray = providedValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        Console.WriteLine($"Object Model: {JsonSerializer.Serialize(objectArray)}");

        var objArray = objectArray.Select(x => Convert.ToInt32(x));

        bindingContext.Model = objArray;

        bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);

        return Task.CompletedTask;
        
    }
}
