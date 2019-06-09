using CognitiveServices.Translator;
using CognitiveServices.Translator.Translate;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{

    public class MockQnAMakerHandler : HttpMessageHandler
    {
        private class InternalQueryResult : QueryResult
        {
            [JsonProperty(PropertyName = "qnaId")]
            public int QnaId { get; set; }
        }

        private class InternalQueryResults
        {
            /// <summary>
            /// Gets or sets the answers for a user query,
            /// sorted in decreasing order of ranking score.
            /// </summary>
            [JsonProperty("answers")]
            public InternalQueryResult[] Answers { get; set; }
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var question = JObject.Parse(await request.Content.ReadAsStringAsync())["question"].ToString();
            var queryResults = new List<InternalQueryResult>();
            if (question == "質問")
            {
                queryResults.Add(new InternalQueryResult()
                {
                    Answer = "答え",
                    Score = 100,
                });
            }
            else
            {
                queryResults.Add(new InternalQueryResult()
                {
                    Answer = "答えなし",
                    Score = 1,
                });
            }
            var results = new InternalQueryResults()
            {
                Answers = queryResults.ToArray()
            };
            
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(results), Encoding.UTF8, "application/json")
            };

            return await Task.FromResult(responseMessage);
        }
    }

    [TestClass]
    public class QnADialogUnitTest
    {
        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs, StringLocalizer<QnADialog> localizer) ArrangeTest(string language)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            // リソースを利用するため StringLocalizer を作成
            var localizer = StringLocalizerFactory.GetStringLocalizer<QnADialog>();
                     
            // 翻訳サービスのモック化
            var mockTranslateClient = new Mock<ITranslateClient>();
            mockTranslateClient.Setup(l => l.TranslateAsync(It.IsAny<RequestContent>(), It.IsAny<RequestParameter>()))
                .Returns((RequestContent requestContent, RequestParameter requestParameter) =>
                {
                    var response = new List<ResponseBody>();
                    switch (requestContent.Text)
                    {
                        case "Question":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "質問" } } });
                            break;
                        case "答え":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "Answer" } } });
                            break;                        
                        default:
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "foo" } } });
                            break;
                    }
                    return Task.FromResult(response as IList<ResponseBody>);
                });

            // QnA サービスのモック化            
            var qnaEndpoint = new QnAMakerEndpoint()
            {
                KnowledgeBaseId = "dummyId",
                EndpointKey = "dummyKey",
                Host = "https://dummyhost.test/qna",
            };
            var qnaMaker = new QnAMaker(qnaEndpoint, httpClient:new HttpClient(new MockQnAMakerHandler()));
            // テスト対象のダイアログをインスタンス化
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new QnADialog(accessors, qnaMaker, mockTranslateClient.Object, localizer));

            // アダプターを作成し必要なミドルウェアを追加
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(accessors.UserState, accessors.ConversationState));

            // TestFlow の作成
            var testFlow = new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // ダイアログに必要なコードだけ追加
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(QnADialog), null, cancellationToken);
                }
            });

            return (testFlow, adapter, dialogs, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task QnADialog_ShouldReturnAnswer(string language)
        {
            // 言語を指定してテストを作成
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            // テストの追加と実行
            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
               
            })
            .Send(language == "ja-JP" ? "質問" : "Question")
            .AssertReply((activity) =>
            {
                Assert.AreEqual((activity as Activity).Text, language == "ja-JP" ? "答え" : "Answer");
            })
            .StartTestAsync();
        }
        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task QnADialog_ShouldReturnNoAnswer(string language)
        {
            // 言語を指定してテストを作成
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            // テストの追加と実行
            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {

            })
            .Send(language == "ja-JP" ? "Foo" : "Foo")
            .AssertReply((activity) =>
            {
                Assert.AreEqual((activity as Activity).Text,arrange.localizer["noanswer"]);
            })
            .StartTestAsync();
        }
    }
}
