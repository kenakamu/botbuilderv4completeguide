using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

public class ScheduleDialog : ComponentDialog
{
    private const string connectionName = "AzureAdv2";
    public ScheduleDialog() : base(nameof(ScheduleDialog))
    {
        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            LoginAsync,
            LoginStepAsync,
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("schedule", waterfallSteps));
        AddDialog(new OAuthPrompt(
                "login",
                new OAuthPromptSettings
                {
                    ConnectionName = connectionName,
                    Text = "サインインダイアログ",
                    Title = "サインイン",
                    Timeout = 300000, // 5分でタイムアウトするように設定
                }));
    }

    private static async Task<DialogTurnResult> LoginAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.BeginDialogAsync("login", cancellationToken: cancellationToken);
    }
    private static async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // ログインの結果よりトークンを取得
        var tokenResponse = (TokenResponse)stepContext.Result;

        await stepContext.Context.SendActivityAsync($"Token: {tokenResponse.Token}", cancellationToken: cancellationToken);
        return await stepContext.EndDialogAsync();
    }
}