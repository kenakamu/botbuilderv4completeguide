using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class SelectLanguageDialogUnitTest
    {
        private TestFlow ArrangeTest()
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
                    { "UserProfile", new UserProfile() { Name = "Ken", Age = 0} }
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

            // �e�X�g�Ώۂ̃_�C�A���O���C���X�^���X��
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new SelectLanguageDialog(accessors));

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
                    await dialogContext.BeginDialogAsync(nameof(SelectLanguageDialog), null, cancellationToken);
                }
                // �_�C�A���O�����������ꍇ�́AUserProfile �̌�����e�X�g���ɕԂ�
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    await turnContext.SendActivityAsync((await accessors.UserProfile.GetAsync(turnContext)).Language);
                }
            });
        } 

        [TestMethod]
        public async Task SelectLanguageDialog_ShouldSetJapanese()
        {
            // �e�X�g�̒ǉ��Ǝ��s
            await ArrangeTest()
            .Test("foo", "�����I�����Ă��������BSelect your language (1) ���{�� or (2) English")
            .Test("���{��","ja-JP")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task SelectLanguageDialog_ShouldSetEnglish()
        {
            // �e�X�g�̒ǉ��Ǝ��s
            await ArrangeTest()
            .Test("foo", "�����I�����Ă��������BSelect your language (1) ���{�� or (2) English")
            .Test("English", "en-US")
            .StartTestAsync();
        }
    }
}
