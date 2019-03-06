using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class PhotoUpdateDialogUnitTest
    {
        private string attachmentUrl = "https://github.com/apple-touch-icon.png";

        private (TestFlow testFlow, StringLocalizer<PhotoUpdateDialog> localizer) ArrangeTest(string language)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            // ���\�[�X�𗘗p���邽�� StringLocalizer ���쐬
            var localizer = StringLocalizerFactory.GetStringLocalizer<PhotoUpdateDialog>();

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

            // IServiceProvider �̃��b�N
            var serviceProvider = new Mock<IServiceProvider>();

            // PhotoUpdateDialog �N���X�ŉ������ׂ��T�[�r�X��o�^
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(MSGraphService))).Returns(new MSGraphService(mockGraphSDK.Object));

            // �e�X�g�Ώۂ̃_�C�A���O���C���X�^���X��
            var loginDialog = new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>());
            // OAuthPrompt ���e�X�g�p�̃v�����v�g�ɍ����ւ�
            loginDialog.ReplaceDialog(new TestOAuthPrompt("login", new OAuthPromptSettings()));
            var photoUpdateDialog = new PhotoUpdateDialog(serviceProvider.Object, localizer);
            // ���O�C���_�C�A���O����L�ł��������̂ɍ����ւ�
            photoUpdateDialog.ReplaceDialog(loginDialog);
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(photoUpdateDialog);
            dialogs.Add(loginDialog);

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(accessors.UserState, accessors.ConversationState));

            // TestFlow �̍쐬
            var testFlow = new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // �_�C�A���O�ɕK�v�ȃR�[�h�����ǉ�
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(PhotoUpdateDialog), attachmentUrl, cancellationToken);
                }
            });

            return (testFlow, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task PhotoUpdateDialogShouldUpdateAndReturnPicture(string language)
        {
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Attachments.Count == 1);
            })
            .StartTestAsync();
        }
    }
}