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

    public const string UPDATE_DOCUMENT_SQL = @"
    UPDATE public.documents
	SET posting_date=@PostingDate
    , document_date=@DocumentDate
    , period_id=@PeriodId
    , updated_date=CURRENT_TIMESTAMP
    , updated_by=@UpdatedBy
    , note=@Note
    , status=@Status
	WHERE document_id = @DocumentId;
    ";

    public const string CHECK_POSTINGDATE_SQL = @"
    SELECT EXISTS(SELECT 1 FROM periods WHERE period_id = @PeriodId AND @PostingDate BETWEEN from_date AND to_date);
    ";
}

