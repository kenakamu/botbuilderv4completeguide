using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class PhotoUpdateDialogUnitTest
    {
        private string attachmentUrl = "https://github.com/apple-touch-icon.png";

        private (TestFlow testFlow, StringLocalizer<PhotoUpdateDialog> localizer) ArrangeTest(string language)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            // リソースを利用するため StringLocalizer を作成
            var localizer = StringLocalizerFactory.GetStringLocalizer<PhotoUpdateDialog>();

            // Microsoft Graph 系のモック
            var mockGraphSDK = new Mock<IGraphServiceClient>();
            // プロファイル写真の操作をモック
            mockGraphSDK.Setup(x => x.Me.Photo.Content.Request(null).PutAsync(It.IsAny<Stream>()))
                .Returns(Task.FromResult(default(Stream)));

            mockGraphSDK.Setup(x => x.Me.Photo.Content.Request(null).GetAsync())
                .Returns(async () =>
                {
                    return new MemoryStream();
                });

            var msGraphService = new MSGraphService(mockGraphSDK.Object);

            // IServiceProvider のモック
            var serviceProvider = new Mock<IServiceProvider>();

            // PhotoUpdateDialog クラスで解決すべきサービスを登録
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(MSGraphService))).Returns(new MSGraphService(mockGraphSDK.Object));

            // テスト対象のダイアログをインスタンス化
            var loginDialog = new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>());
            // OAuthPrompt をテスト用のプロンプトに差し替え
            loginDialog.ReplaceDialog(new TestOAuthPrompt("login", new OAuthPromptSettings()));
            var photoUpdateDialog = new PhotoUpdateDialog(serviceProvider.Object, localizer);
            // ログインダイアログを上記でつくったものに差し替え
            photoUpdateDialog.ReplaceDialog(loginDialog);
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(photoUpdateDialog);
            dialogs.Add(loginDialog);

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(accessors.UserState, accessors.ConversationState));

            // TestFlow の作成
            var testFlow = new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(PhotoUpdateDialog), attachmentUrl, cancellationToken);
                }
            });

            return (testFlow, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task PhotoUpdateDialogShouldUpdateAndReturnPicture(string language)
        {
            // 言語を指定してテストを作成
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Attachments.Count == 1);
            })
            .StartTestAsync();
        }
    }
}