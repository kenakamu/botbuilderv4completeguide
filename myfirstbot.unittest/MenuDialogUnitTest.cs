using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MenuDialogUnitTest
    {
        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs, StringLocalizer<MenuDialog> localizer) ArrangeTest(string language)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            // リソースを利用するため StringLocalizer を作成
            var localizer = StringLocalizerFactory.GetStringLocalizer<MenuDialog>();

            // IServiceProvider のモック
            var serviceProvider = new Mock<IServiceProvider>();

            // MenuDialog クラスで解決すべきサービスを登録
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(WeatherDialog))).Returns(new WeatherDialog(accessors, StringLocalizerFactory.GetStringLocalizer<WeatherDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(ScheduleDialog))).Returns(new ScheduleDialog(accessors, serviceProvider.Object, StringLocalizerFactory.GetStringLocalizer<ScheduleDialog>(), new ScheduleNotificationStore()));
            serviceProvider.Setup(x => x.GetService(typeof(QnADialog))).Returns(new QnADialog(accessors,null, null, StringLocalizerFactory.GetStringLocalizer<QnADialog>()));

            // テスト対象のダイアログをインスタンス化
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new MenuDialog(serviceProvider.Object, localizer));

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(accessors.UserState, accessors.ConversationState));

            // TestFlow の作成
            var testFlow = new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
                }
            });

            return (testFlow, adapter, dialogs, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MenuDialog_ShouldGoToWeatherDialog(string language)
        {
            // 言語を指定してテストを作成
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            // テストの追加と実行
            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["choicemenu"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkweather"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkschedule"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkqa"]) >= 0);
            })
            .Send(arrange.localizer["checkweather"])
            .AssertReply((activity) =>
            {
            // Activity とアダプターからコンテキストを作成
            var turnContext = new TurnContext(arrange.adapter, activity as Activity);
            // ダイアログコンテキストを取得
            var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
            // 現在のダイアログスタックの一番上が WeatherDialog で その下が MenuDialog であることを確認。
            var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                Assert.AreEqual(dialogInstances[0].Id, nameof(WeatherDialog));
            })
            .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        [ExpectedException(typeof(System.Exception), "OAuthPrompt.GetUserToken(): not supported by the current adapter")]

        public async Task MenuDialog_ShouldGoToScheduleDialog(string language)
        {
            // 言語を指定してテストを作成
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["choicemenu"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkweather"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkschedule"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkqa"]) >= 0);
            })
            // 予定を確認を送った時点で OAuthPrompt.GetUserToken(): not supported by the current adapter エラーが出る
            .Test(arrange.localizer["checkschedule"], "dummy")
            .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MenuDialog_ShouldGoToQnADialog(string language)
        {
            // 言語を指定してテストを作成
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            // テストの追加と実行
            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["choicemenu"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkweather"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkschedule"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkqa"]) >= 0);
            })
            .Send(arrange.localizer["checkqa"])
            .AssertReply((activity) =>
            {
                // Activity とアダプターからコンテキストを作成
                var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                // ダイアログコンテキストを取得
                var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                // 現在のダイアログスタックの一番上が QnADialog であることを確認。
                var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                Assert.AreEqual(dialogInstances[0].Id, nameof(QnADialog));
            })
            .StartTestAsync();
        }
    }
}