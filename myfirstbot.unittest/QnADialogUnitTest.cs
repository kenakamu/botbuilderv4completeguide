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
            if (question == "����")
            {
                queryResults.Add(new InternalQueryResult()
                {
                    Answer = "����",
                    Score = 100,
                });
            }
            else
            {
                queryResults.Add(new InternalQueryResult()
                {
                    Answer = "�����Ȃ�",
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

            // ���\�[�X�𗘗p���邽�� StringLocalizer ���쐬
            var localizer = StringLocalizerFactory.GetStringLocalizer<QnADialog>();
                     
            // �|��T�[�r�X�̃��b�N��
            var mockTranslateClient = new Mock<ITranslateClient>();
            mockTranslateClient.Setup(l => l.TranslateAsync(It.IsAny<RequestContent>(), It.IsAny<RequestParameter>()))
                .Returns((RequestContent requestContent, RequestParameter requestParameter) =>
                {
                    var response = new List<ResponseBody>();
                    switch (requestContent.Text)
                    {
                        case "Question":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "����" } } });
                            break;
                        case "����":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "Answer" } } });
                            break;                        
                        default:
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "foo" } } });
                            break;
                    }
                    return Task.FromResult(response as IList<ResponseBody>);
                });

            // QnA �T�[�r�X�̃��b�N��            
            var qnaEndpoint = new QnAMakerEndpoint()
            {
                KnowledgeBaseId = "dummyId",
                EndpointKey = "dummyKey",
                Host = "https://dummyhost.test/qna",
            };
            var qnaMaker = new QnAMaker(qnaEndpoint, httpClient:new HttpClient(new MockQnAMakerHandler()));
            // �e�X�g�Ώۂ̃_�C�A���O���C���X�^���X��
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new QnADialog(accessors, qnaMaker, mockTranslateClient.Object, localizer));

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(accessors.UserState, accessors.ConversationState));

            // TestFlow �̍쐬
            var testFlow = new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // �_�C�A���O�ɕK�v�ȃR�[�h�����ǉ�
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
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
               
            })
            .Send(language == "ja-JP" ? "����" : "Question")
            .AssertReply((activity) =>
            {
                Assert.AreEqual((activity as Activity).Text, language == "ja-JP" ? "����" : "Answer");
            })
            .StartTestAsync();
        }
        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task QnADialog_ShouldReturnNoAnswer(string language)
        {
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            // �e�X�g�̒ǉ��Ǝ��s
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
