using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Configuration;
using Microsoft.Extensions.Localization;

[Route("api/[controller]")]
[ApiController]
public class NotificationsController : ControllerBase
{
    private IServiceProvider serviceProvider;
    private IStringLocalizer<NotificationsController> localizer;
    private ScheduleNotificationStore scheduleNotificationStore;
    private BotFrameworkAdapter adapter;
    private BotConfiguration botConfig;
    public NotificationsController(
        IServiceProvider serviceProvider,
        IStringLocalizer<NotificationsController> localizer,
        BotConfiguration botConfig,
        ScheduleNotificationStore scheduleNotificationStore)
    {
        this.serviceProvider = serviceProvider;
        this.localizer = localizer;
        this.botConfig = botConfig;
        this.scheduleNotificationStore = scheduleNotificationStore;
        this.adapter = (BotFrameworkAdapter)serviceProvider.GetService(typeof(IAdapterIntegration));
    }

    [HttpPost]
    public ActionResult Post()
    {
        // 現在時刻より前の通知時間が設定されているものを全て取得
        var scheduleNotifications = scheduleNotificationStore
            .Where(x => x.NotificationTime.ToUniversalTime() < DateTime.UtcNow).ToList();
        // 通知と削除
        scheduleNotifications.ForEach(async (x) =>
        {
            await SendProactiveMessage(x);
            DeleteCompletedNotificaitons(x);
        });
        return new OkObjectResult(true);
    }

    private async Task SendProactiveMessage(ScheduleNotification scheduleNotification)
    {
        // 構成ファイルより Endpoint を取得
        EndpointService endpointService = (EndpointService)botConfig.Services.Where(x => x.Type == "endpoint").First();

        await adapter.ContinueConversationAsync(
               endpointService.AppId,
               scheduleNotification.ConversationReference,
               async (turnContext, token) =>
               {
                   await turnContext.SendActivityAsync(
                       $"{localizer["notification"]}:{scheduleNotification.StartTime.ToString("HH:mm")} - [{scheduleNotification.Title}]({scheduleNotification.WebLink})");
               },
               CancellationToken.None);
    }

    private void DeleteCompletedNotificaitons(ScheduleNotification scheduleNotification)
    {
        scheduleNotificationStore.Remove(scheduleNotification);
    }
}
