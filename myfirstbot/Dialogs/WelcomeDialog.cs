using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;

public class WelcomeDialog : ComponentDialog
{
    private MyStateAccessors accessors;
    // WelcomeDialog 用のローカライザーを作成
    private IStringLocalizer<WelcomeDialog> localizer;

    public WelcomeDialog(MyStateAccessors accessors, IStringLocalizer<WelcomeDialog> localizer, IServiceProvider serviceProvider) : base(nameof(WelcomeDialog))
    {
        this.accessors = accessors;
        this.localizer = localizer;

        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
        CheckLanguageAsync,
        SendWelcomeHeroCardAsync,
        CheckProfileAsync
        };
        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("welcome", waterfallSteps));
        AddDialog(new TextPrompt("checkStatus"));
        AddDialog((ProfileDialog)serviceProvider.GetService(typeof(ProfileDialog)));
        AddDialog((SelectLanguageDialog)serviceProvider.GetService(typeof(SelectLanguageDialog)));
    }

    private async Task<DialogTurnResult> CheckLanguageAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
        if (string.IsNullOrEmpty(userProfile.Language))
            return await stepContext.BeginDialogAsync(nameof(SelectLanguageDialog), cancellationToken: cancellationToken);
        else
            return await stepContext.NextAsync(cancellationToken);
    }

    private async Task<DialogTurnResult> SendWelcomeHeroCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
        // このタイミングではミドルウェアが呼ばれていないため、明示的に設定
        Thread.CurrentThread.CurrentCulture = new CultureInfo(userProfile.Language);
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(userProfile.Language);

        // カルーセルを作成。ここでは 1 つだけ Attachment を設定するため
        // カルーセル形式にはならない。ボタンには imBack を設定
        var activity = MessageFactory.Carousel(
            new Attachment[]
            {
                new HeroCard(
                    title: localizer["title"],
                    images: new CardImage[] { new CardImage(url: "https://picsum.photos/300/200/?image=433") },
                    buttons: new CardAction[]
                    {
                        new CardAction(title:localizer["yes"], type: ActionTypes.PostBack, value: localizer["yes"].Value),
                        new CardAction(title: localizer["skip"], type: ActionTypes.ImBack, value: localizer["skip"].Value),
                        new CardAction(title: localizer["checkDetail"], type: ActionTypes.OpenUrl, value: "https://dev.botframework.com"),
                    })
                .ToAttachment(),
            });

        // TextPrompt を指定して文字列が返ること期待
        return await stepContext.PromptAsync("checkStatus", new PromptOptions
        {
            Prompt = (Activity)activity
        });
    }

    private async Task<DialogTurnResult> CheckProfileAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // 返事の内容によってプロファイル登録するか決定
        if (stepContext.Result.ToString() == localizer["yes"])
            return await stepContext.BeginDialogAsync(nameof(ProfileDialog), cancellationToken: cancellationToken);

        // 登録しない場合は匿名として設定
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
        userProfile.Name = localizer["anonymous"];
        return await stepContext.EndDialogAsync(true);
    }
}