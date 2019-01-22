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
    public class MenuDialogUnitTest
    {
        
        [TestMethod]
        public async Task MenuDialog_ShouldReturnWeatherResult()
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
            dialogs.Add(new MenuDialog());

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // テストの追加と実行
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
                }
            })
            .Test("foo", "今日はなにをしますか? (1) 天気を確認 または (2) 予定を確認")
            .Send("天気を確認")
            .AssertReply("今日の天気は晴れです")
            .AssertReply("今日はなにをしますか? (1) 天気を確認 または (2) 予定を確認")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task MenuDialog_ShouldReturnScheduleResult()
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
            dialogs.Add(new MenuDialog());

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // テストの追加と実行
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
                }
            })
            .Test("foo", "今日はなにをしますか? (1) 天気を確認 または (2) 予定を確認")
            .Send("予定を確認")
            .AssertReply("今日は予定はありません。")
            .AssertReply("今日はなにをしますか? (1) 天気を確認 または (2) 予定を確認")
            .StartTestAsync();
        }
    }
}
