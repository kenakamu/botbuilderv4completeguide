using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class WelcomeDialogUnitTest
    {
        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs) ArrangeTest()
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
            dialogs.Add(new WelcomeDialog(accessors));

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // TestFlow の作成
            var testFlow = new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(WelcomeDialog), null, cancellationToken);
                }
                // ダイアログが完了した場合は、UserProfile の名前をテスト側に返す
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    await turnContext.SendActivityAsync((await accessors.UserProfile.GetAsync(turnContext)).Name);
                }
            });

            return (testFlow, adapter, dialogs);
        }

        [TestMethod]
        public async Task WelcomeDialog_ShouldGoToProfileDialog()
        {
            var arrange = ArrangeTest();
            // テストの追加と実行
            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                // Activity にヒーローカードとアニメーションカードが含まれていることを確認。
                Assert.AreEqual((activity as Activity).Attachments.Count, 2);
                var heroCard = (activity as Activity).Attachments.First().Content as HeroCard;
                var animationCard = (activity as Activity).Attachments.Last().Content as AnimationCard;
                // ヒーローカードの内容を確認。
                Assert.AreEqual(heroCard.Title, "ようこそ My Bot へ！プロファイル登録をしますか？");
                Assert.AreEqual(heroCard.Buttons.Where(x=>x.Title == "はい").First().Value, "はい");
                Assert.AreEqual(heroCard.Buttons.Where(x=>x.Title == "スキップ").First().Value, "スキップ");
                Assert.AreEqual(heroCard.Buttons.Where(x=>x.Title == "Azure Bot Service").First().Value, "https://picsum.photos/300/200/?image=433");

                // アニメーションカードの内容を確認。
                Assert.AreEqual(animationCard.Title, "アニメーションサンプル");
                Assert.AreEqual(animationCard.Image.Url, "https://docs.microsoft.com/en-us/bot-framework/media/how-it-works/architecture-resize.png");
                Assert.AreEqual(animationCard.Media.Count, 1);
                Assert.AreEqual(animationCard.Media.First().Url, "http://i.giphy.com/Ki55RUbOV5njy.gif");
            })
            .Send("はい")
            .AssertReply((activity) =>
            {
                // Activity とアダプターからコンテキストを作成
                var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                // ダイアログコンテキストを取得
                var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                // 現在のダイアログスタックの一番上が ProfileDialog で その下が welcome であることを確認。
                var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(WelcomeDialog)).First().State["dialogs"] as DialogState).DialogStack;
                Assert.AreEqual(dialogInstances[0].Id, nameof(ProfileDialog));
                Assert.AreEqual(dialogInstances[1].Id, "welcome");
            })
            .StartTestAsync();
        }

        [TestMethod]

        public async Task WelcomeDialog_ShouldSetAnonymous()
        {
            var arrange = ArrangeTest();
            // テストの追加と実行
            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                // Activity にヒーローカードとアニメーションカードが含まれていることを確認。
                Assert.AreEqual((activity as Activity).Attachments.Count, 2);
                var heroCard = (activity as Activity).Attachments.First().Content as HeroCard;
                var animationCard = (activity as Activity).Attachments.Last().Content as AnimationCard;
                // ヒーローカードの内容を確認。
                Assert.AreEqual(heroCard.Title, "ようこそ My Bot へ！プロファイル登録をしますか？");
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == "はい").First().Value, "はい");
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == "スキップ").First().Value, "スキップ");
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == "Azure Bot Service").First().Value, "https://picsum.photos/300/200/?image=433");

                // アニメーションカードの内容を確認。
                Assert.AreEqual(animationCard.Title, "アニメーションサンプル");
                Assert.AreEqual(animationCard.Image.Url, "https://docs.microsoft.com/en-us/bot-framework/media/how-it-works/architecture-resize.png");
                Assert.AreEqual(animationCard.Media.Count, 1);
                Assert.AreEqual(animationCard.Media.First().Url, "http://i.giphy.com/Ki55RUbOV5njy.gif");
            })
            .Send("スキップ")
            .AssertReply((activity) =>
            {
                // 返ってきたテキストが匿名かを確認
                Assert.AreEqual((activity as Activity).Text, "匿名");
            })
            .StartTestAsync();
        }
    }
}
