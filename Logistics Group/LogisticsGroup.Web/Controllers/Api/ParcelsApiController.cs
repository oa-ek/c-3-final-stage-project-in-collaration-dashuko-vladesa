using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsGroup.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParcelsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ParcelsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Отримати всі посилки
        // GET: api/ParcelsApi
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var parcels = await _context.Parcels
                .AsNoTracking() // Оптимізація для GET-запитів
                .ToListAsync();

            return Ok(parcels);
        }

        // 2. Отримати посилку за ID
        // GET: api/ParcelsApi/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var parcel = await _context.Parcels.FindAsync(id);

            if (parcel == null)
            {
                return NotFound(new { message = $"Посилку з ID {id} не знайдено." });
            }

            return Ok(parcel);
        }

        // 3. Створити нову посилку
        // POST: api/ParcelsApi
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] Parcel parcel)
        {
            if (parcel == null)
            {
                return BadRequest("Дані посилки порожні.");
            }

            _context.Parcels.Add(parcel);
            await _context.SaveChangesAsync();

            // Повертаємо 201 Created та посилання на створений ресурс
            return CreatedAtAction(nameof(GetById), new { id = parcel.Id }, parcel);
        }

        // 4. Оновити існуючу посилку
        // PUT: api/ParcelsApi/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] Parcel parcel)
        {
            if (id != parcel.Id)
            {
                return BadRequest("ID в URL не співпадає з ID в тілі запиту.");
            }

            _context.Entry(parcel).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ParcelExists(id))
                {
                    return NotFound(new { message = $"Посилку з ID {id} не знайдено." });
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // 204 NoContent - успішно оновлено, але повертати назад дані не треба
        }

        // 5. Видалити посилку
        // DELETE: api/ParcelsApi/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var parcel = await _context.Parcels.FindAsync(id);
            if (parcel == null)
            {
                return NotFound(new { message = $"Посилку з ID {id} не знайдено." });
            }

            _context.Parcels.Remove(parcel);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 NoContent - успішно видалено
        }

        // Допоміжний метод для перевірки існування
        private bool ParcelExists(int id)
        {
            return _context.Parcels.Any(e => e.Id == id);
        }
    }
}