using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

public class WelcomeDialog : ComponentDialog
{
    private MyStateAccessors accessors;

    public WelcomeDialog(MyStateAccessors accessors) : base(nameof(WelcomeDialog))
    {
        this.accessors = accessors;

        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            SendWelcomeHeroCardAsync,
            CheckProfileAsync
        };
        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("welcome", waterfallSteps));
        AddDialog(new TextPrompt("checkStatus"));
        AddDialog(new ProfileDialog(accessors));
    }

    private async Task<DialogTurnResult> SendWelcomeHeroCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // カルーセルを作成。ここでは 1 つだけ Attachment を設定するため
        // カルーセル形式にはならない。ボタンには imBack を設定
        var activity = MessageFactory.Carousel(
    new Attachment[]
    {
        new HeroCard
        (
            title: "ようこそ My Bot へ！プロファイル登録をしますか？",
            images: new CardImage[] { new CardImage(url: "https://picsum.photos/300/200/?image=433") },
            buttons: new CardAction[]
            {
                new CardAction(title:"はい", type: ActionTypes.ImBack, value: "はい"),
                new CardAction(title: "スキップ", type: ActionTypes.ImBack, value: "スキップ"),
                new CardAction(ActionTypes.ShowImage, title: "Azure Bot Service", value: "https://picsum.photos/300/200/?image=433"),
            }
        ).ToAttachment(),
        new AnimationCard
        (
            title: "アニメーションサンプル",
            image: new ThumbnailUrl("https://docs.microsoft.com/en-us/bot-framework/media/how-it-works/architecture-resize.png"),
            media: new List<MediaUrl>()
            {
                new MediaUrl(url: "http://i.giphy.com/Ki55RUbOV5njy.gif")
            }
        ).ToAttachment(),
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
        if (stepContext.Result.ToString() == "はい")
            return await stepContext.BeginDialogAsync(nameof(ProfileDialog), cancellationToken: cancellationToken);

        // 登録しない場合は匿名として設定
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
        userProfile.Name = "匿名";
        return await stepContext.EndDialogAsync(true);
    }
}