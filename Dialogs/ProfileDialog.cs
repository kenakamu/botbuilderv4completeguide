using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

public class ProfileDialog : ComponentDialog
{
    private MyStateAccessors accessors;
    private IStringLocalizer<ProfileDialog> localizer;

    public ProfileDialog(MyStateAccessors accessors, IStringLocalizer<ProfileDialog> localizer) : base(nameof(ProfileDialog))
    {
        this.accessors = accessors;
        this.localizer = localizer;

        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            //AdaptiveAsync,
            ProfileStepAsync,
            SummaryStepAsync,
        };
        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("profile", waterfallSteps));
        AddDialog(new TextPrompt("adaptive", AdaptiveCardResponseValidators.ValidateInput));
    }

    private async Task<DialogTurnResult> ProfileStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, ()=> new UserProfile(), cancellationToken);
        var adaptiveCardJson = File.ReadAllText($"./AdaptiveJsons/{userProfile.Language}/Profile.json");
        var adaptiveCardAttachment = new Attachment()
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = JsonConvert.DeserializeObject(adaptiveCardJson),
        };

        // 返信をコンテキストより作成
        var reply = stepContext.Context.Activity.CreateReply();
        reply.Attachments = new List<Attachment>() { adaptiveCardAttachment };

        // 戻り値は複雑な型だが一旦 TextPrompt ダイアログを使って送信
        return await stepContext.PromptAsync("adaptive", new PromptOptions
        {
            Prompt = reply,
            Validations = new NumberRange() { MinValue = 0, MaxValue = 120 }
        },
        cancellationToken: cancellationToken);
    }

    private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var input = JsonConvert.DeserializeObject<UserProfile>(stepContext.Context.Activity.Value.ToString());
        // 使用言語は現在のプロファイルよりコピー
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, ()=> new UserProfile(), cancellationToken);
        input.Language = userProfile.Language;
        await stepContext.Context.SendActivityAsync(MessageFactory.Text(localizer["save"]));
        await accessors.UserProfile.SetAsync(stepContext.Context, input, cancellationToken);
        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
    }
}