using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

public class MyMiddleware : IMiddleware
{
    public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
    {
        var activity = turnContext.Activity;

        // 添付ファイルがある場合は処理を止める
        if (activity.Type == ActivityTypes.Message
            && activity.Attachments != null
            && activity.Attachments.Count != 0)
        {
            await turnContext.SendActivityAsync("テキストを送ってください");
        }
        // それ以外の場合は次の処理を実行
        else
            await next.Invoke(cancellationToken);
    }
}
