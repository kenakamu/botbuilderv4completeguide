using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

public class SelectLanguageDialog : ComponentDialog
{
    private MyStateAccessors accessors;
    private static Dictionary<string, string> languages = new Dictionary<string, string>(){
        { "日本語", "ja-JP" },
        { "English", "en-US" }
    };
    public SelectLanguageDialog(MyStateAccessors accessors) : base(nameof(SelectLanguageDialog))
    {
        this.accessors = accessors;

        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            AskLangugage,
            SaveLangugageChoice
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("selectlangugae", waterfallSteps));
        AddDialog(new ChoicePrompt("choice"));
    }

    private async Task<DialogTurnResult> AskLangugage(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Choice プロンプトで言語選択を表示
        return await stepContext.PromptAsync(
            "choice",
            new PromptOptions
            {
                Prompt = MessageFactory.Text("言語を選択してください。Select your language"),
                Choices = ChoiceFactory.ToChoices(languages.Select(x=>x.Key).ToList()),
            },
            cancellationToken);
    }

    private async Task<DialogTurnResult> SaveLangugageChoice(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // 選択した言語をプロファイルに保存
        var languageChoice = (FoundChoice)stepContext.Result;
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
        userProfile.Language = languages[languageChoice.Value];
        // プロファイルを保存
        await accessors.UserProfile.SetAsync(stepContext.Context, userProfile, cancellationToken);
        return await stepContext.EndDialogAsync(true);
    }
}