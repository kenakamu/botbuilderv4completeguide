using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MyBotUnitTest
    {
        [TestMethod]
        public async Task MyBot_ShouldWelcomeAndProfileDialogWithConversationUpdate()
        {
            // アダプターを作成
            var adapter = new TestAdapter();
            adapter.Use(new SetLocaleMiddleware(Culture.Japanese));
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
            // テスト対象のクラスをインスタンス化
            var bot = new MyBot(accessors);
            var conversationUpdateActivity = new Activity(ActivityTypes.ConversationUpdate)
            {
                Id = "test",
                From = new ChannelAccount("TestUser", "Test User"),
                ChannelId = "UnitTest",
                ServiceUrl = "https://example.org",
                MembersAdded = new List<ChannelAccount>() { new ChannelAccount("TestUser", "Test User") }
            };

            // テストの追加と実行
            await new TestFlow(adapter, bot.OnTurnAsync)
                .Send(conversationUpdateActivity)
                .AssertReply("ようこそ MyBot へ！")
                .AssertReply("名前を入力してください。")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldWelcomeAndMenuDialogWithMessage()
        {
            var name = "Ken";
          
            // アダプターを作成
            var adapter = new TestAdapter();
            adapter.Use(new SetLocaleMiddleware(Culture.Japanese));

            // ストレージとしてモックのストレージを利用
            var mock = new Mock<IStorage>();
            // User1用に返すデータを作成
            // UserState のキーは <channelId>/users/<userId>
            var dictionary = new Dictionary<string, object>();
            dictionary.Add("test/users/user1", new Dictionary<string, object>()
            {
                { "UserProfile", new UserProfile() { Name = name, Age = 0 } }
            });
            // ストレージへの読み書きを設定
            mock.Setup(ms => ms.WriteAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);
            mock.Setup(ms => ms.ReadAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .Returns(()=>
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

            var bot = new MyBot(accessors);
           
            // テストの追加と実行
            await new TestFlow(adapter, bot.OnTurnAsync)
                .Test("foo", $"ようこそ '{name}' さん！")
                .AssertReply("今日はなにをしますか? (1) 天気を確認 または (2) 予定を確認")
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
