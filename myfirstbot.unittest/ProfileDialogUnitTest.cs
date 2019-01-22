using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class ProfileDialogUnitTest
    {       

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
    }
}
