using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

public class ProfileDialog : ComponentDialog
{
    private MyStateAccessors accessors;
    private static IList<Choice> choices = ChoiceFactory.ToChoices(
        new List<string>() { "はい", "名前を変更する", "年齢を変更する" }
    );

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
        AddDialog(new NumberPrompt<int>("age", NumberValidators.ValidateRangeAsync));
        AddDialog(new ConfirmPrompt("confirm"));
        AddDialog(new ChoicePrompt("choice"));
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

        // 年齢を既に聞いている場合
        if (userProfile.Age != 0)
        {
            // ウォーターフォールダイアログの 4 ステップ目を実行するため、
            // 3 ステップ目を設定してから NextAsync を実行。(0 が初めのステップ)
            // stepIndex には現在のステップが入る。
            stepContext.ActiveDialog.State["stepIndex"] = 2;
            // 次のステップ (4 ステップ目) に年齢を渡して実行
            return await stepContext.NextAsync(userProfile.Age);
        }

        // 年齢を聞いてもいいか確認のため "confirm" ダイアログを送信。
        return await stepContext.PromptAsync("confirm", new PromptOptions { Prompt = MessageFactory.Text("年齢を聞いてもいいですか？") }, cancellationToken);
    }

    private async Task<DialogTurnResult> AgeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Result より結果の確認
        if ((bool)stepContext.Result)
        {
            var numberRange = new NumberRange() { MinValue = 0, MaxValue = 120 };
            // 年齢を聞いてもいい場合は "age" ダイアログを送信
            return await stepContext.PromptAsync(
                "age",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("年齢を入力してください。"),
                    RetryPrompt = MessageFactory.Text($"{numberRange.MinValue}-{numberRange.MaxValue} の数字で入力してください。"),
                    // 検証プロパティに NumberRange を設定
                    Validations = numberRange,
                },
                cancellationToken);
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

        // 全て正しいか確認。"choice"　ダイアログを利用。
        // Choice プロンプトでメニューを表示
        return await stepContext.PromptAsync(
            "choice",
            new PromptOptions
            {
                Prompt = MessageFactory.Text($"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{userProfile.Name} 年齢:{userProfile.Age}"),
                Choices = choices,
            },
            cancellationToken);
    }

    private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

        var choice = (FoundChoice)stepContext.Result;
        switch (choice.Value)
        {
            case "名前を変更する":
                // 名前は初めに聞くのでダイアログごと Replace
                return await stepContext.ReplaceDialogAsync("profile");
            case "年齢を変更する":
                // 年齢の確認は 3 ステップ目
                stepContext.ActiveDialog.State["stepIndex"] = 1;
                // 前処理の結果として true の場合年齢を聞くので、true を渡す
                return await stepContext.NextAsync(true);
            case "はい":
            default:
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("プロファイルを保存します。"));
                // ダイアログを終了
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}