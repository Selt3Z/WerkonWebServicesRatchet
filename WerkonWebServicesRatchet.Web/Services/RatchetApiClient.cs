using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using WerkonWebServicesRatchet.Web.Models;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class RatchetApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AppTimeZone _appTimeZone;

    public RatchetApiClient(HttpClient httpClient, AppTimeZone appTimeZone)
    {
        _httpClient = httpClient;
        _appTimeZone = appTimeZone;
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

    private static bool IsUnauthorized(HttpResponseMessage response) =>
        response.StatusCode == System.Net.HttpStatusCode.Unauthorized;

    private static bool IsUnauthorized(Exception ex) =>
        ex is HttpRequestException httpEx
        && httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized;

    private async Task<T?> SafeGetFromJsonAsync<T>(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (IsUnauthorized(response))
            {
                return default;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (Exception ex) when (IsUnauthorized(ex))
        {
            return default;
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("api/auth/logout", null, cancellationToken);

        if (IsUnauthorized(response))
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

    public async Task<PagedResult<ClientListItem>> GetClientsAsync(
        string? name,
        string? phone,
        bool includeArchived = false,
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string>
        {
            $"skip={skip}",
            $"take={take}"
        };

        if (includeArchived)
        {
            queryParts.Add("includeArchived=true");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            queryParts.Add($"name={Uri.EscapeDataString(name)}");
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            queryParts.Add($"phone={Uri.EscapeDataString(phone)}");
        }

        var url = "api/clients?" + string.Join("&", queryParts);

        var result = await SafeGetFromJsonAsync<PagedResult<ClientListItem>>(url, cancellationToken);
        return result ?? new PagedResult<ClientListItem>();
    }

    public async Task<PagedResult<VehicleListItem>> GetVehiclesAsync(
        string? vin = null,
        string? licensePlate = null,
        string? clientName = null,
        bool includeArchived = false,
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string>
        {
            $"skip={skip}",
            $"take={take}"
        };

        if (includeArchived)
        {
            queryParts.Add("includeArchived=true");
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            queryParts.Add($"vin={Uri.EscapeDataString(vin)}");
        }

        if (!string.IsNullOrWhiteSpace(licensePlate))
        {
            queryParts.Add($"licensePlate={Uri.EscapeDataString(licensePlate)}");
        }

        if (!string.IsNullOrWhiteSpace(clientName))
        {
            queryParts.Add($"clientName={Uri.EscapeDataString(clientName)}");
        }

        var url = "api/vehicles?" + string.Join("&", queryParts);
        var result = await SafeGetFromJsonAsync<PagedResult<VehicleListItem>>(url, cancellationToken);
        return result ?? new PagedResult<VehicleListItem>();
    }

    public Task<(bool Success, string? ErrorMessage)> ArchiveClientAsync(Guid clientId, CancellationToken cancellationToken = default) =>
        SendArchiveRequestAsync($"api/clients/{clientId}/archive", cancellationToken);

    public Task<(bool Success, string? ErrorMessage)> RestoreClientAsync(Guid clientId, CancellationToken cancellationToken = default) =>
        SendArchiveRequestAsync($"api/clients/{clientId}/restore", cancellationToken);

    public Task<(bool Success, string? ErrorMessage)> DeleteClientAsync(Guid clientId, CancellationToken cancellationToken = default) =>
        SendDeleteRequestAsync($"api/clients/{clientId}", cancellationToken);

    public Task<(bool Success, string? ErrorMessage)> ArchiveVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        SendArchiveRequestAsync($"api/vehicles/{vehicleId}/archive", cancellationToken);

    public Task<(bool Success, string? ErrorMessage)> RestoreVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        SendArchiveRequestAsync($"api/vehicles/{vehicleId}/restore", cancellationToken);

    public Task<(bool Success, string? ErrorMessage)> DeleteVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        SendDeleteRequestAsync($"api/vehicles/{vehicleId}", cancellationToken);

    public Task<(bool Success, string? ErrorMessage)> ArchiveVisitAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        SendArchiveRequestAsync($"api/visits/{visitId}/archive", cancellationToken);

    public Task<(bool Success, string? ErrorMessage)> RestoreVisitAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        SendArchiveRequestAsync($"api/visits/{visitId}/restore", cancellationToken);

    public Task<(bool Success, string? ErrorMessage)> DeleteVisitAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        SendDeleteRequestAsync($"api/visits/{visitId}", cancellationToken);

    private async Task<(bool Success, string? ErrorMessage)> SendArchiveRequestAsync(
        string url,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, url);
        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (IsUnauthorized(response))
        {
            return (false, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            return (false, await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        return (true, null);
    }

    private async Task<(bool Success, string? ErrorMessage)> SendDeleteRequestAsync(
        string url,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync(url, cancellationToken);

        if (IsUnauthorized(response))
        {
            return (false, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            return (false, await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        return (true, null);
    }

    public async Task<ClientListItem?> GetClientAsync(
    Guid clientId,
    CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<ClientListItem>($"api/clients/{clientId}", cancellationToken);
    }

    public async Task<ClientListItem?> CreateClientAsync(
        ClientSaveModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/clients", model, cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ClientListItem>(cancellationToken);
    }

    public async Task<ClientListItem?> UpdateClientAsync(
        Guid clientId,
        ClientSaveModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/clients/{clientId}", model, cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ClientListItem>(cancellationToken);
    }

    public async Task<VehicleListItem?> GetVehicleAsync(
    Guid vehicleId,
    CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<VehicleListItem>($"api/vehicles/{vehicleId}", cancellationToken);
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

        if (IsUnauthorized(response))
        {
            return null;
        }

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

        if (IsUnauthorized(response))
        {
            return null;
        }

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
        return await SafeGetFromJsonAsync<VisitListItem>($"api/visits/{visitId}", cancellationToken);
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
            VisitedAtUtc = _appTimeZone.ToUtc(model.VisitedAtLocal.Value),
            model.MileageAtVisit,
            model.CustomerComplaint,
            model.MechanicComment
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"api/vehicles/{vehicleId}/visits",
            payload,
            cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

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
            VisitedAtUtc = _appTimeZone.ToUtc(model.VisitedAtLocal.Value),
            model.MileageAtVisit,
            model.CustomerComplaint,
            model.MechanicComment
        };

        var response = await _httpClient.PutAsJsonAsync(
            $"api/visits/{visitId}",
            payload,
            cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<VisitListItem>(cancellationToken);
    }

    public async Task<List<ScheduleVisitItemModel>> GetVisitsByDayAsync(
    DateOnly date,
    CancellationToken cancellationToken = default)
    {
        var url = $"api/visits/by-day?date={date:yyyy-MM-dd}";
        var result = await SafeGetFromJsonAsync<List<ScheduleVisitItemModel>>(url, cancellationToken);
        return result ?? [];
    }
    public async Task<ClientDetailsModel?> GetClientDetailsAsync(
    Guid clientId,
    CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<ClientDetailsModel>($"api/clients/{clientId}/details", cancellationToken);
    }

    public async Task<VehicleDetailsModel?> GetVehicleDetailsAsync(
    Guid vehicleId,
    CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<VehicleDetailsModel>($"api/vehicles/{vehicleId}/details", cancellationToken);
    }

    public async Task<VisitDetailsModel?> GetVisitDetailsAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<VisitDetailsModel>($"api/visits/{visitId}/details", cancellationToken);
    }

    public async Task<List<MechanicListItem>> GetMechanicsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/visits/mechanics", cancellationToken);

        if (IsUnauthorized(response) || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<List<MechanicListItem>>(cancellationToken) ?? [];
    }

    public async Task<VisitDetailsModel?> AssignVisitMechanicAsync(
        Guid visitId,
        Guid? mechanicUserId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsJsonAsync(
            $"api/visits/{visitId}/mechanic",
            new { AssignedMechanicUserId = mechanicUserId },
            cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<VisitDetailsModel>(cancellationToken);
    }

    public async Task<VisitListItem?> CloseVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        return await UpdateVisitStatusAsync(visitId, VisitStatuses.Completed, cancellationToken);
    }

    public async Task<VisitListItem?> UpdateVisitStatusAsync(
        Guid visitId,
        int status,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsJsonAsync(
            $"api/visits/{visitId}/status",
            new { Status = status },
            cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<VisitListItem>(cancellationToken);
    }

    public async Task<(byte[]? Data, string? FileName, string? ErrorMessage)> DownloadVisitWorkOrderAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/visits/{visitId}/work-order", cancellationToken);

        if (IsUnauthorized(response))
        {
            return (null, null, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            return (null, null, await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"work-order-{visitId.ToString()[..8]}.pdf";

        return (data, fileName, null);
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

        if (IsUnauthorized(response))
        {
            return null;
        }

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

        if (IsUnauthorized(response))
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<PagedResult<UserListItem>> GetUsersAsync(
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await SafeGetFromJsonAsync<PagedResult<UserListItem>>(
            $"api/users?skip={skip}&take={take}",
            cancellationToken);

        return result ?? new PagedResult<UserListItem>();
    }

    public async Task<UserListItem?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<UserListItem>($"api/users/{userId}", cancellationToken);
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

    public async Task<GlobalSearchResult> GlobalSearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return new GlobalSearchResult();
        }

        var url = $"api/search?q={Uri.EscapeDataString(query.Trim())}";
        return await SafeGetFromJsonAsync<GlobalSearchResult>(url, cancellationToken) ?? new GlobalSearchResult();
    }

    public async Task<PagedResult<ReminderByDayItemModel>> GetRemindersAsync(
        string status = "open",
        string scope = "all",
        string? q = null,
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<string>
        {
            $"status={Uri.EscapeDataString(status)}",
            $"scope={Uri.EscapeDataString(scope)}",
            $"skip={skip}",
            $"take={take}"
        };

        if (!string.IsNullOrWhiteSpace(q))
        {
            parts.Add($"q={Uri.EscapeDataString(q)}");
        }

        var url = "api/reminders?" + string.Join("&", parts);
        return await SafeGetFromJsonAsync<PagedResult<ReminderByDayItemModel>>(url, cancellationToken)
            ?? new PagedResult<ReminderByDayItemModel>();
    }

    public async Task<(bool Success, string? ErrorMessage)> RestoreBackupAsync(
        string relativePath,
        string confirmation,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/system/restore-backup",
            new { relativePath, confirmation },
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        if (IsUnauthorized(response))
        {
            return (false, null);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (false, string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
    }

    public async Task<List<ReminderByDayItemModel>> GetRemindersByDayAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/reminders/by-day?date={date:yyyy-MM-dd}";
        var result = await SafeGetFromJsonAsync<List<ReminderByDayItemModel>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<List<ReminderByDayItemModel>> GetRemindersByRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/reminders/by-range?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        var result = await SafeGetFromJsonAsync<List<ReminderByDayItemModel>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<SystemStatusModel?> GetSystemStatusAsync(
        CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<SystemStatusModel>("api/system/status", cancellationToken);
    }

    public async Task<BackupStatusModel?> GetBackupStatusAsync(
        CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<BackupStatusModel>("api/settings/backup-status", cancellationToken);
    }

    public async Task<ReminderDetailsModel?> GetReminderAsync(
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<ReminderDetailsModel>($"api/reminders/{reminderId}", cancellationToken);
    }

    public async Task<ReminderDetailsModel?> CreateReminderAsync(
        ReminderCreateModel model,
        CancellationToken cancellationToken = default)
    {
        if (!model.ReminderDateLocal.HasValue)
        {
            throw new InvalidOperationException("Reminder date is required.");
        }

        var payload = new
        {
            model.VehicleId,
            ReminderDate = DateOnly.FromDateTime(model.ReminderDateLocal.Value),
            model.Note
        };

        var response = await _httpClient.PostAsJsonAsync("api/reminders", payload, cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReminderDetailsModel>(cancellationToken);
    }

    public async Task<ReminderDetailsModel?> CloseReminderAsync(
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsync($"api/reminders/{reminderId}/close", null, cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReminderDetailsModel>(cancellationToken);
    }

    public Task<(bool Success, string? ErrorMessage)> DeleteReminderAsync(
        Guid reminderId,
        CancellationToken cancellationToken = default) =>
        SendDeleteRequestAsync($"api/reminders/{reminderId}", cancellationToken);

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

    public async Task<PagedResult<CatalogServiceListItem>> GetCatalogServicesAsync(
        string? search = null,
        bool activeOnly = false,
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"skip={skip}",
            $"take={take}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (activeOnly)
        {
            query.Add("activeOnly=true");
        }

        var url = $"api/catalog-services?{string.Join("&", query)}";

        var result = await SafeGetFromJsonAsync<PagedResult<CatalogServiceListItem>>(url, cancellationToken);
        return result ?? new PagedResult<CatalogServiceListItem>();
    }

    public async Task<CatalogServiceListItem?> GetCatalogServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<CatalogServiceListItem>(
            $"api/catalog-services/{serviceId}",
            cancellationToken);
    }

    public async Task<CatalogServiceListItem?> CreateCatalogServiceAsync(
        CatalogServiceSaveModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/catalog-services", model, cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<CatalogServiceListItem>(cancellationToken);
    }

    public async Task<CatalogServiceListItem?> UpdateCatalogServiceAsync(
        Guid serviceId,
        CatalogServiceSaveModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/catalog-services/{serviceId}",
            model,
            cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<CatalogServiceListItem>(cancellationToken);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteCatalogServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/catalog-services/{serviceId}", cancellationToken);

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

    public async Task<PagedResult<AuditLogItemModel>> GetAuditLogAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await SafeGetFromJsonAsync<PagedResult<AuditLogItemModel>>(
            $"api/audit-log?skip={skip}&take={take}",
            cancellationToken);

        return result ?? new PagedResult<AuditLogItemModel>();
    }

    public async Task<SettingsModel?> GetSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<SettingsModel>("api/settings", cancellationToken);
    }

    public async Task<OrganizationProfileModel?> GetOrganizationProfileAsync(
        CancellationToken cancellationToken = default)
    {
        return await SafeGetFromJsonAsync<OrganizationProfileModel>("api/settings/organization", cancellationToken);
    }

    public async Task<OrganizationProfileModel?> UpdateOrganizationProfileAsync(
        OrganizationProfileModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            "api/settings/organization",
            new
            {
                model.OrganizationName,
                model.Phone,
                model.Email,
                model.Address,
                model.Inn,
                model.Kpp,
                model.Ogrn,
                model.BankAccount,
                model.Bik
            },
            cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<OrganizationProfileModel>(cancellationToken);
    }

    public async Task<OrganizationProfileModel?> UploadOrganizationLogoAsync(
        string contentType,
        string dataBase64,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            "api/settings/organization/logo",
            new { ContentType = contentType, DataBase64 = dataBase64 },
            cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<OrganizationProfileModel>(cancellationToken);
    }

    public async Task<OrganizationProfileModel?> DeleteOrganizationLogoAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync("api/settings/organization/logo", cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadApiErrorMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<OrganizationProfileModel>(cancellationToken);
    }

    public async Task<SettingsModel?> UpdateTimeZoneAsync(
        string timeZoneId,
        CancellationToken cancellationToken = default)
    {
        return await UpdateSettingsAsync(new SettingsModel { TimeZoneId = timeZoneId }, cancellationToken);
    }

    public async Task<SettingsModel?> UpdateSettingsAsync(
        SettingsModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            "api/settings",
            new
            {
                model.TimeZoneId,
                model.Theme,
                model.CurrencyCode,
                model.DefaultVisitStatus,
                model.ReminderLookbackDays,
                model.HideArchivedByDefault,
                model.ManagerCanHardDelete,
                model.MechanicCanAddCatalogServices,
                model.AuditRetentionDays,
                model.ListPageSize
            },
            cancellationToken);

        if (IsUnauthorized(response))
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var message = await ReadApiErrorMessageAsync(response, cancellationToken);
            throw new InvalidOperationException(message);
        }

        return await response.Content.ReadFromJsonAsync<SettingsModel>(cancellationToken);
    }
}