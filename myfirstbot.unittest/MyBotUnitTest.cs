using CognitiveServices.Translator;
using CognitiveServices.Translator.Translate;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
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

        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs) ArrangeTest(string language, bool returnUserProfile)
        {
            // 言語を指定してアクセサーを作成
            var accessors = AccessorsFactory.GetAccessors(language, returnUserProfile);

            // アダプターを作成
            var adapter = new TestAdapter()
                .Use(new SetLanguageMiddleware(accessors.UserProfile));
       
            // IServiceProvider のモック
            var serviceProvider = new Mock<IServiceProvider>();

            // MyBot クラスで解決すべきサービスを登録
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(WeatherDialog))).Returns(new WeatherDialog(accessors, StringLocalizerFactory.GetStringLocalizer<WeatherDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(ProfileDialog))).Returns(new ProfileDialog(accessors, StringLocalizerFactory.GetStringLocalizer<ProfileDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(SelectLanguageDialog))).Returns(new SelectLanguageDialog(accessors));
            serviceProvider.Setup(x => x.GetService(typeof(WelcomeDialog))).Returns
                (new WelcomeDialog(accessors, null, serviceProvider.Object));
            serviceProvider.Setup(x => x.GetService(typeof(ScheduleDialog))).Returns(new ScheduleDialog(accessors, serviceProvider.Object, StringLocalizerFactory.GetStringLocalizer<ScheduleDialog>(), new ScheduleNotificationStore()));
            serviceProvider.Setup(x => x.GetService(typeof(QnADialog))).Returns(new QnADialog(accessors, null, null, StringLocalizerFactory.GetStringLocalizer<QnADialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(MenuDialog))).Returns(new MenuDialog(serviceProvider.Object, StringLocalizerFactory.GetStringLocalizer<MenuDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(PhotoUpdateDialog))).Returns(new PhotoUpdateDialog(serviceProvider.Object, StringLocalizerFactory.GetStringLocalizer<PhotoUpdateDialog>()));
            // 各ダイアログの StringLocalizer を追加
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<LoginDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<LoginDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<WeatherDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<WeatherDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<ProfileDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<ProfileDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<SelectLanguageDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<SelectLanguageDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<WelcomeDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<WelcomeDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<ScheduleDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<ScheduleDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<MenuDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<MenuDialog>());

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
            
            // 翻訳サービスのモック化
            var mockTranslateClient = new Mock<ITranslateClient>();
            mockTranslateClient.Setup(l => l.TranslateAsync(It.IsAny<RequestContent>(), It.IsAny<RequestParameter>()))
                .Returns((RequestContent requestContent, RequestParameter requestParameter) =>
                {
                    var response = new List<ResponseBody>();
                    switch (requestContent.Text)
                    {
                        case "Cancel":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "キャンセル" } } });
                            break;
                        case "Check weather":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "天気を確認" } } });
                            break;
                        case "Check today's weather":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "今日の天気を確認" } } });
                            break;
                        case "Help":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "ヘルプ" } } });
                            break;
                        case "Update profile":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "プロファイルの変更" } } });
                            break;
                        default:
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "foo" } } });
                            break;
                    }
                    return Task.FromResult(response as IList<ResponseBody>);
                });

            // MyBot でリソースを利用するため StringLocalizer を作成
            var localizer = StringLocalizerFactory.GetStringLocalizer<MyBot>();

            // テスト対象のクラスをインスタンス化
            var bot = new MyBot(accessors, mockRecognizer.Object, localizer, serviceProvider.Object, mockTranslateClient.Object);

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
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_ShouldGoToWeatherDialog(string language)
        {
            var arrange = ArrangeTest(language, true);

            // テストの追加と実行
            await arrange.testFlow
                .Send(language == "ja-JP" ? "天気を確認" : "Check weather")
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
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_ShouldGoToHelpDialog(string language)
        {
            var arrange = ArrangeTest(language, true);

            // テストの追加と実行
            await arrange.testFlow
                .Test(language == "ja-JP" ? "ヘルプ":"Help", language == "ja-JP" ? "天気と予定が確認できます。": "You can check weather and your schedule")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldGoToSelectLanguageDialogWithConversationUpdateWithoutUserProfile()
        {
            var arrange = ArrangeTest("ja-JP", false);

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
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_ShouldGoToMenuDialogWithConversationUpdateWithUserProfile(string language)
        {
            var arrange = ArrangeTest(language, true);

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
                .AssertReply(language == "ja-JP" ? $"ようこそ '{name}' さん！": $"Welcome {name}!")
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
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_ShouldWelcomeAndMenuDialogWithMessage(string language)
        {
            var arrange = ArrangeTest(language, true);

            // テストの追加と実行
            await arrange.testFlow
                .Test("foo", language == "ja-JP" ? $"ようこそ '{name}' さん！" : $"Welcome {name}!")
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
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_ShouldGoToPhotoUpdateDialog(string language)
        {
            var arrange = ArrangeTest(language, true);
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
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_GlobalCommand_ShouldCancelAllDialog(string language)
        {
            var arrange = ArrangeTest(language, true);

            // テストの追加と実行
            await arrange.testFlow
                .Test("foo", language == "ja-JP" ? $"ようこそ '{name}' さん！" : $"Welcome {name}!")
                .AssertReply((activity) =>
                {
                })
                .Send(language == "ja-JP" ? "天気を確認" : "Check weather")
                .AssertReply((activity) =>
                {
                })
                .Test(language == "ja-JP" ? "キャンセル" : "Cancel", language == "ja-JP" ? "キャンセルします" : "Cancel")
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
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_GlobalCommand_ShouldGoToProfileDialog(string language)
        {
            var arrange = ArrangeTest(language, true);

            // テストの追加と実行
            await arrange.testFlow
                .Test("foo", language == "ja-JP" ? $"ようこそ '{name}' さん！" : $"Welcome {name}!")
                .AssertReply((activity) =>
                {                   
                })
                .Send(language == "ja-JP" ? "天気を確認" : "Check weather")
                .AssertReply((activity) =>
                {
                })
                .Send(language == "ja-JP" ? "プロファイルの変更" : "Update profile")
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
