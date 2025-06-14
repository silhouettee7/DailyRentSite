using System.Net.Http.Json;
using System.Text.Json;
using AutoMapper;
using Domain.Abstractions.Clients;
using Domain.Models.Payment;
using Infrastructure.PaymentSystem.Api;

namespace Infrastructure.PaymentSystem;

public class PaymentApiClient(HttpClient httpClient, IMapper mapper): IPaymentApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    public async Task<PaymentCreated> CreatePaymentAsync(PaymentCreate paymentCreate)
    {
        httpClient.DefaultRequestHeaders.Add("Idempotence-Key", Guid.NewGuid().ToString());
        var response = await httpClient.PostAsJsonAsync("payments", paymentCreate, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        if (response.IsSuccessStatusCode)
        {
            var paymentCreatedResponse = await response.Content.ReadFromJsonAsync<PaymentCreatedResponse>(new JsonSerializerOptions{ PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower});
            return mapper.Map<PaymentCreated>(paymentCreatedResponse);
        }
        throw new Exception($"Payment creation failed with status code {response.StatusCode}");
    }

    public async Task<PaymentInfo> CheckForPaymentAsync(Guid paymentId)
    {
        var response = await httpClient.GetAsync($"payments/{paymentId}");
        if (response.IsSuccessStatusCode)
        {
            var paymentInfoResponse = await response.Content.ReadFromJsonAsync<PaymentInfoResponse>();
            return mapper.Map<PaymentInfo>(paymentInfoResponse);
        }
        throw new Exception($"Getting payment info failed with status code {response.StatusCode}");
    }
}