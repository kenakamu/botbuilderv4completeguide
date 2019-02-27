using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
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
            await ArrangeTestFlow()
            .Send("foo")
            .AssertReply((activity) =>
            {
                // アダプティブカードを比較
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/Profile.json")).ToString()
                );
            })
            .Send(new Activity()
            {
                Value = new JObject
                {
                    {"name", "Ken" },
                    {"email" , "kenakamu@microsoft.com"},
                    {"phone" , "xxx-xxxx-xxxx"},
                    {"birthday" , new DateTime(1976, 7, 21)},
                    {"hasCat" , true},
                    {"catNum" , "3"},
                    {"catTypes", "キジトラ,サバトラ,ハチワレ" },
                    {"playWithCat" , true}
                }.ToString()
            })
            .AssertReply((activity) =>
            {
                Assert.AreEqual(
                    (activity as Activity).Text,
                    "プロファイルを保存します。"
                );
            })
            .StartTestAsync();
        }


        [TestMethod]
        [DataRow(1800)]
        [DataRow(2020)]
        public async Task ProfileDialog_ShouldAskRetryWhenAgeOutOfRange(int year)
        {
            // テストの追加と実行
            await ArrangeTestFlow()
            .Send("foo")
            .AssertReply((activity) =>
            {
                // アダプティブカードを比較
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/Profile.json")).ToString()
                );
            })
            .Send(new Activity()
            {
                Value = new JObject
                {
                    {"name", "Ken" },
                    {"email" , "kenakamu@microsoft.com"},
                    {"phone" , "xxx-xxxx-xxxx"},
                    {"birthday" , new DateTime(year, 7, 21)},
                    {"hasCat" , true},
                    {"catNum" , "3"},
                    {"catTypes", "キジトラ,サバトラ,ハチワレ" },
                    {"playWithCat" , true}
                }.ToString()
            }).AssertReply((activity) =>
            {
                var birthday = new DateTime(year, 7, 21);
                var age = DateTime.Now.Year - birthday.Year;
                if (DateTime.Now < birthday.AddYears(age))
                    age--;

                Assert.AreEqual(
                    (activity as Activity).Text,
                    $"年齢が{age}歳になります。ただしい誕生日を入れてください。"
                );
            })
            .StartTestAsync();
        }
    }
}