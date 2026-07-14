using Microsoft.AspNetCore.Mvc;
using WerkonWebServicesRatchet.Contracts.Clients;
using WerkonWebServicesRatchet.Features.Clients;

namespace WerkonWebServicesRatchet.Tests;

public sealed class ClientValidationTests
{
    [Fact]
    public async Task Create_MissingNameAndPhone_ReturnsValidationProblem()
    {
        await using var dbContext = TestHelpers.CreateDbContext();
        var controller = new ClientsController(dbContext);

        var result = await controller.Create(
            new SaveClientRequest { FullName = " ", PhoneNumber = "" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);

        Assert.Contains(nameof(SaveClientRequest.FullName), problem.Errors.Keys);
        Assert.Contains(nameof(SaveClientRequest.PhoneNumber), problem.Errors.Keys);
        Assert.Empty(dbContext.Clients);
    }

    [Fact]
    public async Task Create_ValidRequest_TrimsAndStoresClient()
    {
        await using var dbContext = TestHelpers.CreateDbContext();
        var controller = new ClientsController(dbContext);

        var result = await controller.Create(
            new SaveClientRequest
            {
                FullName = "  Ivan Petrov  ",
                PhoneNumber = " +79991234567 ",
                Notes = "   "
            },
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);

        var stored = Assert.Single(dbContext.Clients);
        Assert.Equal("Ivan Petrov", stored.FullName);
        Assert.Equal("+79991234567", stored.PhoneNumber);
        Assert.Null(stored.Notes);
        Assert.False(stored.IsArchived);
    }

    [Fact]
    public async Task Update_MissingPhone_ReturnsValidationProblem()
    {
        await using var dbContext = TestHelpers.CreateDbContext();
        var client = TestHelpers.CreateClient();
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        var controller = new ClientsController(dbContext);

        var result = await controller.Update(
            client.Id,
            new SaveClientRequest { FullName = "New Name", PhoneNumber = "" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);

        Assert.Contains(nameof(SaveClientRequest.PhoneNumber), problem.Errors.Keys);
    }
}
