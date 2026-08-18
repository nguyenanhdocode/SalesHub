namespace Application.Shared;

public class InventoryBalanceSqls
{
    public const string UPDATE_INVENTORY_BALANCE_SQL = @"
    UPDATE inventory_balances
    SET quantity = quantity + @QuantityDelta, amount = amount + @AmountDelta
    WHERE warehouse_id = @WarehouseId AND product_id = @ProductId AND unit_id = @UnitId;
    ";

    public const string UPSERT_INVENTORY_BALANCE_SQL = @"
    INSERT INTO public.inventory_balances AS ib(
          warehouse_id
        , product_id
        , unit_id
        , quantity
        , amount
    )
	VALUES (
          @WarehouseId
        , @ProductId
        , @UnitId
        , @Quantity
        , @Amount
    )
    ON CONFLICT (warehouse_id, product_id, unit_id)
    DO UPDATE
    SET quantity = ib.quantity + EXCLUDED.quantity
    , amount = ib.amount + EXCLUDED.amount
    ";
}
