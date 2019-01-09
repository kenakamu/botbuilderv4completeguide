using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

public class ProfileDialog : ComponentDialog
{
    private MyStateAccessors accessors;
    public ProfileDialog(MyStateAccessors accessors) : base(nameof(ProfileDialog))
    {
        this.accessors = accessors;

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
        var adaptiveCardJson = File.ReadAllText("./AdaptiveJsons/Profile.json");
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
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

        var input = JsonConvert.DeserializeObject<UserProfile>(stepContext.Context.Activity.Value.ToString());

        await stepContext.Context.SendActivityAsync(MessageFactory.Text("プロファイルを保存します。"));
        // 必要に応じて入れる値を確認したり変更したりする。
        userProfile.Name = input.Name;
        userProfile.Age = input.Age;
        userProfile.Email = input.Email;
        userProfile.Phone = input.Phone;

        // プロファイルの保存
        await accessors.UserProfile.SetAsync(stepContext.Context, userProfile, cancellationToken);
        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
    }
}