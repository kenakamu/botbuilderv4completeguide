using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
public class MyBot : IBot
{
    public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
    {
        // ユーザーのメッセージにだけ返信
        // Text プロパティの値をオウム返し
        if(turnContext.Activity.Type == ActivityTypes.Message
            && !string.IsNullOrEmpty(turnContext.Activity.Text))
            await turnContext.SendActivityAsync(turnContext.Activity.Text);
    }
}