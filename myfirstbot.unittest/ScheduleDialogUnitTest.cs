using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Localization;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class ScheduleDialogUnitTest
    {
        // ダミーの予定用の時刻
        DateTime datetime = DateTime.Now;

        private (TestFlow testFlow, StringLocalizer<ScheduleDialog> localizer) ArrangeTest(string language)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            // リソースを利用するため StringLocalizer を作成
            var localizer = StringLocalizerFactory.GetStringLocalizer<ScheduleDialog>();

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
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(MSGraphService))).Returns(new MSGraphService(mockGraphSDK.Object));

            // テスト対象のダイアログをインスタンス化
            var loginDialog = new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>());
            // OAuthPrompt をテスト用のプロンプトに差し替え
            loginDialog.ReplaceDialog(new TestOAuthPrompt("login", new OAuthPromptSettings()));
            var scheduleDialog = new ScheduleDialog(serviceProvider.Object, localizer);
            // ログインダイアログを上記でつくったものに差し替え
            scheduleDialog.ReplaceDialog(loginDialog);
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(scheduleDialog);
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
                    await dialogContext.BeginDialogAsync(nameof(ScheduleDialog), null, cancellationToken);
                }
            });

            return (testFlow, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task ScheduleDialog_ShouldReturnEvents(string language)
        {
            // 言語を指定してテストを作成
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply($"{datetime.ToString("HH:mm")}-{datetime.AddMinutes(30).ToString("HH:mm")} : Dummy 1")
            .AssertReply($"{datetime.AddMinutes(60).ToString("HH:mm")}-{datetime.AddMinutes(90).ToString("HH:mm")} : Dummy 2")
            .StartTestAsync();
        }
    }
}