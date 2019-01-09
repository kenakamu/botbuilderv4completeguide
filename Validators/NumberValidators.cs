using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

public static class NumberValidators
{
    public static Task<bool> ValidateRangeAsync(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
    {
        // 期待した型であったか確認して違えば失敗とみなす
        if (!promptContext.Recognized.Succeeded)
        {
            return Task.FromResult(false);
        }

        // ユーザーのインプットを取得
        var value = promptContext.Recognized.Value;

        // オプションの検証プロパティから NumberRange を取得。
        NumberRange range = promptContext.Options.Validations is NumberRange ?
            (NumberRange)promptContext.Options.Validations :
            new NumberRange() { MinValue = 0, MaxValue = 120 };

        // MinValue より小さいか MaxValue より大きい場合は False
        if (value < range.MinValue || value > range.MaxValue)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}