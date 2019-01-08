using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
public class MyBot : IBot
{
    private MyStateAccessors accessors;
    private DialogSet dialogs;
    // DI で MyStateAccessors は自動解決
    public MyBot(MyStateAccessors accessors)
    {
        this.accessors = accessors;
        this.dialogs = new DialogSet(accessors.ConversationDialogState);

        // コンポーネントダイアログを追加
        dialogs.Add(new ProfileDialog(accessors));      
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
                // コンポーネントダイアログを送信
                await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
            }
            // DialogTurnStatus が Complete の場合、ダイアログは完了したため結果を処理
            else if (results.Status == DialogTurnStatus.Complete)
            {
                var userProfile = await accessors.UserProfile.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
                await turnContext.SendActivityAsync(MessageFactory.Text($"ようこそ '{userProfile.Name}' さん！"));
            }
            // 最後に現在の UserProfile と DialogState を保存
            await accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            await accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
    }    
}