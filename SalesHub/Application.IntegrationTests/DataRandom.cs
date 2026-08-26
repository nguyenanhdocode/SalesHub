using Application.Shared;
using Dapper;

namespace Application.IntegrationTests;

public class DataRandom
{
    private readonly DbSession _dbSession;
    public DataRandom(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<int> RandomBranch()
    {
        var code = Guid.NewGuid().ToString("N")[..25];

        int branchId = await _dbSession.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO public.branchs(code, name, address, phone, email, tax_code)
            VALUES (@Code, @Name, @Address, @Phone, @Email, @TaxCode)
            RETURNING branch_id;
        ", new
        {
            Code = code,
            Name = $"{code}code",
            Address = $"{code}address",
            Phone = "0000000000",
            Email = $"{code}@test.local",
            TaxCode = $"{code}taxcode"
        });

        return branchId;
    }

    public async Task DeleteBranch(int branchId)
    {
        await _dbSession.Connection.ExecuteAsync(@"
        DELETE FROM branchs WHERE branch_id = @BranchId
        ", new
        {
           BranchId = branchId                                          
        });
    }

    public async Task<int> RandomPeriod()
    {
        var code = Guid.NewGuid().ToString("N")[..25];

        int periodId = await _dbSession.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO public.periods(code, name, from_date, to_date, is_closed)
            VALUES (@Code, @Name, @FromDate, @ToDate, false)
            RETURNING period_id;
        ", new
        {
            Code = code,
            Name = $"{code}name",
            FromDate = DateTime.UtcNow.AddDays(-5),
            ToDate = DateTime.UtcNow.AddDays(20)
        });

        return periodId;
    }

    public async Task DeletePeriod(int periodId)
    {
        await _dbSession.Connection.ExecuteAsync(@"
        DELETE FROM periods WHERE period_id = @PeriodId
        ", new
        {
           PeriodId = periodId                                          
        });
    }

    public async Task<int> RandomWarehouse(int branchId)
    {
        var code = Guid.NewGuid().ToString("N")[..25];

        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO warehouses (code, name, branch_id)
            VALUES (@Code, @Name, @BranchId)
            RETURNING warehouse_id;
        "
        , new {
            Code = code,
            Name = $"{code}name",
            BranchId = branchId
        });

        return id;
    }

    public async Task DeleteWarehouse(int warehouseId)
    {
        await _dbSession.Connection.ExecuteAsync(@"
        DELETE FROM warehouses WHERE warehouse_id = @WarehouseId
        ", new
        {
           WarehouseId = warehouseId                                          
        });
    }

    public async Task<Guid> RandomUser()
    {
        var code = Guid.NewGuid().ToString();

        var id = await _dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            INSERT INTO users (username, password)
            VALUES (@Username, @Password)
            RETURNING user_id;
        "
        , new {
            Username = code,
            Password = code,
        });

        return id;
    }

    public async Task DeleteUser(Guid userId)
    {
        await _dbSession.Connection.ExecuteAsync(@"
        DELETE FROM users WHERE user_id = @Userid
        ", new
        {
           Userid = userId                                          
        });
    }

    public async Task<int> RandomSupplier()
    {
        var code = Guid.NewGuid().ToString("N")[..25];

        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO suppliers (code, name)
            VALUES (@Code, @Name)
            RETURNING supplier_id;
        "
        , new {
            Code = code,
            Name = $"{code}name",
        });

        return id;
    }

    public async Task DeleteSupplier(int supplierId)
    {
        await _dbSession.Connection.ExecuteAsync(@"
        DELETE FROM suppliers WHERE supplier_id = @SupplierId
        ", new
        {
           SupplierId = supplierId                                          
        });
    }

    public async Task<int> RandomUnit()
    {
        var code = Guid.NewGuid().ToString("N")[..25];

        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO units (code, name)
            VALUES (@Code, @Name)
            RETURNING unit_id;
        "
        , new {
            Code = code,
            Name = $"{code}name",
        });

        return id;
    }

    public async Task DeleteUnit(int unitId)
    {
        await _dbSession.Connection.ExecuteAsync(@"
        DELETE FROM units WHERE unit_id = @UnitId
        ", new
        {
           UnitId = unitId                                          
        });
    }

    public async Task<int> RandomProduct(int unitId, int supplierId)
    {
        var code = Guid.NewGuid().ToString("N")[..25];

        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO public.products(
                  internal_code
                , external_code
                , name
                , costing_method
                , base_unit_id
                , supplier_id)
	        VALUES (
                  @InternalCode
                , @ExternalCode
                , @Name
                , 'AVG'
                , @BaseUnitId
                , @SupplierId
            )
            RETURNING product_id;
        "
        , new {
            InternalCode = code,
            ExternalCode = code,
            Name = $"{code}name",
            BaseUnitId = unitId,
            SupplierId = supplierId
        });

        return id;
    }

    public async Task DeleteProduct(int productId)
    {
        await _dbSession.Connection.ExecuteAsync(@"
        DELETE FROM products WHERE product_id = @ProductId
        ", new
        {
           ProductId = productId                                          
        });
    }

    public async Task DeleteDocument(Guid documentId)
    {
        await _dbSession.Connection.ExecuteAsync(@"
        DELETE FROM documents WHERE document_id = @DocumentId
        ", new
        {
           DocumentId = documentId                                          
        });
    }
}
