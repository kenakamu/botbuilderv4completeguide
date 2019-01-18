using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MyBotUnitTest
    {
        [TestMethod]
        public async Task MyBot_ShouldSaveProfile()
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
            // テスト用の変数
            var name = "Ken";
            var age = "42";

            // テストの追加と実行
            await new TestFlow(adapter, bot.OnTurnAsync)
                .Test("foo", "名前を入力してください。")
                .Test(name, "年齢を聞いてもいいですか？ (1) はい または (2) いいえ")
                .Test("はい", "年齢を入力してください。")
                .Test(age, $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{name} 年齢:{age} (1) はい または (2) いいえ")
                .Test("はい", "プロファイルを保存します。")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldNotSaveProfile()
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
            // テスト用の変数
            var name = "Ken";
            var age = "42";

            // テストの追加と実行
            await new TestFlow(adapter, bot.OnTurnAsync)
                .Test("foo", "名前を入力してください。")
                .Test(name, "年齢を聞いてもいいですか？ (1) はい または (2) いいえ")
                .Test("はい", "年齢を入力してください。")
                .Test(age, $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{name} 年齢:{age} (1) はい または (2) いいえ")
                .Test("いいえ", "プロファイルを破棄します。")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldSaveProfileWithoutAge()
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
            // テスト用の変数
            var name = "Ken";
            var age = 0;

            // テストの追加と実行
            await new TestFlow(adapter, bot.OnTurnAsync)
                .Test("foo", "名前を入力してください。")
                .Test(name, "年齢を聞いてもいいですか？ (1) はい または (2) いいえ")
                .Test("いいえ", $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{name} 年齢:{age} (1) はい または (2) いいえ")
                .Test("はい", "プロファイルを保存します。")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldSaveProfile()
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
            dialogs.Add(new ProfileDialog(accessors));

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // テスト用の変数
            var name = "Ken";
            var age = "42";

            // テストの追加と実行
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
                }
            })
            .Test("foo", "名前を入力してください。")
            .Test(name, "年齢を聞いてもいいですか？ (1) はい または (2) いいえ")
            .Test("はい", "年齢を入力してください。")
            .Test(age, $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{name} 年齢:{age} (1) はい または (2) いいえ")
            .Test("はい", "プロファイルを保存します。")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldNotSaveProfile()
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
            dialogs.Add(new ProfileDialog(accessors));

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // テスト用の変数
            var name = "Ken";
            var age = "42";

            // テストの追加と実行
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
                }
            })
            .Test("foo", "名前を入力してください。")
            .Test(name, "年齢を聞いてもいいですか？ (1) はい または (2) いいえ")
            .Test("はい", "年齢を入力してください。")
            .Test(age, $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{name} 年齢:{age} (1) はい または (2) いいえ")
            .Test("いいえ", "プロファイルを破棄します。")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldSaveProfileWithoutAge()
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
            dialogs.Add(new ProfileDialog(accessors));

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // テスト用の変数
            var name = "Ken";
            var age = 0;

            // テストの追加と実行
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
                }
            })
            .Test("foo", "名前を入力してください。")
            .Test(name, "年齢を聞いてもいいですか？ (1) はい または (2) いいえ")
            .Test("いいえ", $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{name} 年齢:{age} (1) はい または (2) いいえ")
            .Test("はい", "プロファイルを保存します。")
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
