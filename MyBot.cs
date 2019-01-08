using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
public class MyBot : IBot
{
    private MyStateAccessors accessors;
    private DialogSet dialogs;

    public MyBot(MyStateAccessors accessors)
    {
        this.accessors = accessors;
        this.dialogs = new DialogSet(accessors.ConversationDialogState);
        // テキスト型のプロンプトとして id=name で作成
        dialogs.Add(new TextPrompt("name"));
    }

    public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (turnContext.Activity.Type == ActivityTypes.Message)
        {
            // DialogSet からコンテキストを作成
            var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);
            // まず ContinueDialogAsync を実行して既存のダイアログがあれば継続実行。
            var results = await dialogContext.ContinueDialogAsync(cancellationToken);

            // DialogTurnStatus が Empty の場合は既存のダイアログがないため、新規に実行
            if (results.Status == DialogTurnStatus.Empty)
            {
                // id=name のプロンプトを送信
                await dialogContext.PromptAsync(
                    "name",
                    new PromptOptions { Prompt = MessageFactory.Text("名前を入力してください") }, // ユーザーに対する表示
                    cancellationToken);
            }
            // DialogTurnStatus が Complete の場合、ダイアログは完了したため結果を処理
            else if (results.Status == DialogTurnStatus.Complete)
            {
                if (results.Result != null)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"ようこそ '{results.Result}' さん！"));
                }
            }
            // 最後に現在のダイアログステートを保存
            await accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
    }
}