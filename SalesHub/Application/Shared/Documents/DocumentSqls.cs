namespace Application.Shared.Documents;

public class DocumentSqls
{
    public const string INSERT_DOCUMENT_SQL = @"
    INSERT INTO public.documents(
          document_id
        , document_no
        , posting_date
        , document_date
        , period_id
        , document_type
        , created_by
        , note
        , status
    )
	VALUES (
          @DocumentId
        , @DocumentNo
        , @PostingDate
        , @DocumentDate
        , @PeriodId
        , @DocumentType
        , @CreatedBy
        , @Note
        , @Status
    );
    ";

    public const string MERGE_BALANCES_SQL = @"
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

    public const string CHECK_POSTINGDATE_SQL = @"
    SELECT EXISTS(SELECT 1 FROM periods WHERE period_id = @PeriodId AND @PostingDate BETWEEN from_date AND to_date);
    ";
}

