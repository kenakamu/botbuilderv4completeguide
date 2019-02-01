using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using myfirstbot.unittest.Helpers;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class ScheduleDialogUnitTest
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
            var scheduleDialog = new ScheduleDialog();
            // テスト用のプロンプトに差し替え
            scheduleDialog.ReplaceDialog(new TestOAuthPrompt("login", new OAuthPromptSettings()));
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(scheduleDialog);

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
                    await dialogContext.BeginDialogAsync(nameof(ScheduleDialog), null, cancellationToken);
                }
            });
        }

        [TestMethod]
        public async Task ScheduleDialog_ShouldReturnToken()
        {
            await ArrangeTestFlow()
            .Test("foo", "Token: dummyToken")
            .StartTestAsync();
        }
    }
}
