using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MyBotUnitTest
    {
        [TestMethod]
        public async Task MyBot_ShouldSaveProfile()
        {
            // �A�_�v�^�[���쐬
            var adapter = new TestAdapter();
            adapter.Use(new SetLocaleMiddleware(Culture.Japanese));
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
            // �e�X�g�Ώۂ̃N���X���C���X�^���X��
            var bot = new MyBot(accessors);
            // �e�X�g�p�̕ϐ�
            var name = "Ken";
            var age = "42";

            // �e�X�g�̒ǉ��Ǝ��s
            await new TestFlow(adapter, bot.OnTurnAsync)
                .Test("foo", "���O����͂��Ă��������B")
                .Test(name, "�N��𕷂��Ă������ł����H (1) �͂� �܂��� (2) ������")
                .Test("�͂�", "�N�����͂��Ă��������B")
                .Test(age, $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{name} �N��:{age} (1) �͂� �܂��� (2) ������")
                .Test("�͂�", "�v���t�@�C����ۑ����܂��B")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldNotSaveProfile()
        {
            // �A�_�v�^�[���쐬
            var adapter = new TestAdapter();
            adapter.Use(new SetLocaleMiddleware(Culture.Japanese));
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
            // �e�X�g�Ώۂ̃N���X���C���X�^���X��
            var bot = new MyBot(accessors);
            // �e�X�g�p�̕ϐ�
            var name = "Ken";
            var age = "42";

            // �e�X�g�̒ǉ��Ǝ��s
            await new TestFlow(adapter, bot.OnTurnAsync)
                .Test("foo", "���O����͂��Ă��������B")
                .Test(name, "�N��𕷂��Ă������ł����H (1) �͂� �܂��� (2) ������")
                .Test("�͂�", "�N�����͂��Ă��������B")
                .Test(age, $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{name} �N��:{age} (1) �͂� �܂��� (2) ������")
                .Test("������", "�v���t�@�C����j�����܂��B")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldSaveProfileWithoutAge()
        {
            // �A�_�v�^�[���쐬
            var adapter = new TestAdapter();
            adapter.Use(new SetLocaleMiddleware(Culture.Japanese));
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
            // �e�X�g�Ώۂ̃N���X���C���X�^���X��
            var bot = new MyBot(accessors);
            // �e�X�g�p�̕ϐ�
            var name = "Ken";
            var age = 0;

            // �e�X�g�̒ǉ��Ǝ��s
            await new TestFlow(adapter, bot.OnTurnAsync)
                .Test("foo", "���O����͂��Ă��������B")
                .Test(name, "�N��𕷂��Ă������ł����H (1) �͂� �܂��� (2) ������")
                .Test("������", $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{name} �N��:{age} (1) �͂� �܂��� (2) ������")
                .Test("�͂�", "�v���t�@�C����ۑ����܂��B")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldSaveProfile()
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
            dialogs.Add(new ProfileDialog(accessors));

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // �e�X�g�p�̕ϐ�
            var name = "Ken";
            var age = "42";

            // �e�X�g�̒ǉ��Ǝ��s
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // �_�C�A���O�ɕK�v�ȃR�[�h�����ǉ�
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
                }
            })
            .Test("foo", "���O����͂��Ă��������B")
            .Test(name, "�N��𕷂��Ă������ł����H (1) �͂� �܂��� (2) ������")
            .Test("�͂�", "�N�����͂��Ă��������B")
            .Test(age, $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{name} �N��:{age} (1) �͂� �܂��� (2) ������")
            .Test("�͂�", "�v���t�@�C����ۑ����܂��B")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldNotSaveProfile()
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
            dialogs.Add(new ProfileDialog(accessors));

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // �e�X�g�p�̕ϐ�
            var name = "Ken";
            var age = "42";

            // �e�X�g�̒ǉ��Ǝ��s
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // �_�C�A���O�ɕK�v�ȃR�[�h�����ǉ�
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
                }
            })
            .Test("foo", "���O����͂��Ă��������B")
            .Test(name, "�N��𕷂��Ă������ł����H (1) �͂� �܂��� (2) ������")
            .Test("�͂�", "�N�����͂��Ă��������B")
            .Test(age, $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{name} �N��:{age} (1) �͂� �܂��� (2) ������")
            .Test("������", "�v���t�@�C����j�����܂��B")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldSaveProfileWithoutAge()
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
            dialogs.Add(new ProfileDialog(accessors));

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // �e�X�g�p�̕ϐ�
            var name = "Ken";
            var age = 0;

            // �e�X�g�̒ǉ��Ǝ��s
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // �_�C�A���O�ɕK�v�ȃR�[�h�����ǉ�
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
                }
            })
            .Test("foo", "���O����͂��Ă��������B")
            .Test(name, "�N��𕷂��Ă������ł����H (1) �͂� �܂��� (2) ������")
            .Test("������", $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{name} �N��:{age} (1) �͂� �܂��� (2) ������")
            .Test("�͂�", "�v���t�@�C����ۑ����܂��B")
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
