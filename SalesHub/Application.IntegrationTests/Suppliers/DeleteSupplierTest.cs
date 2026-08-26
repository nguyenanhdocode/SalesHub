using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Suppliers.Delete;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Suppliers;

public class DeleteSupplierTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public DeleteSupplierTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Delete_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        var code = Guid.NewGuid().ToString("N")[..25];
        int insertedId = 0;

        var command = new CreateSupplierCommand
        {
            Code = code.ToString(),
            Name = $"{code}name",
            ContactPerson = $"{code}contactperson",
            Phone = "0300000000",
            TaxCode = "0400000000",
            Email = $"{code}@gmail.com",
            Address = $"{code}address"
        };

        try
        {

            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.True(insertedId > 0);

            await sender.Send(new DeleteSupplierCommand { SupplierId = insertedId }, CancellationToken.None);

            int count = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM suppliers WHERE supplier_id = @SupplierId
            ", new { SupplierId = insertedId });

            Assert.Equal(0, count);
        }
        finally
        {
            await dataRand.DeleteSupplier(insertedId);
        }
    }
}