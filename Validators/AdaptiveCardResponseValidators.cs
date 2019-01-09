using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

public static class AdaptiveCardResponseValidators
{
    public static Task<bool> ValidateInput(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
    {
        // 文字は返ってこないため Succeeded の場合は false
        if (promptContext.Recognized.Succeeded)
        {
            return Task.FromResult(false);
        }

        // オプションの検証プロパティから NumberRange を取得。
        NumberRange range = promptContext.Options.Validations is NumberRange ?
            (NumberRange)promptContext.Options.Validations :
            new NumberRange() { MinValue = 0, MaxValue = 120 };

        var input = JsonConvert.DeserializeObject<UserProfile>(promptContext.Context.Activity.Value.ToString());
        // 検証する場合、ここで検証
        // 0 より小さいか 120 より大きい場合は False
        if (input.Age < range.MinValue || input.Age > range.MaxValue)
        {
            promptContext.Context.SendActivityAsync("年齢は 1 以上 120 未満で入れてください。");
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }
}