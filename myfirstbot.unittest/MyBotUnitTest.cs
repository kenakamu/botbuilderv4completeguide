using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MyBotUnitTest
    {
        [TestMethod]
        public async Task MyBot_ShouldReturnSameText()
        {
            // �A�_�v�^�[���쐬
            var adapter = new TestAdapter();
            
            // �e�X�g�Ώۂ̃N���X���C���X�^���X��
            var bot = new MyBot();
            await new TestFlow(adapter, bot.OnTurnAsync)
                .Test("foo", "foo")
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
