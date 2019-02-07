using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class PhotoUpdateDialogUnitTest
    {
        private string attachmentUrl = "https://github.com/apple-touch-icon.png";

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

            // Microsoft Graph �n�̃��b�N
            var mockGraphSDK = new Mock<IGraphServiceClient>();
            // �v���t�@�C���ʐ^�̑�������b�N
            mockGraphSDK.Setup(x => x.Me.Photo.Content.Request(null).PutAsync(It.IsAny<Stream>()))
                .Returns(Task.FromResult(default(Stream)));

            mockGraphSDK.Setup(x => x.Me.Photo.Content.Request(null).GetAsync())
                .Returns(async () =>
                {
                    return new MemoryStream();
                });

            var msGraphService = new MSGraphService(mockGraphSDK.Object);
            
            // �e�X�g�Ώۂ̃_�C�A���O���C���X�^���X��
            var loginDialog = new LoginDialog();
            // OAuthPrompt ���e�X�g�p�̃v�����v�g�ɍ����ւ�
            loginDialog.ReplaceDialog(new TestOAuthPrompt("login", new OAuthPromptSettings()));
            var photoUpdateDialog = new PhotoUpdateDialog(msGraphService);
            // ���O�C���_�C�A���O����L�ł��������̂ɍ����ւ�
            photoUpdateDialog.ReplaceDialog(loginDialog);
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(photoUpdateDialog);
            dialogs.Add(loginDialog);

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
                    await dialogContext.BeginDialogAsync(nameof(PhotoUpdateDialog), attachmentUrl, cancellationToken);
                }
            });
        }

        [TestMethod]
        public async Task PhotoUpdateDialogShouldUpdateAndReturnPicture()
        {            
            await ArrangeTestFlow()
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Attachments.Count == 1);
            })
            .StartTestAsync();
        }
    }
}
