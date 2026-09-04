using MapsterMapper;
using HotelPOS.Application.DTOs.Reservation;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Future-dated table bookings/scheduling, distinct from live Billing table status —
    /// requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class ReservationsController : BaseApiController
    {
        private readonly IReservationService _reservationService;
        private readonly IMapper _mapper;

        public ReservationsController(IReservationService reservationService, IMapper mapper)
        {
            _reservationService = reservationService;
            _mapper = mapper;
        }

        /// <summary>Optionally filter to a single date via <c>?date=yyyy-MM-dd</c>.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservations([FromQuery] DateTime? date)
        {
            var reservations = await _reservationService.GetReservationsAsync(date);
            return Ok(_mapper.Map<IEnumerable<ReservationDto>>(reservations));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReservationDto>> GetReservation(int id)
        {
            if (id <= 0) return BadRequest("Invalid reservation ID.");

            var reservation = await _reservationService.GetReservationByIdAsync(id);
            if (reservation == null) return NotFound();

            return Ok(_mapper.Map<ReservationDto>(reservation));
        }

        [HttpPost]
        public async Task<ActionResult<ReservationDto>> CreateReservation([FromBody] SaveReservationDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var reservation = BuildReservation(0, request);

            try
            {
                await _reservationService.SaveReservationAsync(reservation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, _mapper.Map<ReservationDto>(reservation));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateReservation(int id, [FromBody] SaveReservationDto request)
        {
            if (id <= 0) return BadRequest("Invalid reservation ID.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var reservation = BuildReservation(id, request);

            try
            {
                await _reservationService.UpdateReservationAsync(reservation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            if (id <= 0) return BadRequest("Invalid reservation ID.");

            await _reservationService.DeleteReservationAsync(id);
            return NoContent();
        }

        /// <summary>Transitions a reservation's status (e.g. Reserved -&gt; CheckedIn), enforcing the
        /// workflow's allowed-transitions list server-side.</summary>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeReservationStatusDto request)
        {
            if (id <= 0) return BadRequest("Invalid reservation ID.");
            if (string.IsNullOrWhiteSpace(request.Status)) return BadRequest("Status is required.");

            try
            {
                await _reservationService.ChangeStatusAsync(id, request.Status);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return NoContent();
        }

        private static Reservation BuildReservation(int id, SaveReservationDto request) => new()
        {
            Id = id,
            TableId = request.TableId,
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            ReservationDate = request.ReservationDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            PartySize = request.PartySize,
            Notes = request.Notes
        };
    }
}
