using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;

public class ScheduleDialog : ComponentDialog
{
    private IStringLocalizer<ScheduleDialog> localizer;

    public ScheduleDialog(IServiceProvider serviceProvider, IStringLocalizer<ScheduleDialog> localizer) : base(nameof(ScheduleDialog))
    {
        this.localizer = localizer;
        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            LoginAsync,
            GetScheduleAsync,
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("schedule", waterfallSteps));
        AddDialog((LoginDialog)serviceProvider.GetService(typeof(LoginDialog)));
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
            events.ForEach(async x =>
            {
                await stepContext.Context.SendActivityAsync($"{System.DateTime.Parse(x.Start.DateTime).ToString("HH:mm")}-{System.DateTime.Parse(x.End.DateTime).ToString("HH:mm")} : {x.Subject}", cancellationToken: cancellationToken);
            });
        }
        else
            await stepContext.Context.SendActivityAsync(localizer["failed"], cancellationToken: cancellationToken);

        return await stepContext.EndDialogAsync(true, cancellationToken);
    }
}