using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        // 一旦 JObject として中身をパース
        var input = JObject.Parse(promptContext.Context.Activity.Value.ToString());
        // CatTypes がある場合、対応する UserProfile クラスのプロパティが 、
        // List<string> のため、JArray に変換。
        if (input.ContainsKey("catTypes"))
            input["catTypes"] = new JArray(input["catTypes"].ToString().Split(','));

        // 誕生日から年齢を計算し新しいプロパティとして追加
        var birthday = DateTime.Parse(input["birthday"].ToString());
        var age = DateTime.Now.Year - birthday.Year;
        if (DateTime.Now < birthday.AddYears(age))
            age--;
        input["age"] = age;
        // 0 より小さいか 120 より大きい場合は False
        if (age < range.MinValue || age > range.MaxValue)
        {
            promptContext.Context.SendActivityAsync($"年齢が{age}歳になります。ただしい誕生日を入れてください。");
            return Task.FromResult(false);
        }
        // 値を入れ替え
        promptContext.Context.Activity.Value = input;
        return Task.FromResult(true);
    }
}