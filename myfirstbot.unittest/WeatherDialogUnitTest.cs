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
            // �X�g���[�W�Ƃ��ăC���������𗘗p
            IStorage dataStore = new MemoryStorage();
            // ���ꂼ��̃X�e�[�g���쐬
            var conversationState = new ConversationState(dataStore);
            var userState = new UserState(dataStore);
            var accessors = new MyStateAccessors(userState, conversationState)
            {
                // DialogState �� ConversationState �̃v���p�e�B�Ƃ��Đݒ�
                ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState"),
                // UserProfile ���쐬
                UserProfile = userState.CreateProperty<UserProfile>("UserProfile")
            };
            // �e�X�g�Ώۂ̃_�C�A���O���C���X�^���X��
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new WeatherDialog());

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // TestFlow �̍쐬
            return new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // �_�C�A���O�ɕK�v�ȃR�[�h�����ǉ�
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
        [DataRow("����")]
        [DataRow("�����")]
        public async Task WeatherDialog_ShouldReturnChoice(string date)
        {
            await ArrangeTestFlow()
            .Send("foo")
            .AssertReply((activity) =>
            {
                // �A�_�v�e�B�u�J�[�h���r
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/Weather.json").Replace("{0}", "����")).ToString()
                );
            })
            .Send("���̓��̓V�C")
            .AssertReply((activity) =>
            {
                // �A�_�v�e�B�u�J�[�h���r
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/WeatherDateChoice.json")).ToString()
                );
            })
            .Send(date)
            .AssertReply((activity) =>
            {
                // �A�_�v�e�B�u�J�[�h���r
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/Weather.json").Replace("{0}", date)).ToString()
                );
            })
            .Test("�I��", "Done")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task WeatherDialog_ShouldReturnChoiceAndComplete()
        {
            await ArrangeTestFlow()
            .Send("foo")
            .AssertReply((activity) =>
            {
                // �A�_�v�e�B�u�J�[�h���r
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/Weather.json").Replace("{0}", "����")).ToString()
                );
            })
            .Test("�I��", "Done")
            .StartTestAsync();
        }
    }
}