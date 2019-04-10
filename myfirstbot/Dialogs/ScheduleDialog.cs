using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;

public class ScheduleDialog : ComponentDialog
{
    private IStringLocalizer<ScheduleDialog> localizer;
    private MSGraphService graphClient;

    // public ScheduleDialog(IServiceProvider serviceProvider, IStringLocalizer<ScheduleDialog> localizer) : base(nameof(ScheduleDialog))
    private ScheduleNotificationStore scheduleNotificationStore;
    private MyStateAccessors accessors;
    public ScheduleDialog(MyStateAccessors accessors, IServiceProvider serviceProvider, IStringLocalizer<ScheduleDialog> localizer, ScheduleNotificationStore scheduleNotificationStore) : base(nameof(ScheduleDialog))
    {
        this.accessors = accessors;
        this.localizer = localizer;
        this.graphClient = (MSGraphService)serviceProvider.GetService(typeof(MSGraphService));

        this.scheduleNotificationStore = scheduleNotificationStore;
        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            LoginAsync,
            GetScheduleAsync,
            ProcessChoiceInputAsync,
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("schedule", waterfallSteps));
        AddDialog((LoginDialog)serviceProvider.GetService(typeof(LoginDialog)));
        AddDialog(new ChoicePrompt("choice"));
    }

    private async Task<DialogTurnResult> LoginAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.BeginDialogAsync(nameof(LoginDialog), cancellationToken: cancellationToken);
    }

    private async Task<DialogTurnResult> GetScheduleAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // ログインの結果よりトークンを取得
        var accessToken = (string)stepContext.Result;

        if (!string.IsNullOrEmpty(accessToken))
        {
            this.graphClient.Token = accessToken;
            var events = await graphClient.GetScheduleAsync();
            if( events.Count() > 0)
            {
                events.ForEach(async x =>
                {
                    await stepContext.Context.SendActivityAsync($"{System.DateTime.Parse(x.Start.DateTime).ToString("HH:mm")}-{System.DateTime.Parse(x.End.DateTime).ToString("HH:mm")} : {x.Subject}", cancellationToken: cancellationToken);
                });

                // ステートに取得した予定を保存
                await accessors.Events.SetAsync(stepContext.Context, events, cancellationToken);
                
                // Choice プロンプトで予定の一覧と通知不要の選択肢を表示
                var choices = ChoiceFactory.ToChoices(events.Select(x => $"{DateTime.Parse(x.Start.DateTime).ToString("HH:mm")}-{x.Subject}").ToList());
                choices.Add(new Choice(localizer["nonotification"]));
                return await stepContext.PromptAsync(
                    "choice",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(localizer["setnotification"]),
                        Choices = choices,
                    },
                    cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(localizer["noevents"], cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync(true, cancellationToken);
            }
        }
        else
        {
            await stepContext.Context.SendActivityAsync(localizer["failed"], cancellationToken: cancellationToken);
            return await stepContext.EndDialogAsync(false, cancellationToken);
        }
    }

    private async Task<DialogTurnResult> ProcessChoiceInputAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // 答えを確認して予定を取得
        var choice = (FoundChoice)stepContext.Result;
        
        var events = await accessors.Events.GetAsync(stepContext.Context, null, cancellationToken);
        var @event = events.Where(x=> $"{DateTime.Parse(x.Start.DateTime).ToString("HH:mm")}-{x.Subject}" == choice.Value).FirstOrDefault();

        if (@event != null)
        {
            // 開始時間と通知時間を取得
            var start = DateTime.Parse(@event.Start.DateTime);
            var reminderMinutesBeforeStartmin = @event.ReminderMinutesBeforeStart;
            var notificationTime = reminderMinutesBeforeStartmin == null ? start : start.AddMinutes(-double.Parse(reminderMinutesBeforeStartmin.ToString()));
            // 通知の情報を追加
            scheduleNotificationStore.Add(new ScheduleNotification
            {
                Title = @event.Subject,
                NotificationTime = notificationTime,
                StartTime = start,
                WebLink = @event.WebLink,
                ConversationReference = stepContext.Context.Activity.GetConversationReference(),
            });
            await stepContext.Context.SendActivityAsync(localizer["notificationset"], cancellationToken: cancellationToken);
            return await stepContext.EndDialogAsync(true, cancellationToken);
        }
        else
        {
            return await stepContext.EndDialogAsync(true, cancellationToken);
        }
    }
}