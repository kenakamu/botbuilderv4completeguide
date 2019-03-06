using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Localization;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class ScheduleDialogUnitTest
    {
        // �_�~�[�̗\��p�̎���
        DateTime datetime = DateTime.Now;

        private (TestFlow testFlow, StringLocalizer<ScheduleDialog> localizer) ArrangeTest(string language)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            // ���\�[�X�𗘗p���邽�� StringLocalizer ���쐬
            var localizer = StringLocalizerFactory.GetStringLocalizer<ScheduleDialog>();

            // Microsoft Graph �n�̃��b�N
            var mockGraphSDK = new Mock<IGraphServiceClient>();
            // �_�~�[�̗\���Ԃ��B
            mockGraphSDK.Setup(x => x.Me.CalendarView.Request(It.IsAny<List<QueryOption>>()).GetAsync())
                .ReturnsAsync(() =>
                {
                    var page = new UserCalendarViewCollectionPage();
                    page.Add(new Event()
                    {
                        Subject = "Dummy 1",
                        Start = new DateTimeTimeZone() { DateTime = datetime.ToString() },
                        End = new DateTimeTimeZone() { DateTime = datetime.AddMinutes(30).ToString() }
                    });
                    page.Add(new Event()
                    {
                        Subject = "Dummy 2",
                        Start = new DateTimeTimeZone() { DateTime = datetime.AddMinutes(60).ToString() },
                        End = new DateTimeTimeZone() { DateTime = datetime.AddMinutes(90).ToString() }
                    });
                    return page;
                });

            // IServiceProvider �̃��b�N
            var serviceProvider = new Mock<IServiceProvider>();

            // ScheduleDialog �N���X�ŉ������ׂ��T�[�r�X��o�^
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(MSGraphService))).Returns(new MSGraphService(mockGraphSDK.Object));

            // �e�X�g�Ώۂ̃_�C�A���O���C���X�^���X��
            var loginDialog = new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>());
            // OAuthPrompt ���e�X�g�p�̃v�����v�g�ɍ����ւ�
            loginDialog.ReplaceDialog(new TestOAuthPrompt("login", new OAuthPromptSettings()));
            var scheduleDialog = new ScheduleDialog(serviceProvider.Object, localizer);
            // ���O�C���_�C�A���O����L�ł��������̂ɍ����ւ�
            scheduleDialog.ReplaceDialog(loginDialog);
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(scheduleDialog);
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
                    await dialogContext.BeginDialogAsync(nameof(ScheduleDialog), null, cancellationToken);
                }
            });

            return (testFlow, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task ScheduleDialog_ShouldReturnEvents(string language)
        {
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply($"{datetime.ToString("HH:mm")}-{datetime.AddMinutes(30).ToString("HH:mm")} : Dummy 1")
            .AssertReply($"{datetime.AddMinutes(60).ToString("HH:mm")}-{datetime.AddMinutes(90).ToString("HH:mm")} : Dummy 2")
            .StartTestAsync();
        }
    }
}