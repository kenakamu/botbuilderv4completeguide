using System;
using System.Linq;
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
        dialogs.Add(new MenuDialog());
    }

    public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
    {
        // DialogSet からコンテキストを作成
        var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

        // ユーザーからメッセージが来た場合
        if (turnContext.Activity.Type == ActivityTypes.Message)
        {
            // まず ContinueDialogAsync を実行して既存のダイアログがあれば継続実行。
            var results = await dialogContext.ContinueDialogAsync(cancellationToken);

            // DialogTurnStatus が Complete または Empty の場合、メニューへ。
            if (results.Status == DialogTurnStatus.Complete || results.Status == DialogTurnStatus.Empty)
            {
                var userProfile = await accessors.UserProfile.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
                await turnContext.SendActivityAsync(MessageFactory.Text($"ようこそ '{userProfile.Name}' さん！"));
                // メニューの表示
                await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
            }
        }
        // ユーザーとボットが会話に参加した
        else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
        {
            // turnContext より Activity を取得
            var activity = turnContext.Activity.AsConversationUpdateActivity();
            // ユーザーの参加に対してだけ、プロファイルダイアログを開始
            if (activity.MembersAdded.Any(member => member.Id != activity.Recipient.Id))
            {
                await turnContext.SendActivityAsync("ようこそ MyBot へ！");
                await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
            }
        }

        // 最後に現在の UserProfile と DialogState を保存
        await accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        await accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
    }
}