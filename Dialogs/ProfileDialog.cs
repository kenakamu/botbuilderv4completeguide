using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

public class ProfileDialog : ComponentDialog
{
    private MyStateAccessors accessors;

    public ProfileDialog(MyStateAccessors accessors) : base(nameof(ProfileDialog))
    {
        this.accessors = accessors;

        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            NameStepAsync,
            NameConfirmStepAsync,
            AgeStepAsync,
            ConfirmStepAsync,
            SummaryStepAsync,
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("profile", waterfallSteps));
        AddDialog(new TextPrompt("name"));
        AddDialog(new NumberPrompt<int>("age"));
        AddDialog(new ConfirmPrompt("confirm"));
    }

    private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // "name" ダイアログ(プロンプト) を返信。
        // ユーザーに対する表示は PromptOptions で指定。
        return await stepContext.PromptAsync("name", new PromptOptions { Prompt = MessageFactory.Text("名前を入力してください。") }, cancellationToken);
    }

    private async Task<DialogTurnResult> NameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // UserProfile をステートより取得
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
        // Result より名前の更新
        userProfile.Name = (string)stepContext.Result;
        // 年齢を聞いてもいいか確認のため "confirm" ダイアログを送信。
        return await stepContext.PromptAsync("confirm", new PromptOptions { Prompt = MessageFactory.Text("年齢を聞いてもいいですか？") }, cancellationToken);
    }

    private async Task<DialogTurnResult> AgeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Result より結果の確認
        if ((bool)stepContext.Result)
        {
            // 年齢を聞いてもいい場合は "age" ダイアログを送信
            return await stepContext.PromptAsync("age", new PromptOptions { Prompt = MessageFactory.Text("年齢を入力してください。") }, cancellationToken);
        }
        else
        {
            // "いいえ" を選択した場合、次のステップに進む。"age" ダイアログの結果は "-1" を指定。
            return await stepContext.NextAsync(-1, cancellationToken);
        }
    }

    private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // 年齢の回答を取得
        var age = (int)stepContext.Result;
        // UserProfile をステートより取得
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
        // 年齢をスキップしなかった場合はユーザープロファイルに設定
        if (age != -1)
            userProfile.Age = age;
        // 全て正しいか確認。"confirm"　ダイアログを再利用。
        var prompt = $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{userProfile.Name} 年齢:{userProfile.Age}";
        return await stepContext.PromptAsync("confirm", new PromptOptions { Prompt = MessageFactory.Text(prompt) }, cancellationToken);
    }

    private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        if ((bool)stepContext.Result)
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("プロファイルを保存します。"));
        else
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("プロファイルを破棄します。"));

        // ダイアログを終了
        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
    }
}