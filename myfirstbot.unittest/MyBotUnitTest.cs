using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            var accessors = AccessorsFactory.GetAccessors("ja-JP", returnUserProfile);

            // IServiceProvider のモック
            var serviceProvider = new Mock<IServiceProvider>();

            // MyBot クラスで解決すべきサービスを登録
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(WeatherDialog))).Returns(new WeatherDialog(accessors, StringLocalizerFactory.GetStringLocalizer<WeatherDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(ProfileDialog))).Returns(new ProfileDialog(accessors, StringLocalizerFactory.GetStringLocalizer<ProfileDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(SelectLanguageDialog))).Returns(new SelectLanguageDialog(accessors));
            serviceProvider.Setup(x => x.GetService(typeof(WelcomeDialog))).Returns
                (new WelcomeDialog(accessors, null, serviceProvider.Object));
            serviceProvider.Setup(x => x.GetService(typeof(ScheduleDialog))).Returns(new ScheduleDialog(serviceProvider.Object, StringLocalizerFactory.GetStringLocalizer<ScheduleDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(MenuDialog))).Returns(new MenuDialog(serviceProvider.Object, StringLocalizerFactory.GetStringLocalizer<MenuDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(PhotoUpdateDialog))).Returns(new PhotoUpdateDialog(serviceProvider.Object, StringLocalizerFactory.GetStringLocalizer<PhotoUpdateDialog>()));


            // IRecognizer のモック化
            var mockRecognizer = new Mock<IRecognizer>();
            mockRecognizer.Setup(l => l.RecognizeAsync(It.IsAny<TurnContext>(), It.IsAny<CancellationToken>()))
                .Returns((TurnContext turnContext, CancellationToken cancellationToken) =>
                {
                    // RecognizerResult の作成
                    var recognizerResult = new RecognizerResult()
                    {
                        Intents = new Dictionary<string, IntentScore>(),
                        Entities = new JObject()
                    };

                    switch (turnContext.Activity.Text)
                    {
                        case "キャンセル":
                            recognizerResult.Intents.Add("Cancel", new IntentScore() { Score = 1 });
                            break;
                        case "天気を確認":
                            recognizerResult.Intents.Add("Weather", new IntentScore() { Score = 1 });
                            break;
                        case "今日の天気を確認":
                            recognizerResult.Intents.Add("Weather", new IntentScore() { Score = 1 });
                            recognizerResult.Entities.Add("day", JArray.Parse("[['今日']]"));
                            break;
                        case "ヘルプ":
                            recognizerResult.Intents.Add("Help", new IntentScore() { Score = 1 });
                            break;
                        case "プロファイルの変更":
                            recognizerResult.Intents.Add("Profile", new IntentScore() { Score = 1 });
                            break;
                        default:
                            recognizerResult.Intents.Add("None", new IntentScore() { Score = 1 });
                            break;
                    }
                    return Task.FromResult(recognizerResult);
                });
            // テスト対象のクラスをインスタンス化
            var bot = new MyBot(accessors, mockRecognizer.Object, serviceProvider.Object);

            // 差し替える必要があるものを差し替え
            var photoUpdateDialog = new DummyDialog(nameof(PhotoUpdateDialog));
            bot.ReplaceDialog(photoUpdateDialog);

            // DialogSet を作成したクラスより Refactor
            var dialogSet = (DialogSet)typeof(MyBot).GetField("dialogs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bot);
            // TestFlow の作成
            var testFlow = new TestFlow(adapter, bot.OnTurnAsync);
            return (testFlow, adapter, dialogSet);
        }

        [TestMethod]
        public async Task MyBot_ShouldGoToWeatherDialog()
        {
            var arrange = ArrangeTest(true);

            // テストの追加と実行
            await arrange.testFlow
                .Send("天気を確認")
                .AssertReply((activity) =>
                {
                    // Activity とアダプターからコンテキストを作成
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // ダイアログコンテキストを取得
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // 現在のダイアログスタックの一番上が WeatherDialog の choice であることを確認。
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(WeatherDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "date");
                })
                .StartTestAsync();
        }
       
        [TestMethod]
        public async Task MyBot_ShouldGoToHelpDialog()
        {
            var arrange = ArrangeTest(false);

            // テストの追加と実行
            await arrange.testFlow
                .Test("ヘルプ", "天気と予定が確認できます。")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldGoToSelectLanguageDialogWithConversationUpdateWithoutUserProfile()
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
                .AssertReply((activity) =>
                {
                    // Activity とアダプターからコンテキストを作成
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // ダイアログコンテキストを取得
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // 現在のダイアログスタックの一番上が SelectLanguageDialog であることを確認。
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(WelcomeDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "SelectLanguageDialog");
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
        public async Task MyBot_ShouldGoToPhotoUpdateDialog()
        {
            var arrange = ArrangeTest(true);
            var attachmentActivity = new Activity(ActivityTypes.Message)
            {
                Id = "test",
                From = new ChannelAccount("TestUser", "Test User"),
                ChannelId = "UnitTest",
                ServiceUrl = "https://example.org",
                Attachments = new List<Microsoft.Bot.Schema.Attachment>()
                {
                    new Microsoft.Bot.Schema.Attachment(
                        "image/pgn",
                        "https://github.com/apple-touch-icon.png"
                    )
                }
            };

            await arrange.testFlow
            .Send(attachmentActivity)
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
                .AssertReply((activity) =>
                {
                    //// Activity とアダプターからコンテキストを作成
                    //var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    //// ダイアログコンテキストを取得
                    //var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    //// 現在のダイアログスタックの一番上が MenuDialog の choice であることを確認。
                    //var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    //Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .Send("天気を確認")
                .AssertReply((activity) =>
                {
                    //    // Activity とアダプターからコンテキストを作成
                    //    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    //    // ダイアログコンテキストを取得
                    //    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    //    // 現在のダイアログスタックの一番上が WeatherDialog の choice であることを確認。
                    //    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(WeatherDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    //    Assert.AreEqual(dialogInstances[0].Id, "choice");
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
                .AssertReply((activity) =>
                {
                    //// Activity とアダプターからコンテキストを作成
                    //var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    //// ダイアログコンテキストを取得
                    //var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    //// 現在のダイアログスタックの一番上が MenuDialog の choice であることを確認。
                    //var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    //Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .Send("天気を確認")
                .AssertReply((activity) =>
                {
                    //// Activity とアダプターからコンテキストを作成
                    //var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    //// ダイアログコンテキストを取得
                    //var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    //// 現在のダイアログスタックの一番上が WeatherDialog の choice であることを確認。
                    //var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(WeatherDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    //Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .Send("プロファイルの変更")
                .AssertReply((activity) =>
                {
                    // Activity とアダプターからコンテキストを作成
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // ダイアログコンテキストを取得
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // 現在のダイアログスタックの一番上が ProfileDialog の name であることを確認。
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(ProfileDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "adaptive");
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
