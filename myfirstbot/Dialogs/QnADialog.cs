using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Localization;
using Microsoft.Bot.Builder.AI.QnA;
using CognitiveServices.Translator;
using CognitiveServices.Translator.Translate;

public class QnADialog : ComponentDialog
{
    private MyStateAccessors accessors;
    private QnAMaker qnaMaker;
    private ITranslateClient translator;
    private IStringLocalizer<QnADialog> localizer;

    public QnADialog(MyStateAccessors accessors, QnAMaker qnaMaker, ITranslateClient translator, IStringLocalizer<QnADialog> localizer) : base(nameof(QnADialog))
    {
        this.accessors = accessors;
        this.qnaMaker = qnaMaker;
        this.translator = translator;
        this.localizer = localizer;
        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            AskQuestionAsync,
            ReplyAnswerAsync,
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("qna", waterfallSteps));
        AddDialog(new TextPrompt("question"));
    }

    private async Task<DialogTurnResult> AskQuestionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // 戻り値は文字型のため、TextPrompt ダイアログを使って送信
        return await stepContext.PromptAsync("question", new PromptOptions
        {
            Prompt = MessageFactory.Text(localizer["whatisquestion"])
        },
        cancellationToken: cancellationToken);
    }

    private async Task<DialogTurnResult> ReplyAnswerAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var userProfile = await accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

        // 日本語ではない場合、日本語に翻訳
        if (userProfile.Language != "ja-JP")
        {
            var translateParams = new RequestParameter
            {
                From = userProfile.Language,
                To = new[] { "ja" },
                IncludeAlignment = true,
            };

            // LUIS に投げるために、元のインプットを変換したものに差し替え。
            stepContext.Context.Activity.Text =
            (await translator.TranslateAsync(
                new RequestContent(stepContext.Context.Activity.Text), translateParams)
            ).First().Translations.First().Text;
        }

        var answer = await qnaMaker.GetAnswersAsync(stepContext.Context);
        if (answer.FirstOrDefault() == null || answer.First().Score == 0)
            await stepContext.Context.SendActivityAsync(localizer["noanswer"]);
        else
        {
            var answerText = answer.First().Answer;
            // 日本語以外の場合は元の言語に戻す
            if (userProfile.Language != "ja-JP")
            {
                var translateParams = new RequestParameter
                {
                    From = "ja-JP",
                    To = new[] { userProfile.Language },
                    IncludeAlignment = true,
                };

                // LUIS に投げるために、元のインプットを変換したものに差し替え。
                answerText =
                (await translator.TranslateAsync(
                    new RequestContent(answerText), translateParams)
                ).First().Translations.First().Text;
            }
            // 答えを返す
            await stepContext.Context.SendActivityAsync(answerText);
        }
        return await stepContext.EndDialogAsync(true, cancellationToken);
    }
}