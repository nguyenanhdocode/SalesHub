using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Suppliers.Delete;
using Application.Features.Suppliers.Get;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Suppliers;

public class GetSupplierTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public GetSupplierTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Get_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        var code = Guid.NewGuid().ToString();
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

            var res = await sender.Send(new GetSupplierQuery { SupplierId = insertedId }, CancellationToken.None);

            Assert.Equal(insertedId, res.SupplierId);
            Assert.Equal(command.Name, res.Name);
            Assert.Equal(command.ContactPerson, res.ContactPerson);
            Assert.Equal(command.Phone, res.Phone);
            Assert.Equal(command.TaxCode, res.TaxCode);
            Assert.Equal(command.Email, res.Email);
            Assert.Equal(command.Address, res.Address);
        }
        finally
        {
            await dataRand.DeleteSupplier(insertedId);
        }
    }

    [Fact]
    public async Task Get_Should_Throw_NotFound()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
        {
           await sender.Send(new GetSupplierQuery { SupplierId = int.MaxValue }, CancellationToken.None); 
        });

        Assert.Equal("notfound", ex.Code);
    }
}