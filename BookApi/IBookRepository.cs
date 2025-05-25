public interface IBookRepository
{
    Task<PagedResult<BookDto>> GetBooksAsync(BookSearchRequest request);
}
