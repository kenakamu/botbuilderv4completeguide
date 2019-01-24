using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MyBotUnitTest
    {
        // テスト用変数
        string name = "Ken";

        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs) ArrangeTest(bool returnUserProfile)
        {
            // アダプターを作成
            var adapter = new TestAdapter();
            adapter.Use(new SetLocaleMiddleware(Culture.Japanese));
            // ストレージとしてモックのストレージを利用
            var mock = new Mock<IStorage>();
            // User1用に返すデータを作成
            // UserState のキーは <channelId>/users/<userId>
            var dictionary = new Dictionary<string, object>();
            if (returnUserProfile)
            {
                dictionary.Add("test/users/user1", new Dictionary<string, object>()
                {
                    { "UserProfile", new UserProfile() { Name = name, Age = 0 } }
                });
            }
            // ストレージへの読み書きを設定
            mock.Setup(ms => ms.WriteAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .Returns((Dictionary<string, object> dic, CancellationToken token) =>
                {
                    foreach (var dicItem in dic)
                    {
                        if (dicItem.Key != "test/users/user1")
                        {
                            if (dictionary.ContainsKey(dicItem.Key))
                            {
                                dictionary[dicItem.Key] = dicItem.Value;
                            }
                            else
                            {
                                dictionary.Add(dicItem.Key, dicItem.Value);
                            }
                        }
                    }

                    return Task.CompletedTask;
                });
            mock.Setup(ms => ms.ReadAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    return Task.FromResult(result: (IDictionary<string, object>)dictionary);
                });

            // それぞれのステートを作成
            var conversationState = new ConversationState(mock.Object);
            var userState = new UserState(mock.Object);
            var accessors = new MyStateAccessors(userState, conversationState)
            {
                // DialogState を ConversationState のプロパティとして設定
                ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState"),
                // UserProfile を作成
                UserProfile = userState.CreateProperty<UserProfile>("UserProfile")
            };

            // テスト対象のダイアログをインスタンス化
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new ProfileDialog(accessors));
            dialogs.Add(new MenuDialog());

            // テスト対象のクラスをインスタンス化
            var bot = new MyBot(accessors);

            // TestFlow の作成
            var testFlow = new TestFlow(adapter, bot.OnTurnAsync);
            return (testFlow, adapter, dialogs);
        }

        [TestMethod]
        public async Task MyBot_ShouldGoToProfileDialogWithConversationUpdateWithoutUserProfile()
        {
            var arrange = ArrangeTest(false);

            var conversationUpdateActivity = new Activity(ActivityTypes.ConversationUpdate)
            {
                Id = "test",
                From = new ChannelAccount("TestUser", "Test User"),
                ChannelId = "UnitTest",
                ServiceUrl = "https://example.org",
                MembersAdded = new List<ChannelAccount>() { new ChannelAccount("TestUser", "Test User") }
            };

            // テストの追加と実行
            await arrange.testFlow
                .Send(conversationUpdateActivity)
                .AssertReply("ようこそ MyBot へ！")
                .AssertReply((activity) =>
                {
                    // Activity とアダプターからコンテキストを作成
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // ダイアログコンテキストを取得
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // 現在のダイアログスタックの一番上が ProfileDialog の name であることを確認。
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(ProfileDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "name");
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldGoToMenuDialogWithConversationUpdateWithUserProfile()
        {
            var arrange = ArrangeTest(true);

            var conversationUpdateActivity = new Activity(ActivityTypes.ConversationUpdate)
            {
                Id = "test",
                From = new ChannelAccount("TestUser", "Test User"),
                ChannelId = "UnitTest",
                ServiceUrl = "https://example.org",
                MembersAdded = new List<ChannelAccount>() { new ChannelAccount("TestUser", "Test User") }
            };

            // テストの追加と実行
            await arrange.testFlow
                .Send(conversationUpdateActivity)
                .AssertReply($"ようこそ '{name}' さん！")
                .AssertReply((activity) =>
                {
                    // Activity とアダプターからコンテキストを作成
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // ダイアログコンテキストを取得
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // 現在のダイアログスタックの一番上が MenuDialog の choice であることを確認。
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldWelcomeAndMenuDialogWithMessage()
        {
            var arrange = ArrangeTest(true);

            // テストの追加と実行
            await arrange.testFlow
                .Test("foo", $"ようこそ '{name}' さん！")
                .AssertReply((activity) =>
                {
            // Activity とアダプターからコンテキストを作成
            var turnContext = new TurnContext(arrange.adapter, activity as Activity);
            // ダイアログコンテキストを取得
            var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // 現在のダイアログスタックの一番上が MenuDialog の choice であることを確認。
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_GlobalCommand_ShouldCancelAllDialog()
        {
            var arrange = ArrangeTest(true);

            // テストの追加と実行
            await arrange.testFlow
                .Test("foo", $"ようこそ '{name}' さん！")
                .AssertReply("今日はなにをしますか? (1) 天気を確認 または (2) 予定を確認")
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
                .Test("キャンセル", "キャンセルします")
                .AssertReply((activity) =>
                {
                    // Activity とアダプターからコンテキストを作成
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // ダイアログコンテキストを取得
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // 現在のダイアログスタックの一番上が MenuDialog の choice であることを確認。
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_GlobalCommand_ShouldGoToProfileDialog()
        {
            var arrange = ArrangeTest(true);

            // テストの追加と実行
            await arrange.testFlow
                .Test("foo", $"ようこそ '{name}' さん！")
                .AssertReply("今日はなにをしますか? (1) 天気を確認 または (2) 予定を確認")
                .Send("天気を確認")
                .AssertReply((activity) =>
                {
                    // Activity とアダプターからコンテキストを作成
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // ダイアログコンテキストを取得
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // 現在のダイアログスタックの一番上が WeatherDialog であることを確認。
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, nameof(WeatherDialog));
                })
                .Send("プロファイルの変更")
                .AssertReply((activity) =>
                {
                    // Activity とアダプターからコンテキストを作成
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // ダイアログコンテキストを取得
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // 現在のダイアログスタックの一番上が ProfileDialog でその下が MenuDialog であることを確認。
                    // WeatherDialog は MenuDialog の最上部にある
                    Assert.AreEqual(dc.Stack[0].Id, nameof(ProfileDialog));
                    Assert.AreEqual(dc.Stack[1].Id, nameof(MenuDialog));

                    // ProfileDialog ダイアログスタックの一番上が ProfileDialog の name であることを確認。
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(ProfileDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "name");
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyMiddleware_ShouldStopProcessingWithAttachment()
        {            
            // アダプターを作成し、利用するミドルウェアを追加。
            var adapter = new TestAdapter()
                .Use(new MyMiddleware());

            // 添付ファイルを送る
            var activityWithAttachment = new Activity(ActivityTypes.Message)
            {
                Attachments = new List<Attachment>() { new Attachment() }
            };

            // テストの実行
            await new TestFlow(adapter)
            .Send(activityWithAttachment)
            .AssertReply("テキストを送ってください")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task MyMiddleware_ShouldProcessingWithoutAttachment()
        {
            var nextMiddlewareCalled = false;

            // 登録したミドルウェがすべて呼ばれた後に呼び出されるコールバック
            Task ValidateMiddleware(ITurnContext turnContext, CancellationToken cancellationToken)
            {
                // 今回は turnContext の中身を検証する必要はないため、
                // 次のミドルウェアが呼び出されたこと自体で検証を成功とする。
                nextMiddlewareCalled = true;
                return Task.CompletedTask;
            }
            // MiddlewareSet にテスト対象のミドルウェアを追加。
            var middlewareSet = new MiddlewareSet();
            middlewareSet.Use(new MyMiddleware());

            // テキストメッセージと ITurnContext を作成。
            var activityWithoutAttachment = new Activity(ActivityTypes.Message)
            {
                Text = "foo"
            };
            var ctx = new TurnContext(new TestAdapter(), activityWithoutAttachment);

            // MiddlewareSet にメッセージを送信。
            await middlewareSet.ReceiveActivityWithStatusAsync(ctx, ValidateMiddleware, default(CancellationToken));

            Assert.IsTrue(nextMiddlewareCalled);
        }
    }
}
