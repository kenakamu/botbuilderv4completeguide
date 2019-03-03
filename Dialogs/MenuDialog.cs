using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Extensions.Localization;

public class MenuDialog : ComponentDialog
{
    // メニューの作成。表示される文字と子ダイアログの名称をセットで登録
    private static Dictionary<string, string> menus = new Dictionary<string, string>(){
        { "天気を確認", nameof(WeatherDialog) },
        { "予定を確認", nameof(ScheduleDialog) }
    };

    // ChoiceFactory で選択肢に設定する IList<Choice> を作成
    private static IList<Choice> choices = ChoiceFactory.ToChoices(menus.Select(x => x.Key).ToList());
    private IServiceProvider serviceProvider;
    public MenuDialog(IServiceProvider serviceProvider) : base(nameof(MenuDialog))
    {
        this.serviceProvider = serviceProvider;

        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            ShowMenuAsync,
            ProcessInputAsync,
            LoopMenu
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("menu", waterfallSteps));
        AddDialog(new ChoicePrompt("choice"));
        AddDialog((WeatherDialog)serviceProvider.GetService(typeof(WeatherDialog)));
        AddDialog((ScheduleDialog)serviceProvider.GetService(typeof(ScheduleDialog)));
    }

    public async Task<DialogTurnResult> ShowMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Choice プロンプトでメニューを表示
        return await stepContext.PromptAsync(
            "choice",
            new PromptOptions
            {
                Prompt = MessageFactory.Text("今日はなにをしますか?"),
                Choices = choices,
            },
            cancellationToken);
    }

    private async Task<DialogTurnResult> ProcessInputAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // 答えを確認して次のダイアログ名を取得
        var choice = (FoundChoice)stepContext.Result;
        var dialogId = menus[choice.Value];
        // 子ダイアログの実行
        return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
    }

    private async Task<DialogTurnResult> LoopMenu(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // スタックに乗せないように、Replace でメニューを再表示
        return await stepContext.ReplaceDialogAsync("menu", null, cancellationToken);
    }
}