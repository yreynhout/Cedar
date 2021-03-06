﻿// ReSharper disable once CheckNamespace
namespace Cedar.Commands.Client
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Cedar.Commands.ExceptionModels;
    using Newtonsoft.Json;

    public static class HttpClientExtensions
    {
        public static async Task ExecuteCommand(this HttpClient client, object command, Guid commandId, ICommandExecutionSettings settings)
        {
            string commandJson = JsonConvert.SerializeObject(command, DefaultJsonSerializerSettings.Settings);
            var httpContent = new StringContent(commandJson);
            httpContent.Headers.ContentType =
                MediaTypeHeaderValue.Parse("application/vnd.{0}.{1}+json".FormatWith(settings.Vendor, command.GetType().Name.ToLower()));

            var request = new HttpRequestMessage(HttpMethod.Put, settings.Path + "/{0}".FormatWith(commandId))
            {
                Content = httpContent
            };
            request.Headers.Accept.ParseAdd("application/json");
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException("Got 404 Not Found for {0}".FormatWith(request.RequestUri));
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var exceptionModel = await response.Content.ReadObject(DefaultJsonSerializerSettings.Settings) as ExceptionModel;
                throw settings.ModelToExceptionConverter.Convert(exceptionModel);
            }
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                var exceptionModel = await response.Content.ReadObject(DefaultJsonSerializerSettings.Settings) as ExceptionModel;
                throw settings.ModelToExceptionConverter.Convert(exceptionModel);
            }
        }
    }
}