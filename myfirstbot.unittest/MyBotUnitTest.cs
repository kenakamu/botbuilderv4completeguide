using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MyBotUnitTest
    {
        // �e�X�g�p�ϐ�
        string name = "Ken";

        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs) ArrangeTest(bool returnUserProfile)
        {
            // �A�_�v�^�[���쐬
            var adapter = new TestAdapter();
            adapter.Use(new SetLocaleMiddleware(Culture.Japanese));
            // �X�g���[�W�Ƃ��ă��b�N�̃X�g���[�W�𗘗p
            var mock = new Mock<IStorage>();
            // User1�p�ɕԂ��f�[�^���쐬
            // UserState �̃L�[�� <channelId>/users/<userId>
            var dictionary = new Dictionary<string, object>();
            if (returnUserProfile)
            {
                dictionary.Add("test/users/user1", new Dictionary<string, object>()
                {
                    { "UserProfile", new UserProfile() { Name = name, Age = 0 } }
                });
            }
            // �X�g���[�W�ւ̓ǂݏ�����ݒ�
            mock.Setup(ms => ms.WriteAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
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
            mock.Setup(ms => ms.ReadAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    return Task.FromResult(result: (IDictionary<string, object>)dictionary);
                });

            // ���ꂼ��̃X�e�[�g���쐬
            var conversationState = new ConversationState(mock.Object);
            var userState = new UserState(mock.Object);
            var accessors = new MyStateAccessors(userState, conversationState)
            {
                // DialogState �� ConversationState �̃v���p�e�B�Ƃ��Đݒ�
                ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState"),
                // UserProfile ���쐬
                UserProfile = userState.CreateProperty<UserProfile>("UserProfile")
            };

            // �e�X�g�Ώۂ̃_�C�A���O���C���X�^���X��
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new ProfileDialog(accessors));
            dialogs.Add(new MenuDialog());

            // �e�X�g�Ώۂ̃N���X���C���X�^���X��
            var bot = new MyBot(accessors);

            // TestFlow �̍쐬
            var testFlow = new TestFlow(adapter, bot.OnTurnAsync);
            return (testFlow, adapter, dialogs);
        }

        [TestMethod]
        public async Task MyBot_ShouldGoToProfileDialogWithConversationUpdateWithoutUserProfile()
        {
            var arrange = ArrangeTest(false);

            var conversationUpdateActivity = new Activity(ActivityTypes.ConversationUpdate)
            {
                Id = "test",
                From = new ChannelAccount("TestUser", "Test User"),
                ChannelId = "UnitTest",
                ServiceUrl = "https://example.org",
                MembersAdded = new List<ChannelAccount>() { new ChannelAccount("TestUser", "Test User") }
            };

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Send(conversationUpdateActivity)
                .AssertReply("�悤���� MyBot �ցI")
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ ProfileDialog �� name �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(ProfileDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "name");
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldGoToMenuDialogWithConversationUpdateWithUserProfile()
        {
            var arrange = ArrangeTest(true);

            var conversationUpdateActivity = new Activity(ActivityTypes.ConversationUpdate)
            {
                Id = "test",
                From = new ChannelAccount("TestUser", "Test User"),
                ChannelId = "UnitTest",
                ServiceUrl = "https://example.org",
                MembersAdded = new List<ChannelAccount>() { new ChannelAccount("TestUser", "Test User") }
            };

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Send(conversationUpdateActivity)
                .AssertReply($"�悤���� '{name}' ����I")
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ MenuDialog �� choice �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldWelcomeAndMenuDialogWithMessage()
        {
            var arrange = ArrangeTest(true);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Test("foo", $"�悤���� '{name}' ����I")
                .AssertReply((activity) =>
                {
            // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
            var turnContext = new TurnContext(arrange.adapter, activity as Activity);
            // �_�C�A���O�R���e�L�X�g���擾
            var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ MenuDialog �� choice �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_GlobalCommand_ShouldCancelAllDialog()
        {
            var arrange = ArrangeTest(true);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Test("foo", $"�悤���� '{name}' ����I")
                .AssertReply("�����͂Ȃɂ����܂���? (1) �V�C���m�F �܂��� (2) �\����m�F")
                .Send("�V�C���m�F")
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ WeatherDialog �� ���̉��� MenuDialog �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, nameof(WeatherDialog));
                })
                .Test("�L�����Z��", "�L�����Z�����܂�")
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ MenuDialog �� choice �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_GlobalCommand_ShouldGoToProfileDialog()
        {
            var arrange = ArrangeTest(true);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Test("foo", $"�悤���� '{name}' ����I")
                .AssertReply("�����͂Ȃɂ����܂���? (1) �V�C���m�F �܂��� (2) �\����m�F")
                .Send("�V�C���m�F")
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ WeatherDialog �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, nameof(WeatherDialog));
                })
                .Send("�v���t�@�C���̕ύX")
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ ProfileDialog �ł��̉��� MenuDialog �ł��邱�Ƃ��m�F�B
                    // WeatherDialog �� MenuDialog �̍ŏ㕔�ɂ���
                    Assert.AreEqual(dc.Stack[0].Id, nameof(ProfileDialog));
                    Assert.AreEqual(dc.Stack[1].Id, nameof(MenuDialog));

                    // ProfileDialog �_�C�A���O�X�^�b�N�̈�ԏオ ProfileDialog �� name �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(ProfileDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "name");
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyMiddleware_ShouldStopProcessingWithAttachment()
        {            
            // �A�_�v�^�[���쐬���A���p����~�h���E�F�A��ǉ��B
            var adapter = new TestAdapter()
                .Use(new MyMiddleware());

            // �Y�t�t�@�C���𑗂�
            var activityWithAttachment = new Activity(ActivityTypes.Message)
            {
                Attachments = new List<Attachment>() { new Attachment() }
            };

            // �e�X�g�̎��s
            await new TestFlow(adapter)
            .Send(activityWithAttachment)
            .AssertReply("�e�L�X�g�𑗂��Ă�������")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task MyMiddleware_ShouldProcessingWithoutAttachment()
        {
            var nextMiddlewareCalled = false;

            // �o�^�����~�h���E�F�����ׂČĂ΂ꂽ��ɌĂяo�����R�[���o�b�N
            Task ValidateMiddleware(ITurnContext turnContext, CancellationToken cancellationToken)
            {
                // ����� turnContext �̒��g�����؂���K�v�͂Ȃ����߁A
                // ���̃~�h���E�F�A���Ăяo���ꂽ���Ǝ��̂Ō��؂𐬌��Ƃ���B
                nextMiddlewareCalled = true;
                return Task.CompletedTask;
            }
            // MiddlewareSet �Ƀe�X�g�Ώۂ̃~�h���E�F�A��ǉ��B
            var middlewareSet = new MiddlewareSet();
            middlewareSet.Use(new MyMiddleware());

            // �e�L�X�g���b�Z�[�W�� ITurnContext ���쐬�B
            var activityWithoutAttachment = new Activity(ActivityTypes.Message)
            {
                Text = "foo"
            };
            var ctx = new TurnContext(new TestAdapter(), activityWithoutAttachment);

            // MiddlewareSet �Ƀ��b�Z�[�W�𑗐M�B
            await middlewareSet.ReceiveActivityWithStatusAsync(ctx, ValidateMiddleware, default(CancellationToken));

            Assert.IsTrue(nextMiddlewareCalled);
        }
    }
}
