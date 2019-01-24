using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MenuDialogUnitTest
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
            dialogs.Add(new MenuDialog());

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
                    await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
                }
            });

            return (testFlow, adapter, dialogs);
        }

        [TestMethod]
        public async Task MenuDialog_ShouldGoToWeatherDialog()
        {
            var arrange = ArrangeTest();
            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
            .Test("foo", "�����͂Ȃɂ����܂���? (1) �V�C���m�F �܂��� (2) �\����m�F")
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
            .StartTestAsync();
        }

        [TestMethod]
        public async Task MenuDialog_ShouldGoToScheduleDialog()
        {
            var arrange = ArrangeTest();
            await arrange.testFlow
            .Test("foo", "�����͂Ȃɂ����܂���? (1) �V�C���m�F �܂��� (2) �\����m�F")
            .Send("�\����m�F")
            .AssertReply((activity) =>
            {
                // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                // �_�C�A���O�R���e�L�X�g���擾
                var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ ScheduleDialog�@�� choice �ł��邱�Ƃ��m�F�B
                var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                Assert.AreEqual(dialogInstances[0].Id, "choice");
            })
            .StartTestAsync();            
        }
    }
}
