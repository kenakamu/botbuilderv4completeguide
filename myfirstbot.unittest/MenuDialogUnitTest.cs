using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MenuDialogUnitTest
    {
        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs) ArrangeTest()
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
            // テスト対象のダイアログをインスタンス化
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new MenuDialog());

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

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

            return (testFlow, adapter, dialogs);
        }

        [TestMethod]
        public async Task MenuDialog_ShouldGoToWeatherDialog()
        {
            var arrange = ArrangeTest();
            // テストの追加と実行
            await arrange.testFlow
            .Test("foo", "今日はなにをしますか? (1) 天気を確認 または (2) 予定を確認")
            .Send("天気を確認")
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
        public async Task MenuDialog_ShouldGoToScheduleDialog()
        {
            var arrange = ArrangeTest();
            await arrange.testFlow
            .Test("foo", "今日はなにをしますか? (1) 天気を確認 または (2) 予定を確認")
            .Send("予定を確認")
            .AssertReply((activity) =>
            {
                // Activity とアダプターからコンテキストを作成
                var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                // ダイアログコンテキストを取得
                var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                // 現在のダイアログスタックの一番上が ScheduleDialog　の choice であることを確認。
                var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                Assert.AreEqual(dialogInstances[0].Id, "choice");
            })
            .StartTestAsync();            
        }
    }
}
