using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

public class ScheduleDialog : ComponentDialog
{
    public ScheduleDialog() : base(nameof(ScheduleDialog))
    {
        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            ShowScheduleAsync,
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("schedule", waterfallSteps));
    }

    public static async Task<DialogTurnResult> ShowScheduleAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        await stepContext.Context.SendActivityAsync("今日は予定はありません。");
        return await stepContext.EndDialogAsync(true, cancellationToken);
    }
}