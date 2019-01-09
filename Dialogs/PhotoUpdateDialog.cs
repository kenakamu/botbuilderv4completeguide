using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;

public class PhotoUpdateDialog : ComponentDialog
{
    private IServiceProvider serviceProvider;
    private IStringLocalizer<PhotoUpdateDialog> localizer;

    public PhotoUpdateDialog(IServiceProvider serviceProvider, IStringLocalizer<PhotoUpdateDialog> localizer) : base(nameof(PhotoUpdateDialog))
    {
        this.serviceProvider = serviceProvider;
        this.localizer = localizer;

        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            LoginAsync,
            UpdatePhotoAsync,
            GetPhotoAsync,
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("updatephoto", waterfallSteps));
        AddDialog((LoginDialog)serviceProvider.GetService(typeof(LoginDialog)));
    }

    private async Task<DialogTurnResult> LoginAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // 認証ダイアログはテキストがないと落ちるため、ダミーを設定
        stepContext.Context.Activity.Text = "dummy";
        return await stepContext.BeginDialogAsync(nameof(LoginDialog), cancellationToken: cancellationToken);       
    }
    private async Task<DialogTurnResult> UpdatePhotoAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // ログインの結果よりトークンを取得
        var accessToken = (string)stepContext.Result;
        // 親ダイアログより渡されたイメージを取得

        // 添付ファイルを取得して MemoryStream に格納
        var connector = new ConnectorClient(new Uri(stepContext.Context.Activity.ServiceUrl));
        var image = await connector.HttpClient.GetStreamAsync(stepContext.Options.ToString());
        if (!string.IsNullOrEmpty(accessToken))
        {
            var graphClient = new MSGraphService(accessToken);
            await graphClient.UpdatePhotoAsync(image);
        }
        else
            await stepContext.Context.SendActivityAsync(localizer["failed"], cancellationToken: cancellationToken);

        return await stepContext.NextAsync(accessToken, cancellationToken);
    }

    private async Task<DialogTurnResult> GetPhotoAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // 前の処理よりトークンを取得
        var accessToken = (string)stepContext.Result;
        if (!string.IsNullOrEmpty(accessToken))
        {
            // 返信の作成
            var reply = stepContext.Context.Activity.CreateReply();
            // 現在の写真を取得
            var graphClient = new MSGraphService(accessToken);
            var image = await graphClient.GetPhotoAsync();
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = image.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                var image64 = System.Convert.ToBase64String(ms.ToArray());
                // 返信に画像を設定
                reply.Attachments.Add(new Attachment(
                    contentType: "image/png",
                    contentUrl: $"data:image/png;base64,{image64}"
                    ));
            }
            await stepContext.Context.SendActivityAsync(reply, cancellationToken);
        }
        else
            await stepContext.Context.SendActivityAsync(localizer["failed"], cancellationToken: cancellationToken);

        return await stepContext.EndDialogAsync(true, cancellationToken);
    }
}