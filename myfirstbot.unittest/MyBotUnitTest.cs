using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MyBotUnitTest
    {
        [TestMethod]
        public async Task MyBot_ShouldAskName()
        {
            // アダプターを作成
            var adapter = new TestAdapter();
            // ストレージとしてインメモリを利用
            IStorage dataStore = new MemoryStorage();
            // それぞれのステートを作成
            var conversationState = new ConversationState(dataStore);
            var accessors = new MyStateAccessors(conversationState)
            {
                // DialogState を ConversationState のプロパティとして設定
                ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState"),
            };
            // テスト対象のクラスをインスタンス化
            var bot = new MyBot(accessors);
            // テストの追加と実行
            await new TestFlow(adapter, bot.OnTurnAsync)
                .Test("foo", "名前を入力してください")
                .Test("Ken", "ようこそ 'Ken' さん！")
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
