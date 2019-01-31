using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
public class MyBot : IBot
{
    private MyStateAccessors accessors;
    private IRecognizer luisRecognizer;
    private DialogSet dialogs;
    // DI で MyStateAccessors および luisRecognizer は自動解決
    public MyBot(MyStateAccessors accessors, IRecognizer luisRecognizer)
    {
        this.accessors = accessors;
        this.luisRecognizer = luisRecognizer;
        this.dialogs = new DialogSet(accessors.ConversationDialogState);

        // コンポーネントダイアログを追加
        dialogs.Add(new ProfileDialog(accessors));
        dialogs.Add(new MenuDialog());
        dialogs.Add(new WeatherDialog());
        dialogs.Add(new ScheduleDialog());
    }

    public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
    {
        // DialogSet からコンテキストを作成
        var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

        // ユーザーからメッセージが来た場合
        if (turnContext.Activity.Type == ActivityTypes.Message)
        {
            var luisResult = await luisRecognizer.RecognizeAsync(turnContext, cancellationToken);
            var topIntent = luisResult?.GetTopScoringIntent();
            if (topIntent != null && topIntent.HasValue)
            {
                if (topIntent.Value.intent == "Cancel")
                {
                    // Cancel any dialog on the stack.
                    await turnContext.SendActivityAsync("キャンセルします", cancellationToken: cancellationToken);
                    await dialogContext.CancelAllDialogsAsync(cancellationToken);
                    await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
                }
                else if (topIntent.Value.intent == "Profile")
                {
                    await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
                }
                else if (topIntent.Value.intent == "Weather")
                {
                    var day = luisResult.Entities["day"] == null ? null : luisResult.Entities["day"][0][0].ToString();
                    await dialogContext.BeginDialogAsync(nameof(WeatherDialog), day, cancellationToken);
                }
                else if (topIntent.Value.intent == "Schedule")
                {
                    await dialogContext.BeginDialogAsync(nameof(ScheduleDialog), null, cancellationToken);
                }
                else if (topIntent.Value.intent == "Logout")
                {
                    // アダプターを取得
                    var botAdapter = (BotFrameworkAdapter)turnContext.Adapter;
                    // 指定した接続をログアウト
                    await botAdapter.SignOutUserAsync(turnContext, "AzureAdv2", cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync("ログアウトしました。", cancellationToken: cancellationToken);
                    var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                    // DialogTurnStatus が Complete または Empty の場合、メニューへ。
                    if (results.Status == DialogTurnStatus.Complete || results.Status == DialogTurnStatus.Empty)
                        // メニューの表示
                        await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
                }
                else
                {
                    // ヘルプの場合は使い方を言って、そのまま処理継続
                    if (topIntent.Value.intent == "Help")
                    {
                        await turnContext.SendActivityAsync("天気と予定が確認できます。", cancellationToken: cancellationToken);
                    }
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
            }

            // ユーザーに応答できなかった場合
            if (!turnContext.Responded)
            {
                await turnContext.SendActivityAsync("わかりませんでした。全てキャンセルします。", cancellationToken: cancellationToken);
                await dialogContext.CancelAllDialogsAsync(cancellationToken);
                await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
            }
        }
        else if (turnContext.Activity.Type == ActivityTypes.Event || turnContext.Activity.Type == ActivityTypes.Invoke)
        {
            // Event または Invoke で戻ってきた場合ダイアログを続ける
            await dialogContext.ContinueDialogAsync(cancellationToken);
            if (!turnContext.Responded)
            {
                await turnContext.SendActivityAsync("わかりませんでした。全てキャンセルします。", cancellationToken: cancellationToken);
                await dialogContext.CancelAllDialogsAsync(cancellationToken);
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
                var userProfile = await accessors.UserProfile.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
                if (userProfile == null || string.IsNullOrEmpty(userProfile.Name))
                {
                    await turnContext.SendActivityAsync("ようこそ MyBot へ！");
                    await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
                }
                else
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"ようこそ '{userProfile.Name}' さん！"));
                    // メニューの表示
                    await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
                }
            }
        }

        // 最後に現在の UserProfile と DialogState を保存
        await accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        await accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
    }
}