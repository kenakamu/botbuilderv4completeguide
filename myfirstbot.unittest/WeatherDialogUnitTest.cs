using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using myfirstbot.unittest.Helpers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class WeatherDialogUnitTest
    {
        private (TestFlow testFlow, StringLocalizer<WeatherDialog> localizer) ArrangeTest(string language)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            // リソースを利用するため StringLocalizer を作成
            var localizer = StringLocalizerFactory.GetStringLocalizer<WeatherDialog>();

            // テスト対象のダイアログをインスタンス化
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new WeatherDialog(accessors, localizer));

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(accessors.UserState, accessors.ConversationState));
            
            // TestFlow の作成
            var testFlow = new TestFlow(adapter, async (turnContext, cancellationToken) =>
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

            return (testFlow, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP","明日")]
        [DataRow("ja-JP","明後日")]
        [DataRow("en-US","tomorrow")]
        [DataRow("en-US","day after tomorrow")]
        public async Task WeatherDialog_ShouldReturnChoice(string language, string date)
        {
            // 言語を指定してテストを作成
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                // アダプティブカードを比較
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText($"./AdaptiveJsons/{language}/Weather.json").Replace("{0}", arrange.localizer["today"])).ToString()
                );
            })
            .Send("他の日の天気")
            .AssertReply((activity) =>
            {
                // アダプティブカードを比較
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText($"./AdaptiveJsons/{language}/WeatherDateChoice.json")).ToString()
                );
            })
            .Send(date)
            .AssertReply((activity) =>
            {
                // アダプティブカードを比較
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText($"./AdaptiveJsons/{language}/Weather.json").Replace("{0}", date)).ToString()
                );
            })
            .Test(arrange.localizer["end"], "Done")
            .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task WeatherDialog_ShouldReturnChoiceAndComplete(string language)
        {
            // 言語を指定してテストを作成
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                // アダプティブカードを比較
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText($"./AdaptiveJsons/{language}/Weather.json").Replace("{0}", arrange.localizer["today"])).ToString()
                );
            })
            .Test(arrange.localizer["end"], "Done")
            .StartTestAsync();
        }
    }
}