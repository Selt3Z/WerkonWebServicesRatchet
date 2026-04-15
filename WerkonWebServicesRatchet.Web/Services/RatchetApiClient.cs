using System.Net.Http.Json;
using WerkonWebServicesRatchet.Web.Models;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class RatchetApiClient
{
    private readonly HttpClient _httpClient;

    public RatchetApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ClientListItem>> GetClientsAsync(
        string? name,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(name))
        {
            queryParts.Add($"name={Uri.EscapeDataString(name)}");
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            queryParts.Add($"phone={Uri.EscapeDataString(phone)}");
        }

        var url = "api/clients";

        if (queryParts.Count > 0)
        {
            url += "?" + string.Join("&", queryParts);
        }

        var result = await _httpClient.GetFromJsonAsync<List<ClientListItem>>(url, cancellationToken);

        return result ?? [];
    }

    public async Task<ClientListItem?> GetClientAsync(
    Guid clientId,
    CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ClientListItem>(
            $"api/clients/{clientId}",
            cancellationToken);
    }

    public async Task<ClientListItem?> CreateClientAsync(
        ClientSaveModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/clients", model, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ClientListItem>(cancellationToken);
    }

    public async Task<ClientListItem?> UpdateClientAsync(
        Guid clientId,
        ClientSaveModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/clients/{clientId}", model, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ClientListItem>(cancellationToken);
    }

    public async Task<VehicleListItem?> GetVehicleAsync(
    Guid vehicleId,
    CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<VehicleListItem>(
            $"api/vehicles/{vehicleId}",
            cancellationToken);
    }

    public async Task<VehicleListItem?> CreateVehicleAsync(
        Guid clientId,
        VehicleSaveModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/clients/{clientId}/vehicles",
            model,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<VehicleListItem>(cancellationToken);
    }

    public async Task<VehicleListItem?> UpdateVehicleAsync(
        Guid vehicleId,
        VehicleSaveModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/vehicles/{vehicleId}",
            model,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        /*if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Update vehicle failed. Status: {(int)response.StatusCode}. Body: {errorText}");
        }*/

        return await response.Content.ReadFromJsonAsync<VehicleListItem>(cancellationToken);
    }

    public async Task<VisitListItem?> GetVisitAsync(
    Guid visitId,
    CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<VisitListItem>(
            $"api/visits/{visitId}",
            cancellationToken);
    }

    public async Task<VisitListItem?> CreateVisitAsync(
        Guid vehicleId,
        VisitSaveModel model,
        CancellationToken cancellationToken = default)
    {
        if (!model.VisitedAtLocal.HasValue)
        {
            throw new Exception("Visit date and time is required.");
        }

        var payload = new
        {
            VisitedAtUtc = DateTime.SpecifyKind(model.VisitedAtLocal.Value, DateTimeKind.Local).ToUniversalTime(),
            model.MileageAtVisit,
            model.CustomerComplaint,
            model.MechanicComment
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"api/vehicles/{vehicleId}/visits",
            payload,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<VisitListItem>(cancellationToken);
    }

    public async Task<VisitListItem?> UpdateVisitAsync(
        Guid visitId,
        VisitSaveModel model,
        CancellationToken cancellationToken = default)
    {
        if (!model.VisitedAtLocal.HasValue)
        {
            throw new Exception("Visit date and time is required.");
        }

        var payload = new
        {
            VisitedAtUtc = DateTime.SpecifyKind(model.VisitedAtLocal.Value, DateTimeKind.Local).ToUniversalTime(),
            model.MileageAtVisit,
            model.CustomerComplaint,
            model.MechanicComment
        };

        var response = await _httpClient.PutAsJsonAsync(
            $"api/visits/{visitId}",
            payload,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<VisitListItem>(cancellationToken);
    }
    public async Task<ClientDetailsModel?> GetClientDetailsAsync(
    Guid clientId,
    CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ClientDetailsModel>(
            $"api/clients/{clientId}/details",
            cancellationToken);
    }

    public async Task<VehicleDetailsModel?> GetVehicleDetailsAsync(
    Guid vehicleId,
    CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<VehicleDetailsModel>(
            $"api/vehicles/{vehicleId}/details",
            cancellationToken);
    }

    public async Task<VisitDetailsModel?> GetVisitDetailsAsync(
    Guid visitId,
    CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<VisitDetailsModel>(
            $"api/visits/{visitId}/details",
            cancellationToken);
    }
}