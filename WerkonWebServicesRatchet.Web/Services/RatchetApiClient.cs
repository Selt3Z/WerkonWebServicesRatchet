using System.Net.Http.Json;
using System.Text.Json;
using WerkonWebServicesRatchet.Web.Models;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class RatchetApiClient
{
    private readonly HttpClient _httpClient;

    public RatchetApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> LoginAsync(
        LoginModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            new
            {
                UserName = model.UserName.Trim(),
                Password = model.Password
            },
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("api/auth/logout", null, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<CurrentUserModel?> GetCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/auth/me", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CurrentUserModel>(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
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

    public async Task<List<ScheduleVisitItemModel>> GetVisitsByDayAsync(
    DateOnly date,
    CancellationToken cancellationToken = default)
    {
        var url = $"api/visits/by-day?date={date:yyyy-MM-dd}";

        var result = await _httpClient.GetFromJsonAsync<List<ScheduleVisitItemModel>>(url, cancellationToken);

        return result ?? [];
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

    public async Task<VisitServiceItemModel?> CreateVisitServiceItemAsync(
        Guid visitId,
        VisitServiceItemSaveModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/visits/{visitId}/service-items",
            model,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<VisitServiceItemModel>(cancellationToken);
    }

    public async Task DeleteVisitServiceItemAsync(
        Guid visitId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"api/visits/{visitId}/service-items/{itemId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task<List<UserListItem>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<UserListItem>>("api/users", cancellationToken);
        return result ?? [];
    }

    public async Task<UserListItem?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<UserListItem>(
            $"api/users/{userId}",
            cancellationToken);
    }

    public async Task<(UserListItem? User, string? ErrorMessage)> CreateUserAsync(
        UserSaveModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/users", model, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return (null, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            return (null, await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        var user = await response.Content.ReadFromJsonAsync<UserListItem>(cancellationToken);
        return (user, null);
    }

    public async Task<(UserListItem? User, string? ErrorMessage)> UpdateUserAsync(
        Guid userId,
        UserSaveModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/users/{userId}", model, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return (null, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            return (null, await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        var user = await response.Content.ReadFromJsonAsync<UserListItem>(cancellationToken);
        return (user, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/users/{userId}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return (false, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            return (false, await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        return (true, null);
    }

    private static async Task<string> ReadApiErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return $"HTTP {(int)response.StatusCode}";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (root.TryGetProperty("message", out var messageProperty)
                && messageProperty.ValueKind == JsonValueKind.String)
            {
                var message = messageProperty.GetString();

                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }

            if (root.TryGetProperty("errors", out var errorsProperty)
                && errorsProperty.ValueKind == JsonValueKind.Object)
            {
                var messages = new List<string>();

                foreach (var property in errorsProperty.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var value = item.GetString();

                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                messages.Add(value);
                            }
                        }
                    }
                }

                if (messages.Count > 0)
                {
                    return string.Join(" ", messages);
                }
            }

            if (root.TryGetProperty("detail", out var detailProperty)
                && detailProperty.ValueKind == JsonValueKind.String)
            {
                var detail = detailProperty.GetString();

                if (!string.IsNullOrWhiteSpace(detail))
                {
                    return detail;
                }
            }
        }
        catch (JsonException)
        {
        }

        return content;
    }
}