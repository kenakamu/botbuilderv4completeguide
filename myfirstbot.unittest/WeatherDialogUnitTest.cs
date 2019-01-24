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
                    await dialogContext.BeginDialogAsync(nameof(WeatherDialog), null, cancellationToken);
                }
            });
        }

        [TestMethod]
        [DataRow("今日")]
        [DataRow("明日")]
        [DataRow("明後日")]
        public async Task WeatherDialog_ShouldReturnWeatherForcast(string day)
        {
            await ArrangeTestFlow()
            .Test("foo", "いつの天気を知りたいですか？ (1) 今日、 (2) 明日、 または (3) 明後日")
            .Test(day, $"{day}の天気は晴れです")
            .StartTestAsync();
        }
    }
}
