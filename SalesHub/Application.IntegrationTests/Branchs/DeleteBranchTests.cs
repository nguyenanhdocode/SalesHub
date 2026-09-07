using System.Media;
using Application.Exceptions;
using Application.Features.Branchs.Create;
using Application.Features.Branchs.Delete;
using Application.Features.Suppliers.Create;
using Application.Features.Units.Create;
using Application.Features.Warehouses.Create;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Branchs;

public class DeleteBranchTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public DeleteBranchTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataSeed = scope.ServiceProvider.GetRequiredService<DataRandom>();
        var code = Guid.NewGuid().ToString("N")[..25];
        int branchId = 0;

        try
        {
            var command = new CreateBranchCommand
            {
                Code = code,
                Name = code,
                Address = $"{code}address",
                Email = $"{code}@gmail.com",
                Phone = "0000000000",
                TaxCode = code
            };

            branchId = await sender.Send(command, CancellationToken.None);
            Assert.True(branchId > 0);

            await sender.Send(new DeleteBranchCommand { BranchId = branchId }, CancellationToken.None);

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM branchs
            WHERE branch_id = @BranchId
            ", new { BranchId = branchId });

            Assert.Equal(0, testId);
        }
        finally
        {
            await dataSeed.DeleteBranch(branchId);
        }
    }
}