using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CognitiveServices.Translator;
using CognitiveServices.Translator.Translate;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;

public class MyBot : IBot
{
    private MyStateAccessors accessors;
    private LuisRecognizer luisRecognizer;
    private DialogSet dialogs;
    private IStringLocalizer<MyBot> localizer;
    private ITranslateClient translator;
    private IServiceProvider serviceProvider;

    // DI で MyStateAccessors および luisRecognizer は自動解決
    public MyBot(MyStateAccessors accessors, LuisRecognizer luisRecognizer,
        IStringLocalizer<MyBot> localizer, IServiceProvider serviceProvider,
        ITranslateClient translator)
    {
        this.accessors = accessors;
        this.luisRecognizer = luisRecognizer;
        this.dialogs = new DialogSet(accessors.ConversationDialogState);
        this.localizer = localizer;
        this.translator = translator;
        this.serviceProvider = serviceProvider;
        // コンポーネントダイアログを追加
        dialogs.Add((WelcomeDialog)serviceProvider.GetService(typeof(WelcomeDialog)));
        dialogs.Add((ProfileDialog)serviceProvider.GetService(typeof(ProfileDialog)));
        dialogs.Add((MenuDialog)serviceProvider.GetService(typeof(MenuDialog)));
        dialogs.Add((WeatherDialog)serviceProvider.GetService(typeof(WeatherDialog)));
        dialogs.Add((ScheduleDialog)serviceProvider.GetService(typeof(ScheduleDialog)));
        dialogs.Add((PhotoUpdateDialog)serviceProvider.GetService(typeof(PhotoUpdateDialog)));
    }

    // 指定したクラスに対応するリソースを取得
    public List<string> GetResourceStrings<T>()
    {
        var localizer = (IStringLocalizer<T>)serviceProvider.GetService(
                    typeof(IStringLocalizer<T>));
        return localizer.GetAllStrings().Select(x => x.Value).ToList();
    }

    private async Task ContinueDialog(ITurnContext turnContext, DialogContext dialogContext, 
        UserProfile userProfile, CancellationToken cancellationToken)
    {
        // まず ContinueDialogAsync を実行して既存のダイアログがあれば継続実行。
        var results = await dialogContext.ContinueDialogAsync(cancellationToken);

        // DialogTurnStatus が Complete または Empty の場合、メニューへ。
        if (results.Status == DialogTurnStatus.Complete || results.Status == DialogTurnStatus.Empty)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text(String.Format(localizer["welcome"], userProfile.Name)));
            // メニューの表示
            await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
        }
    }

    public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
    {
        // DialogSet からコンテキストを作成
        var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);
        // プロファイルを取得
        var userProfile = await accessors.UserProfile.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

        // ユーザーからメッセージが来た場合
        if (turnContext.Activity.Type == ActivityTypes.Message)
        {
            if (turnContext.Activity.Attachments != null)
            {
                // 添付ファイルのアドレスを取得
                var attachment = turnContext.Activity.Attachments.First();
                var attachmentUrl = attachment.ContentUrl;
                // PhotoUpdateDialog に対して画像のアドレスを渡す
                await dialogContext.BeginDialogAsync(nameof(PhotoUpdateDialog), attachmentUrl, cancellationToken);
            }
            else if (string.IsNullOrEmpty(turnContext.Activity.Text))
            {
                // Text がないためダイアログをそのまま継続
                await dialogContext.ContinueDialogAsync(cancellationToken);
            }
            else
            {                
                // 現在のダイアログのリソースを取得
                var activeDialog = dialogContext.ActiveDialog.Id;
                List<string> strings = (List<string>)
                    typeof(MyBot).GetMethod("GetResourceStrings").MakeGenericMethod(new Type[] { Type.GetType(activeDialog) }).Invoke(this, null);

                // リソースのテキストだった場合そのままダイアログを実行
                if (strings.Where(x => x == turnContext.Activity.Text).FirstOrDefault() != null)
                {
                    await ContinueDialog(turnContext, dialogContext, userProfile, cancellationToken);
                }
                else
                {
                    // 日本語以外の言語の処理のため元のテキストを保存
                    var originalInput = turnContext.Activity.Text;

                    // 言語が日本語でない場合は一旦翻訳
                    if (userProfile.Language != "ja-JP" && !string.IsNullOrEmpty(turnContext.Activity.Text))
                    {
                        var translateParams = new RequestParameter
                        {
                            From = userProfile.Language,
                            To = new[] { "ja" },
                            IncludeAlignment = true,
                        };

                        // LUIS に投げるために、元のインプットを変換したものに差し替え。
                        turnContext.Activity.Text =
                        (await translator.TranslateAsync(
                            new RequestContent(turnContext.Activity.Text), translateParams)
                        ).First().Translations.First().Text;
                    }

                    var luisResult = await luisRecognizer.RecognizeAsync(turnContext, cancellationToken);
                    var topIntent = luisResult?.GetTopScoringIntent();
                    if (topIntent != null && topIntent.HasValue)
                    {
                        if (topIntent.Value.intent == "Cancel")
                        {
                            await turnContext.SendActivityAsync(localizer["cancel"], cancellationToken: cancellationToken);
                            await dialogContext.CancelAllDialogsAsync(cancellationToken);
                            await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
                        }
                        else if (topIntent.Value.intent == "Logout")
                        {
                            // アダプターを取得
                            var botAdapter = (BotFrameworkAdapter)turnContext.Adapter;
                            // 指定した接続をログアウト
                            await botAdapter.SignOutUserAsync(turnContext, "AzureAdv2", cancellationToken: cancellationToken);
                            await turnContext.SendActivityAsync(localizer["loggedout"], cancellationToken: cancellationToken);
                            var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                            // DialogTurnStatus が Complete または Empty の場合、メニューへ。
                            if (results.Status == DialogTurnStatus.Complete || results.Status == DialogTurnStatus.Empty)
                                // メニューの表示
                                await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
                        }
                        else if (topIntent.Value.intent == "Schedule")
                        {
                            await dialogContext.BeginDialogAsync(nameof(ScheduleDialog), null, cancellationToken);
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
                        else
                        {
                            // ヘルプの場合は使い方を言って、そのまま処理継続
                            if (topIntent.Value.intent == "Help")
                            {
                                await turnContext.SendActivityAsync(localizer["help"], cancellationToken: cancellationToken);
                            }

                            // LUIS で分類できなかったため、元に戻す
                            turnContext.Activity.Text = originalInput;
                            // まず ContinueDialogAsync を実行して既存のダイアログがあれば継続実行。
                            await ContinueDialog(turnContext, dialogContext, userProfile, cancellationToken);
                        }
                    }
                }
            }

            // ユーザーに応答できなかった場合
            if (!turnContext.Responded)
            {
                await turnContext.SendActivityAsync(localizer["resetall"], cancellationToken: cancellationToken);
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
                if (userProfile == null || string.IsNullOrEmpty(userProfile.Name))
                {
                    //await turnContext.SendActivityAsync("ようこそ MyBot へ！");
                    await dialogContext.BeginDialogAsync(nameof(WelcomeDialog), null, cancellationToken);
                    //await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
                }
                else
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(string.Format(localizer["welcome"], userProfile.Name)));
                    if (userProfile.HasCat)
                        await turnContext.SendActivityAsync(MessageFactory.Text(string.Format(localizer["howarecats"], userProfile.CatNum)));

                    // メニューの表示
                    await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
                }
            }
        }
        else if (turnContext.Activity.Type == ActivityTypes.Event || turnContext.Activity.Type == ActivityTypes.Invoke)
        {
            // Event または Invoke で戻ってきた場合ダイアログを続ける
            await dialogContext.ContinueDialogAsync(cancellationToken);
            if (!turnContext.Responded)
            {
                await turnContext.SendActivityAsync(localizer["resetall"], cancellationToken: cancellationToken);
                await dialogContext.CancelAllDialogsAsync(cancellationToken);
                await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
            }
        }

        // 最後に現在の UserProfile と DialogState を保存
        await accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        await accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
    }
}