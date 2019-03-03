using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class SelectLanguageDialogUnitTest
    {
        private TestFlow ArrangeTest()
        {
            // ストレージとしてインメモリを利用
            IStorage dataStore = new MemoryStorage();
            // それぞれのステートを作成
            var mockStorage = new Mock<IStorage>();
            // User1用に返すデータを作成
            // UserState のキーは <channelId>/users/<userId>
            var dictionary = new Dictionary<string, object>();
            // ユーザープロファイルを設定。
            dictionary.Add("test/users/user1", new Dictionary<string, object>()
                {
                    { "UserProfile", new UserProfile() { Name = "Ken", Age = 0} }
                });
            // ストレージへの読み書きを設定
            mockStorage.Setup(ms => ms.WriteAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
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
            mockStorage.Setup(ms => ms.ReadAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    return Task.FromResult(result: (IDictionary<string, object>)dictionary);
                });

            // それぞれのステートを作成
            var conversationState = new ConversationState(mockStorage.Object);
            var userState = new UserState(mockStorage.Object);
            var accessors = new MyStateAccessors(userState, conversationState)
            {
                // DialogState を ConversationState のプロパティとして設定
                ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState"),
                // UserProfile を作成
                UserProfile = userState.CreateProperty<UserProfile>("UserProfile")
            };

            // テスト対象のダイアログをインスタンス化
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new SelectLanguageDialog(accessors));

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // TestFlow の作成
            return new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(SelectLanguageDialog), null, cancellationToken);
                }
                // ダイアログが完了した場合は、UserProfile の言語をテスト側に返す
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    await turnContext.SendActivityAsync((await accessors.UserProfile.GetAsync(turnContext)).Language);
                }
            });
        } 

        [TestMethod]
        public async Task SelectLanguageDialog_ShouldSetJapanese()
        {
            // テストの追加と実行
            await ArrangeTest()
            .Test("foo", "言語を選択してください。Select your language (1) 日本語 or (2) English")
            .Test("日本語","ja-JP")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task SelectLanguageDialog_ShouldSetEnglish()
        {
            // テストの追加と実行
            await ArrangeTest()
            .Test("foo", "言語を選択してください。Select your language (1) 日本語 or (2) English")
            .Test("English", "en-US")
            .StartTestAsync();
        }
    }
}
