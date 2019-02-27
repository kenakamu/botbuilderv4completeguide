using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

public class WeatherDialog : ComponentDialog
{
    // 既定で今日の天気を表示
    private string date = "今日";
    public WeatherDialog() : base(nameof(WeatherDialog))
    {
        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            ShowWeatherAsync,
            CheckUserInputAsync,
            CheckDateAsync,
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("weather", waterfallSteps));
        AddDialog(new TextPrompt("date"));
    }

    private async Task<DialogTurnResult> ShowWeatherAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {        
        // アダプティブカードの定義を JSON ファイルより読込み、対象日を変更
        var adaptiveCardJson = File.ReadAllText("./AdaptiveJsons/Weather.json").Replace("{0}", date);
        var adaptiveCardAttachment = new Attachment()
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = JsonConvert.DeserializeObject(adaptiveCardJson),
        };

        // 返信をコンテキストより作成
        var reply = stepContext.Context.Activity.CreateReply();
        reply.Attachments = new List<Attachment>() { adaptiveCardAttachment };

        // 戻り値は文字型のため、TextPrompt ダイアログを使って送信
        return await stepContext.PromptAsync("date", new PromptOptions
        {
            Prompt = reply
        },
        cancellationToken: cancellationToken);
    }

    private async Task<DialogTurnResult> CheckUserInputAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var input = stepContext.Result.ToString();

        if (input == "終了")
            return await stepContext.EndDialogAsync();

        // 他の日を選択するようにアダプティブカードを作成
        var adaptiveCardJson = File.ReadAllText("./AdaptiveJsons/WeatherDateChoice.json");
        var adaptiveCardAttachment = new Attachment()
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = JsonConvert.DeserializeObject(adaptiveCardJson),
        };

        var reply = stepContext.Context.Activity.CreateReply();
        reply.Attachments = new List<Attachment>() { adaptiveCardAttachment };

        // 戻り値は文字型のため、TextPrompt ダイアログを使って送信
        return await stepContext.PromptAsync("date", new PromptOptions
        {
            Prompt = reply
        },
        cancellationToken: cancellationToken);
    }

    private async Task<DialogTurnResult> CheckDateAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // 結果を date に指定してダイアログを再利用
        date = stepContext.Result.ToString();
        return await stepContext.ReplaceDialogAsync("weather");
    }
}