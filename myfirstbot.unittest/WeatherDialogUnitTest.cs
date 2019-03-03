using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class WeatherDialogUnitTest
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
            dialogs.Add(new WeatherDialog());

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
                    await dialogContext.BeginDialogAsync(nameof(WeatherDialog), null, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    await turnContext.SendActivityAsync("Done");
                }
            });

        }

        [TestMethod]
        [DataRow("明日")]
        [DataRow("明後日")]
        public async Task WeatherDialog_ShouldReturnChoice(string date)
        {
            await ArrangeTestFlow()
            .Send("foo")
            .AssertReply((activity) =>
            {
                // アダプティブカードを比較
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/Weather.json").Replace("{0}", "今日")).ToString()
                );
            })
            .Send("他の日の天気")
            .AssertReply((activity) =>
            {
                // アダプティブカードを比較
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/WeatherDateChoice.json")).ToString()
                );
            })
            .Send(date)
            .AssertReply((activity) =>
            {
                // アダプティブカードを比較
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/Weather.json").Replace("{0}", date)).ToString()
                );
            })
            .Test("終了", "Done")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task WeatherDialog_ShouldReturnChoiceAndComplete()
        {
            await ArrangeTestFlow()
            .Send("foo")
            .AssertReply((activity) =>
            {
                // アダプティブカードを比較
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/Weather.json").Replace("{0}", "今日")).ToString()
                );
            })
            .Test("終了", "Done")
            .StartTestAsync();
        }
    }
}