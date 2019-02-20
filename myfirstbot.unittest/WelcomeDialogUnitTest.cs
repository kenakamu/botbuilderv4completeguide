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
            dialogs.Add(new WelcomeDialog(accessors));

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // TestFlow �̍쐬
            var testFlow = new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // �_�C�A���O�ɕK�v�ȃR�[�h�����ǉ�
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(WelcomeDialog), null, cancellationToken);
                }
                // �_�C�A���O�����������ꍇ�́AUserProfile �̖��O���e�X�g���ɕԂ�
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
            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                // Activity �Ƀq�[���[�J�[�h�ƃA�j���[�V�����J�[�h���܂܂�Ă��邱�Ƃ��m�F�B
                Assert.AreEqual((activity as Activity).Attachments.Count, 2);
                var heroCard = (activity as Activity).Attachments.First().Content as HeroCard;
                var animationCard = (activity as Activity).Attachments.Last().Content as AnimationCard;
                // �q�[���[�J�[�h�̓��e���m�F�B
                Assert.AreEqual(heroCard.Title, "�悤���� My Bot �ցI�v���t�@�C���o�^�����܂����H");
                Assert.AreEqual(heroCard.Buttons.Where(x=>x.Title == "�͂�").First().Value, "�͂�");
                Assert.AreEqual(heroCard.Buttons.Where(x=>x.Title == "�X�L�b�v").First().Value, "�X�L�b�v");
                Assert.AreEqual(heroCard.Buttons.Where(x=>x.Title == "Azure Bot Service").First().Value, "https://picsum.photos/300/200/?image=433");

                // �A�j���[�V�����J�[�h�̓��e���m�F�B
                Assert.AreEqual(animationCard.Title, "�A�j���[�V�����T���v��");
                Assert.AreEqual(animationCard.Image.Url, "https://docs.microsoft.com/en-us/bot-framework/media/how-it-works/architecture-resize.png");
                Assert.AreEqual(animationCard.Media.Count, 1);
                Assert.AreEqual(animationCard.Media.First().Url, "http://i.giphy.com/Ki55RUbOV5njy.gif");
            })
            .Send("�͂�")
            .AssertReply((activity) =>
            {
                // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                // �_�C�A���O�R���e�L�X�g���擾
                var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ ProfileDialog �� ���̉��� welcome �ł��邱�Ƃ��m�F�B
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
            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                // Activity �Ƀq�[���[�J�[�h�ƃA�j���[�V�����J�[�h���܂܂�Ă��邱�Ƃ��m�F�B
                Assert.AreEqual((activity as Activity).Attachments.Count, 2);
                var heroCard = (activity as Activity).Attachments.First().Content as HeroCard;
                var animationCard = (activity as Activity).Attachments.Last().Content as AnimationCard;
                // �q�[���[�J�[�h�̓��e���m�F�B
                Assert.AreEqual(heroCard.Title, "�悤���� My Bot �ցI�v���t�@�C���o�^�����܂����H");
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == "�͂�").First().Value, "�͂�");
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == "�X�L�b�v").First().Value, "�X�L�b�v");
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == "Azure Bot Service").First().Value, "https://picsum.photos/300/200/?image=433");

                // �A�j���[�V�����J�[�h�̓��e���m�F�B
                Assert.AreEqual(animationCard.Title, "�A�j���[�V�����T���v��");
                Assert.AreEqual(animationCard.Image.Url, "https://docs.microsoft.com/en-us/bot-framework/media/how-it-works/architecture-resize.png");
                Assert.AreEqual(animationCard.Media.Count, 1);
                Assert.AreEqual(animationCard.Media.First().Url, "http://i.giphy.com/Ki55RUbOV5njy.gif");
            })
            .Send("�X�L�b�v")
            .AssertReply((activity) =>
            {
                // �Ԃ��Ă����e�L�X�g�����������m�F
                Assert.AreEqual((activity as Activity).Text, "����");
            })
            .StartTestAsync();
        }
    }
}
