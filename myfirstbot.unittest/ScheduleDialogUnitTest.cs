using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Graph;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class ScheduleDialogUnitTest
    {
        // ダミーの予定用の時刻
        DateTime datetime = DateTime.Now;

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
            // ダミーの予定を返す。
            mockGraphSDK.Setup(x => x.Me.CalendarView.Request(It.IsAny<List<QueryOption>>()).GetAsync())
                .ReturnsAsync(() =>
                {
                    var page = new UserCalendarViewCollectionPage();
                    page.Add(new Event()
                    {
                        Subject = "Dummy 1",
                        Start = new DateTimeTimeZone() { DateTime = datetime.ToString() },
                        End = new DateTimeTimeZone() { DateTime = datetime.AddMinutes(30).ToString() }
                    });
                    page.Add(new Event()
                    {
                        Subject = "Dummy 2",
                        Start = new DateTimeTimeZone() { DateTime = datetime.AddMinutes(60).ToString() },
                        End = new DateTimeTimeZone() { DateTime = datetime.AddMinutes(90).ToString() }
                    });
                    return page;
                });

            // IServiceProvider のモック
            var serviceProvider = new Mock<IServiceProvider>();

            // ScheduleDialog クラスで解決すべきサービスを登録
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog());
            serviceProvider.Setup(x => x.GetService(typeof(MSGraphService))).Returns(new MSGraphService(mockGraphSDK.Object));

            // テスト対象のダイアログをインスタンス化
            var loginDialog = new LoginDialog();
            // OAuthPrompt をテスト用のプロンプトに差し替え
            loginDialog.ReplaceDialog(new TestOAuthPrompt("login", new OAuthPromptSettings()));
            var scheduleDialog = new ScheduleDialog(serviceProvider.Object);
            // ログインダイアログを上記でつくったものに差し替え
            scheduleDialog.ReplaceDialog(loginDialog);
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(scheduleDialog);
            dialogs.Add(loginDialog);

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // TestFlow の作成
            return new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(ScheduleDialog), null, cancellationToken);
                }
            });
        }

        [TestMethod]
        public async Task ScheduleDialog_ShouldReturnEvents()
        {
            await ArrangeTestFlow()
            .Send("foo")
            .AssertReply($"{datetime.ToString("HH:mm")}-{datetime.AddMinutes(30).ToString("HH:mm")} : Dummy 1")
            .AssertReply($"{datetime.AddMinutes(60).ToString("HH:mm")}-{datetime.AddMinutes(90).ToString("HH:mm")} : Dummy 2")
            .StartTestAsync();
        }
    }
}
