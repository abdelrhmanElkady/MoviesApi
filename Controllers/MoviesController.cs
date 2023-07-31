using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MoviesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {

        private readonly ApplicationDbContext _context;

        private readonly List<string> _allowedExtensions = new() { ".jpg", ".png" };
        private readonly long _maxAllowedPosterSize = 6291456;


        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var movies = await _context.Movies.Include(m => m.Genre).OrderByDescending(m => m.Rate).ToListAsync();
            return Ok(movies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id )
        {
            var movie = await _context.Movies.Include(m => m.Genre).SingleOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return Ok(movie);   
        }

        [HttpGet("GetByGenreId")]
        public async Task<IActionResult> GetByGenreIdAsync(byte id)
        {
            var movies = await _context.Movies.Include(m => m.Genre).OrderByDescending(m => m.Rate).Where(m => m.GenreId == id).ToListAsync();  
            return Ok(movies);
        }


        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm]MovieDto dto)
        {

            if (dto.Poster == null)
                return BadRequest("Poster is required!");

            if (!_allowedExtensions.Contains(Path.GetExtension(dto.Poster.FileName.ToLower())))
            {
                return BadRequest("Only .jpg or .png images are allowed!");
            }
            if(dto.Poster.Length > _maxAllowedPosterSize)
            {
                return BadRequest("Max allowed size for poster is 6MB");
            }

            var isValidGenre = await _context.Genres.AnyAsync(g => g.Id == dto.GenreId);
            if (!isValidGenre)
            {
                return BadRequest("Invalid Genre ID!");

            }
            using var dataStream = new MemoryStream();
            await dto.Poster.CopyToAsync(dataStream);

            var movie = new Movie
            {
                GenreId = dto.GenreId,
                Title = dto.Title,
                Poster = dataStream.ToArray(),
                Rate = dto.Rate,
                StoryLine = dto.StoryLine,
                Year = dto.Year
            };

           await _context.Movies.AddAsync(movie);
           _context.SaveChanges();
            return Ok(movie);   
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromForm] MovieDto dto)
        {
            var movie = await _context.Movies.FindAsync(id);

            if (movie == null)
                return NotFound($"No movie was found with ID {id}");

            var isValidGenre = await _context.Genres.AnyAsync(g => g.Id == dto.GenreId);
            if (!isValidGenre)
            {
                return BadRequest("Invalid Genre ID!");

            }

            if (dto.Poster != null)
            {
                if (!_allowedExtensions.Contains(Path.GetExtension(dto.Poster.FileName).ToLower()))
                    return BadRequest("Only .png and .jpg images are allowed!");

                if (dto.Poster.Length > _maxAllowedPosterSize)
                    return BadRequest("Max allowed size for poster is 1MB!");

                using var dataStream = new MemoryStream();

                await dto.Poster.CopyToAsync(dataStream);

                movie.Poster = dataStream.ToArray();
            }

            movie.Title = dto.Title;
            movie.GenreId = dto.GenreId;
            movie.Year = dto.Year;
            movie.StoryLine = dto.StoryLine;
            movie.Rate = dto.Rate;

            _context.Movies.Update(movie);
            _context.SaveChanges();

            return Ok(movie);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DelteAsync(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                return NotFound($"NO movie was found with ID:{id}");
            }
             _context.Movies.Remove(movie);
            _context.SaveChanges();
            return Ok(movie);
        }

    }
}
