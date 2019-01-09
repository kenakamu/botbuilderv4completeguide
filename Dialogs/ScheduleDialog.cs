using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

public class ScheduleDialog : ComponentDialog
{
    private IServiceProvider serviceProvider;
    public ScheduleDialog(IServiceProvider serviceProvider) : base(nameof(ScheduleDialog))
    {
        this.serviceProvider = serviceProvider;

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

    private static async Task<DialogTurnResult> LoginAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.BeginDialogAsync(nameof(LoginDialog), cancellationToken: cancellationToken);
    }
    private static async Task<DialogTurnResult> GetScheduleAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // ログインの結果よりトークンを取得
        var accessToken = (string)stepContext.Result;

        if (!string.IsNullOrEmpty(accessToken))
        {
            var graphClient = new MSGraphService(accessToken);
            var events = await graphClient.GetScheduleAsync();
            events.ForEach(async x =>
            {
                await stepContext.Context.SendActivityAsync($"{System.DateTime.Parse(x.Start.DateTime).ToString("HH:mm")}-{System.DateTime.Parse(x.End.DateTime).ToString("HH:mm")} : {x.Subject}", cancellationToken: cancellationToken);
            });
        }
        else
            await stepContext.Context.SendActivityAsync($"サインインに失敗しました。", cancellationToken: cancellationToken);

        return await stepContext.EndDialogAsync(true, cancellationToken);
    }
}