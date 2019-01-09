using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Linq;
using System;
using Microsoft.Extensions.Localization;

public class MenuDialog : ComponentDialog
{    
    static private Dictionary<string, string> menus;
    static private IList<Choice> choices;
    private IStringLocalizer<MenuDialog> localizer;

    public MenuDialog(IServiceProvider serviceProvider, IStringLocalizer<MenuDialog> localizer) : base(nameof(MenuDialog))
    {
        this.localizer = localizer;
        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            InitializeAsync,
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

    public async Task<DialogTurnResult> InitializeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // メニューの作成。表示される文字と子ダイアログの名称をセットで登録
        menus = new Dictionary<string, string>(){
            { localizer["checkweather"], nameof(WeatherDialog) },
            { localizer["checkschedule"], nameof(ScheduleDialog) },
        };

        // ChoiceFactory で選択肢に設定する IList<Choice> を作成
        choices = ChoiceFactory.ToChoices(menus.Select(x => x.Key).ToList());
        return await stepContext.NextAsync();
    }

    public async Task<DialogTurnResult> ShowMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Choice プロンプトでメニューを表示
        return await stepContext.PromptAsync(
            "choice",
            new PromptOptions
            {
                Prompt = MessageFactory.Text(localizer["choicemenu"]),
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