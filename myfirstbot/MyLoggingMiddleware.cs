using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Bot.Builder;

public class MyLoggingMiddleware : IMiddleware
{
    public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
    {
        Debug.WriteLine($"{turnContext.Activity.From}:{turnContext.Activity.Type}");
        Debug.WriteLineIf(
            !string.IsNullOrEmpty(turnContext.Activity.Text),
            turnContext.Activity.Text);
        await next.Invoke(cancellationToken);
    }
}