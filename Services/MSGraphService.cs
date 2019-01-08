using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;

public class MSGraphService
{
    private string token;
    public MSGraphService(string token)
    {
        this.token = token;
    }

    public async Task<User> GetMeAsync()
    {
        var graphClient = GetAuthenticatedClient();
        var me = await graphClient.Me.Request().GetAsync();
        return me;
    }

    public async Task<Stream> GetPhotoAsync()
    {
        var graphClient = GetAuthenticatedClient();
        var profilePhoto = await graphClient.Me.Photo.Content.Request().GetAsync();       
        return profilePhoto;
    }

    public async Task UpdatePhotoAsync(Stream image)
    {
        var graphClient = GetAuthenticatedClient();
        await graphClient.Me.Photo.Content.Request().PutAsync(image);
        return;
    }

    public async Task<List<Event>> GetScheduleAsync()
    {
        var graphClient = GetAuthenticatedClient();
        var queryOption = new List<QueryOption>(){
            new QueryOption("startDateTime", DateTime.Today.ToString()),
            new QueryOption("endDateTime", DateTime.Today.AddDays(1).ToString())
        };
        var events = await graphClient.Me.CalendarView.Request(queryOption).GetAsync();
        return events.CurrentPage.ToList();
    }

    private GraphServiceClient GetAuthenticatedClient()
    {
        var graphClient = new GraphServiceClient(
            new DelegateAuthenticationProvider(
                requestMessage =>
                {
                    // トークンを指定
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                    // タイムゾーンを指定
                    requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Local.Id + "\"");
                    return Task.CompletedTask;
                }));
        return graphClient;
    }
}