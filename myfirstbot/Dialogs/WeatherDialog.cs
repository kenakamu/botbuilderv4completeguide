using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

public class WeatherDialog : ComponentDialog
{
    public WeatherDialog() : base(nameof(WeatherDialog))
    {
        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            ShowWeatherAsync,
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("weather", waterfallSteps));
    }

    public static async Task<DialogTurnResult> ShowWeatherAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        await stepContext.Context.SendActivityAsync("今日の天気は晴れです");
        return await stepContext.EndDialogAsync(true, cancellationToken);
    }
}