using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookRepository _repo;

    public BooksController(IBookRepository repo)
    {
        _repo = repo;
    }

   
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] BookSearchRequest request)
    {
        var result = await _repo.GetBooksAsync(request);
        return Ok(result);
    }


}
