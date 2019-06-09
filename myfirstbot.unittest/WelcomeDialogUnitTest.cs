using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class WelcomeDialogUnitTest
    {
        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs, StringLocalizer<WelcomeDialog> localizer) ArrangeTest(string language)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            // リソースを利用するため StringLocalizer を作成
            var localizer = StringLocalizerFactory.GetStringLocalizer<WelcomeDialog>();

            // IServiceProvider のモック
            var serviceProvider = new Mock<IServiceProvider>();

            // WelcomeDialog クラスで解決すべきサービスを登録
            serviceProvider.Setup(x => x.GetService(typeof(ProfileDialog))).Returns(new ProfileDialog(accessors, null));
            serviceProvider.Setup(x => x.GetService(typeof(SelectLanguageDialog))).Returns(new SelectLanguageDialog(accessors));

            // テスト対象のダイアログをインスタンス化
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new WelcomeDialog(accessors, localizer, serviceProvider.Object));

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
                    await dialogContext.BeginDialogAsync(nameof(WelcomeDialog), null, cancellationToken);
                }
                // ダイアログが完了した場合は、UserProfile の名前をテスト側に返す
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    await turnContext.SendActivityAsync((await accessors.UserProfile.GetAsync(turnContext)).Name);
                }
            });

            return (testFlow, adapter, dialogs, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task WelcomeDialog_ShouldGoToProfileDialog(string language)
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
                // Activity にヒーローカードが含まれていることを確認。
                Assert.AreEqual((activity as Activity).Attachments.Count, 1);
                var heroCard = (activity as Activity).Attachments.First().Content as HeroCard;
                // ヒーローカードの内容を確認。
                Assert.AreEqual(heroCard.Title, arrange.localizer["title"]);
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["yes"]).First().Value, arrange.localizer["yes"].ToString());
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["skip"]).First().Value, arrange.localizer["skip"].ToString());
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["checkDetail"]).First().Value, "https://dev.botframework.com");
            })
            .Send(arrange.localizer["yes"])
            .AssertReply((activity) =>
            {
                // Activity とアダプターからコンテキストを作成
                var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                // ダイアログコンテキストを取得
                var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                // 現在のダイアログスタックの一番上が ProfileDialog で その下が welcome であることを確認。
                var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(WelcomeDialog)).First().State["dialogs"] as DialogState).DialogStack;
                Assert.AreEqual(dialogInstances[0].Id, nameof(ProfileDialog));
                Assert.AreEqual(dialogInstances[1].Id, "welcome");
            })
            .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task WelcomeDialog_ShouldSetAnonymous(string language)
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
                // Activity にヒーローカードが含まれていることを確認。
                Assert.AreEqual((activity as Activity).Attachments.Count, 1);
                var heroCard = (activity as Activity).Attachments.First().Content as HeroCard;
                // ヒーローカードの内容を確認。
                Assert.AreEqual(heroCard.Title, arrange.localizer["title"]);
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["yes"]).First().Value, arrange.localizer["yes"].ToString());
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["skip"]).First().Value, arrange.localizer["skip"].ToString());
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["checkDetail"]).First().Value, "https://dev.botframework.com");
            })
            .Send(arrange.localizer["skip"])
            .AssertReply((activity) =>
            {
                // 返ってきたテキストが匿名かを確認
                Assert.AreEqual((activity as Activity).Text, arrange.localizer["anonymous"].ToString());
            })
            .StartTestAsync();
        }
    }
}
