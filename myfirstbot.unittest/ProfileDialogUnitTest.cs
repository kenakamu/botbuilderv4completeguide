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

        private TestFlow ArrangeTestFlow()
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

            // TestFlow の作成
            return new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
                }
            });
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldSaveProfile()
        {            
            // テスト用の変数
            var name = "Ken";
            var age = "42";

            await ArrangeTestFlow()
            .Test("foo", "名前を入力してください。")
            .Test(name, "年齢を聞いてもいいですか？ (1) はい または (2) いいえ")
            .Test("はい", "年齢を入力してください。")
            .Test(age, $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{name} 年齢:{age} (1) はい、 (2) 名前を変更する、 または (3) 年齢を変更する")
            .Test("はい", "プロファイルを保存します。")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldSaveProfileWithNewName()
        {            
            // テスト用の変数
            var name = "Ken";
            var newName = "Kenichiro";
            var age = "42";
            
            // テストの追加と実行
            await ArrangeTestFlow()
            .Test("foo", "名前を入力してください。")
            .Test(name, "年齢を聞いてもいいですか？ (1) はい または (2) いいえ")
            .Test("はい", "年齢を入力してください。")
            .Test(age, $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{name} 年齢:{age} (1) はい、 (2) 名前を変更する、 または (3) 年齢を変更する")
            .Test("名前を変更する", "名前を入力してください。")
            .Test(newName, $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{newName} 年齢:{age} (1) はい、 (2) 名前を変更する、 または (3) 年齢を変更する")
            .Test("はい", "プロファイルを保存します。")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldSaveProfileWithNewAge()
        {
            // テスト用の変数
            var name = "Ken";
            var age = "42";
            var newAge = "43";

            // テストの追加と実行
            await ArrangeTestFlow()
            .Test("foo", "名前を入力してください。")
            .Test(name, "年齢を聞いてもいいですか？ (1) はい または (2) いいえ")
            .Test("はい", "年齢を入力してください。")
            .Test(age, $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{name} 年齢:{age} (1) はい、 (2) 名前を変更する、 または (3) 年齢を変更する")
            .Test("年齢を変更する", "年齢を入力してください。")
            .Test(newAge, $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{name} 年齢:{newAge} (1) はい、 (2) 名前を変更する、 または (3) 年齢を変更する")
            .Test("はい", "プロファイルを保存します。")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldSaveProfileWithoutAge()
        {           
            // テスト用の変数
            var name = "Ken";
            var age = 0;

            // テストの追加と実行
            await ArrangeTestFlow()
            .Test("foo", "名前を入力してください。")
            .Test(name, "年齢を聞いてもいいですか？ (1) はい または (2) いいえ")
            .Test("いいえ", $"次の情報で登録します。いいですか？{Environment.NewLine} 名前:{name} 年齢:{age} (1) はい、 (2) 名前を変更する、 または (3) 年齢を変更する")
            .Test("はい", "プロファイルを保存します。")
            .StartTestAsync();
        }        
    }
}
