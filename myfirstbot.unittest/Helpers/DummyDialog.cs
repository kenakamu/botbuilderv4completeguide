using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

public class DummyDialog : ComponentDialog
{
    public DummyDialog(string dialogId) : base(dialogId)
    {   
        // ウォーターフォールのステップを定義。処理順にメソッドを追加。
        var waterfallSteps = new WaterfallStep[]
        {
            CompleteAsync,
        };

        // ウォーターフォールダイアログと各種プロンプトを追加
        AddDialog(new WaterfallDialog("dummy", waterfallSteps));
    }

    private async Task<DialogTurnResult> CompleteAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.EndDialogAsync(true);
    }    
}