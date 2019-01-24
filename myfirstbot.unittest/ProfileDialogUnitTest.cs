using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class ProfileDialogUnitTest
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
            dialogs.Add(new ProfileDialog(accessors));

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(userState, conversationState));

            // TestFlow �̍쐬
            return new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // �_�C�A���O�ɕK�v�ȃR�[�h�����ǉ�
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
            // �e�X�g�p�̕ϐ�
            var name = "Ken";
            var age = "42";

            await ArrangeTestFlow()
            .Test("foo", "���O����͂��Ă��������B")
            .Test(name, "�N��𕷂��Ă������ł����H (1) �͂� �܂��� (2) ������")
            .Test("�͂�", "�N�����͂��Ă��������B")
            .Test(age, $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{name} �N��:{age} (1) �͂��A (2) ���O��ύX����A �܂��� (3) �N���ύX����")
            .Test("�͂�", "�v���t�@�C����ۑ����܂��B")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldSaveProfileWithNewName()
        {            
            // �e�X�g�p�̕ϐ�
            var name = "Ken";
            var newName = "Kenichiro";
            var age = "42";
            
            // �e�X�g�̒ǉ��Ǝ��s
            await ArrangeTestFlow()
            .Test("foo", "���O����͂��Ă��������B")
            .Test(name, "�N��𕷂��Ă������ł����H (1) �͂� �܂��� (2) ������")
            .Test("�͂�", "�N�����͂��Ă��������B")
            .Test(age, $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{name} �N��:{age} (1) �͂��A (2) ���O��ύX����A �܂��� (3) �N���ύX����")
            .Test("���O��ύX����", "���O����͂��Ă��������B")
            .Test(newName, $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{newName} �N��:{age} (1) �͂��A (2) ���O��ύX����A �܂��� (3) �N���ύX����")
            .Test("�͂�", "�v���t�@�C����ۑ����܂��B")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldSaveProfileWithNewAge()
        {
            // �e�X�g�p�̕ϐ�
            var name = "Ken";
            var age = "42";
            var newAge = "43";

            // �e�X�g�̒ǉ��Ǝ��s
            await ArrangeTestFlow()
            .Test("foo", "���O����͂��Ă��������B")
            .Test(name, "�N��𕷂��Ă������ł����H (1) �͂� �܂��� (2) ������")
            .Test("�͂�", "�N�����͂��Ă��������B")
            .Test(age, $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{name} �N��:{age} (1) �͂��A (2) ���O��ύX����A �܂��� (3) �N���ύX����")
            .Test("�N���ύX����", "�N�����͂��Ă��������B")
            .Test(newAge, $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{name} �N��:{newAge} (1) �͂��A (2) ���O��ύX����A �܂��� (3) �N���ύX����")
            .Test("�͂�", "�v���t�@�C����ۑ����܂��B")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ProfileDialog_ShouldSaveProfileWithoutAge()
        {           
            // �e�X�g�p�̕ϐ�
            var name = "Ken";
            var age = 0;

            // �e�X�g�̒ǉ��Ǝ��s
            await ArrangeTestFlow()
            .Test("foo", "���O����͂��Ă��������B")
            .Test(name, "�N��𕷂��Ă������ł����H (1) �͂� �܂��� (2) ������")
            .Test("������", $"���̏��œo�^���܂��B�����ł����H{Environment.NewLine} ���O:{name} �N��:{age} (1) �͂��A (2) ���O��ύX����A �܂��� (3) �N���ύX����")
            .Test("�͂�", "�v���t�@�C����ۑ����܂��B")
            .StartTestAsync();
        }        
    }
}
