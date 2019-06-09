using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class NumberValidatorsUnitTest
    {       

        private PromptValidatorContext<T> CreatePromptValidatorContext<T>(bool succeeded, T obj, object validations)
        {
            // リフレクションでコンストラクターを取得して実行し、PromptValidatorContext を作成
            var promptContext = (PromptValidatorContext<T>)typeof(PromptValidatorContext<T>).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                new[] {
                    typeof(ITurnContext),
                    typeof(PromptRecognizerResult<T>),
                    typeof(IDictionary<string, object>),
                    typeof(PromptOptions)
                }, null)
                .Invoke(new object[]
                {
                    null,
                    new PromptRecognizerResult<T>() { Succeeded = succeeded, Value = obj },
                    null,
                    new PromptOptions(){ Validations = validations }
                });

            return promptContext;
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(50)]
        [DataRow(120)]
        public async Task NumberValidators_ShouldReturnTrueWithValidNumber(int input)
        {
            var promptContext = CreatePromptValidatorContext<int>(true, input, new NumberRange() { MinValue = 0, MaxValue = 120 });
            var result = await NumberValidators.ValidateRangeAsync(promptContext, CancellationToken.None);
            Assert.IsTrue(result);
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(121)]
        public async Task NumberValidators_ShouldReturnFalseWithInalidNumber(int input)
        {
            var promptContext = CreatePromptValidatorContext<int>(true, input, new NumberRange() { MinValue = 0, MaxValue = 120 });
            var result = await NumberValidators.ValidateRangeAsync(promptContext, CancellationToken.None);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task NumberValidators_ShouldReturnFalseWithFailedResult()
        {
            var promptContext = CreatePromptValidatorContext<int>(false, 0, new NumberRange() { MinValue = 0, MaxValue = 120 });
            var result = await NumberValidators.ValidateRangeAsync(promptContext, CancellationToken.None);
            Assert.IsFalse(result);
        }
    }
}
