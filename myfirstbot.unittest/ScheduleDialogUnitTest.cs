using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using myfirstbot.unittest.Helpers;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class ScheduleDialogUnitTest
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
            var scheduleDialog = new ScheduleDialog();
            // �e�X�g�p�̃v�����v�g�ɍ����ւ�
            scheduleDialog.ReplaceDialog(new TestOAuthPrompt("login", new OAuthPromptSettings()));
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(scheduleDialog);

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
                    await dialogContext.BeginDialogAsync(nameof(ScheduleDialog), null, cancellationToken);
                }
            });
        }

        [TestMethod]
        public async Task ScheduleDialog_ShouldReturnToken()
        {
            await ArrangeTestFlow()
            .Test("foo", "Token: dummyToken")
            .StartTestAsync();
        }
    }
}
