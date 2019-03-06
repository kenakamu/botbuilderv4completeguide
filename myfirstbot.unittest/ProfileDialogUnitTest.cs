using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class ProfileDialogUnitTest
    {

        private (TestFlow testFlow, StringLocalizer<ProfileDialog> localizer) ArrangeTest(string language)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            // ���\�[�X�𗘗p���邽�� StringLocalizer ���쐬
            var localizer = StringLocalizerFactory.GetStringLocalizer<ProfileDialog>();

            // �e�X�g�Ώۂ̃_�C�A���O���C���X�^���X��
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new ProfileDialog(accessors, localizer));

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
                    await dialogContext.BeginDialogAsync(nameof(ProfileDialog), null, cancellationToken);
                }
            });

            return (testFlow, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task ProfileDialog_ShouldSaveProfile(string language)
        {
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                // �A�_�v�e�B�u�J�[�h���r
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText($"./AdaptiveJsons/{language}/Profile.json")).ToString()
                );
            })
            .Send(new Activity()
            {
                Value = new JObject
                {
                    {"name", "Ken" },
                    {"email" , "kenakamu@microsoft.com"},
                    {"phone" , "xxx-xxxx-xxxx"},
                    {"birthday" , new DateTime(1976, 7, 21)},
                    {"hasCat" , true},
                    {"catNum" , "3"},
                    {"catTypes", "�L�W�g��,�T�o�g��,�n�`����" },
                    {"playWithCat" , true}
                }.ToString()
            })
            .AssertReply((activity) =>
            {
                Assert.AreEqual((activity as Activity).Text, arrange.localizer["save"]);
            })
            .StartTestAsync();
        }
    }
}