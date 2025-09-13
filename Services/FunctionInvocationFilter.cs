using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace SupportPilotAgent.Services
{
    public class FunctionInvocationFilter : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Log before function execution
            Console.WriteLine($"[FUNCTION CALL] Invoking: {context.Function.PluginName}.{context.Function.Name}");
            
            if (context.Arguments.Count > 0)
            {
                Console.WriteLine($"[FUNCTION ARGS] Arguments:");
                foreach (var arg in context.Arguments)
                {
                    var value = arg.Value?.ToString();
                    var truncatedValue = value?.Length > 100 ? value.Substring(0, 100) + "..." : value;
                    Console.WriteLine($"  - {arg.Key}: {truncatedValue}");
                }
            }
            
            try
            {
                // Execute the function
                await next(context);
                
                stopwatch.Stop();
                
                // Log after successful execution
                Console.WriteLine($"[FUNCTION RESULT] Completed: {context.Function.PluginName}.{context.Function.Name} in {stopwatch.ElapsedMilliseconds}ms");
                
                if (context.Result != null)
                {
                    var resultValue = context.Result.ToString();
                    var truncatedResult = resultValue?.Length > 200 ? resultValue.Substring(0, 200) + "..." : resultValue;
                    Console.WriteLine($"[FUNCTION OUTPUT] Result: {truncatedResult}");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Log error
                Console.WriteLine($"[FUNCTION ERROR] Failed: {context.Function.PluginName}.{context.Function.Name} in {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"[FUNCTION ERROR] Error: {ex.Message}");
                
                // Re-throw the exception to maintain normal error handling
                throw;
            }
            
            Console.WriteLine($"[FUNCTION END] ─────────────────────────────────────────");
        }
    }
}