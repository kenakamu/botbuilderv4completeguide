using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
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
            await ArrangeTestFlow()
            .Send("foo")
            .AssertReply((activity) =>
            {
                // �A�_�v�e�B�u�J�[�h���r
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/Profile.json")).ToString()
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
                Assert.AreEqual(
                    (activity as Activity).Text,
                    "�v���t�@�C����ۑ����܂��B"
                );
            })
            .StartTestAsync();
        }


        [TestMethod]
        [DataRow(1800)]
        [DataRow(2020)]
        public async Task ProfileDialog_ShouldAskRetryWhenAgeOutOfRange(int year)
        {
            // �e�X�g�̒ǉ��Ǝ��s
            await ArrangeTestFlow()
            .Send("foo")
            .AssertReply((activity) =>
            {
                // �A�_�v�e�B�u�J�[�h���r
                Assert.AreEqual(
                    JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                    JObject.Parse(File.ReadAllText("./AdaptiveJsons/Profile.json")).ToString()
                );
            })
            .Send(new Activity()
            {
                Value = new JObject
                {
                    {"name", "Ken" },
                    {"email" , "kenakamu@microsoft.com"},
                    {"phone" , "xxx-xxxx-xxxx"},
                    {"birthday" , new DateTime(year, 7, 21)},
                    {"hasCat" , true},
                    {"catNum" , "3"},
                    {"catTypes", "�L�W�g��,�T�o�g��,�n�`����" },
                    {"playWithCat" , true}
                }.ToString()
            }).AssertReply((activity) =>
            {
                var birthday = new DateTime(year, 7, 21);
                var age = DateTime.Now.Year - birthday.Year;
                if (DateTime.Now < birthday.AddYears(age))
                    age--;

                Assert.AreEqual(
                    (activity as Activity).Text,
                    $"�N�{age}�΂ɂȂ�܂��B���������a���������Ă��������B"
                );
            })
            .StartTestAsync();
        }
    }
}