using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class WelcomeDialogUnitTest
    {
        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs, StringLocalizer<WelcomeDialog> localizer) ArrangeTest(string language)
        {
            // �X�g���[�W�Ƃ��ăC���������𗘗p
            IStorage dataStore = new MemoryStorage();
            // ���ꂼ��̃X�e�[�g���쐬
            var mockStorage = new Mock<IStorage>();
            // User1�p�ɕԂ��f�[�^���쐬
            // UserState �̃L�[�� <channelId>/users/<userId>
            var dictionary = new Dictionary<string, object>();
            // ���[�U�[�v���t�@�C����ݒ�B
            dictionary.Add("test/users/user1", new Dictionary<string, object>()
                {
                    { "UserProfile", new UserProfile() { Name = "Ken", Age = 0, Language = language } }
                });
            // �X�g���[�W�ւ̓ǂݏ�����ݒ�
            mockStorage.Setup(ms => ms.WriteAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .Returns((Dictionary<string, object> dic, CancellationToken token) =>
                {
                    foreach (var dicItem in dic)
                    {
                        if (dicItem.Key != "test/users/user1")
                        {
                            if (dictionary.ContainsKey(dicItem.Key))
                            {
                                dictionary[dicItem.Key] = dicItem.Value;
                            }
                            else
                            {
                                dictionary.Add(dicItem.Key, dicItem.Value);
                            }
                        }
                    }

                    return Task.CompletedTask;
                });
            mockStorage.Setup(ms => ms.ReadAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    return Task.FromResult(result: (IDictionary<string, object>)dictionary);
                });

            // ���ꂼ��̃X�e�[�g���쐬
            var conversationState = new ConversationState(mockStorage.Object);
            var userState = new UserState(mockStorage.Object);
            var accessors = new MyStateAccessors(userState, conversationState)
            {
                // DialogState �� ConversationState �̃v���p�e�B�Ƃ��Đݒ�
                ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState"),
                // UserProfile ���쐬
                UserProfile = userState.CreateProperty<UserProfile>("UserProfile")
            };

            // ���\�[�X�𗘗p���邽�� StringLocalizer ���쐬
            ResourceManagerStringLocalizerFactory factory = new ResourceManagerStringLocalizerFactory(
                Options.Create(new LocalizationOptions() { ResourcesPath = "Resources" }), NullLoggerFactory.Instance);
            var localizer = new StringLocalizer<WelcomeDialog>(factory);

            // IServiceProvider �̃��b�N
            var serviceProvider = new Mock<IServiceProvider>();

            // WelcomeDialog �N���X�ŉ������ׂ��T�[�r�X��o�^
            serviceProvider.Setup(x => x.GetService(typeof(ProfileDialog))).Returns(new ProfileDialog(accessors));
            serviceProvider.Setup(x => x.GetService(typeof(SelectLanguageDialog))).Returns(new SelectLanguageDialog(accessors));
        
            // �e�X�g�Ώۂ̃_�C�A���O���C���X�^���X��
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new WelcomeDialog(accessors, localizer, serviceProvider.Object));

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new TestAdapter()
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

            return (testFlow, adapter, dialogs, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task WelcomeDialog_ShouldGoToProfileDialog(string language)
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
                // Activity �Ƀq�[���[�J�[�h���܂܂�Ă��邱�Ƃ��m�F�B
                Assert.AreEqual((activity as Activity).Attachments.Count, 1);
                var heroCard = (activity as Activity).Attachments.First().Content as HeroCard;
                // �q�[���[�J�[�h�̓��e���m�F�B
                Assert.AreEqual(heroCard.Title, arrange.localizer["title"]);
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["yes"]).First().Value, arrange.localizer["yes"].ToString());
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["skip"]).First().Value, arrange.localizer["skip"].ToString());
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["checkDetail"]).First().Value, "https://dev.botframework.com");
            })
            .Send(arrange.localizer["yes"])
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
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task WelcomeDialog_ShouldSetAnonymous(string language)
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
                // Activity �Ƀq�[���[�J�[�h���܂܂�Ă��邱�Ƃ��m�F�B
                Assert.AreEqual((activity as Activity).Attachments.Count, 1);
                var heroCard = (activity as Activity).Attachments.First().Content as HeroCard;
                // �q�[���[�J�[�h�̓��e���m�F�B
                Assert.AreEqual(heroCard.Title, arrange.localizer["title"]);
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["yes"]).First().Value, arrange.localizer["yes"].ToString());
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["skip"]).First().Value, arrange.localizer["skip"].ToString());
                Assert.AreEqual(heroCard.Buttons.Where(x => x.Title == arrange.localizer["checkDetail"]).First().Value, "https://dev.botframework.com");
            })
            .Send(arrange.localizer["skip"])
            .AssertReply((activity) =>
            {
                // �Ԃ��Ă����e�L�X�g�����������m�F
                Assert.AreEqual((activity as Activity).Text, arrange.localizer["anonymous"].ToString());
            })
            .StartTestAsync();
        }
    }
}
