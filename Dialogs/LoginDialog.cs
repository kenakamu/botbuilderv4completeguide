using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;

public class LoginDialog : ComponentDialog
{
    private const string connectionName = "AzureAdv2";
    private IStringLocalizer<LoginDialog> localizer;

    public LoginDialog(IStringLocalizer<LoginDialog> localizer) : base(nameof(LoginDialog))
    {
        this.localizer = localizer;
        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            LoginAsync,
            CompleteLoginAsync,
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("logindialog", waterfallSteps));
        AddDialog(new OAuthPrompt(
                "login",
                new OAuthPromptSettings
                {
                    ConnectionName = connectionName,
                    Text = localizer["signindialog"],
                    Title = localizer["signin"],
                    Timeout = 300000, // 5分でタイムアウトするように設定
                }));
    }

    private async Task<DialogTurnResult> LoginAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.BeginDialogAsync("login", cancellationToken: cancellationToken);
    }
    private async Task<DialogTurnResult> CompleteLoginAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // ログインの結果よりトークンを取得
        var tokenResponse = (TokenResponse)stepContext.Result;

        if (string.IsNullOrEmpty(tokenResponse.Token))
        {
            await stepContext.Context.SendActivityAsync(localizer["failed"], cancellationToken: cancellationToken);
            return await stepContext.EndDialogAsync("", cancellationToken);
        }
        else
            // 戻り値としてトークンを返す
            return await stepContext.EndDialogAsync(tokenResponse.Token, cancellationToken);
    }
}