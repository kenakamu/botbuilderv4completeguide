using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class PhotoUpdateDialogUnitTest
    {
        private string attachmentUrl = "https://github.com/apple-touch-icon.png";

        private TestFlow ArrangeTestFlow()
        {
            // ストレージとしてインメモリを利用
            IStorage dataStore = new MemoryStorage();
            // それぞれのステートを作成
            var conversationState = new ConversationState(dataStore);
            var userState = new UserState(dataStore);
            var accessors = new MyStateAccessors(userState, conversationState)
            {
                // DialogState を ConversationState のプロパティとして設定
                ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState"),
                // UserProfile を作成
                UserProfile = userState.CreateProperty<UserProfile>("UserProfile")
            };

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
            
            // テスト対象のダイアログをインスタンス化
            var loginDialog = new LoginDialog();
            // OAuthPrompt をテスト用のプロンプトに差し替え
            loginDialog.ReplaceDialog(new TestOAuthPrompt("login", new OAuthPromptSettings()));
            var photoUpdateDialog = new PhotoUpdateDialog(msGraphService);
            // ログインダイアログを上記でつくったものに差し替え
            photoUpdateDialog.ReplaceDialog(loginDialog);
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(photoUpdateDialog);
            dialogs.Add(loginDialog);

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // TestFlow の作成
            return new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(PhotoUpdateDialog), attachmentUrl, cancellationToken);
                }
            });
        }

        [TestMethod]
        public async Task PhotoUpdateDialogShouldUpdateAndReturnPicture()
        {            
            await ArrangeTestFlow()
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Attachments.Count == 1);
            })
            .StartTestAsync();
        }
    }
}
