using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentSwitch.Data.Abstraction;
using PaymentSwitch.Models.DTO;
using PaymentSwitch.Services.Abstraction;
using PaymentSwitch.Services.Implementation;

namespace PaymentSwitch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransfersController : ControllerBase
    {
        private readonly ITransferService _service;
        private readonly ITransferRepository _repo;
        public TransfersController(ITransferService service, ITransferRepository repo)
        {
            _service = service;
            _repo = repo;                
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TransferRequestDto dto)
        {
            // validate dto, auth, KYC checks, balance check etc.
            var txRef = await _service.InitiateTransferAsync(dto.FromAccount, dto.ToAccount, dto.ToBankCode, dto.Amount);
            return StatusCode(200, txRef);
        }

        [HttpGet("{ref}")]
        public async Task<IActionResult> GetStatus(string @ref)
        {
            var tx = await  _repo.GetByRefAsync(@ref);
            if (tx == null) return NotFound();
            return Ok(new { tx.TransactionRef, Status = tx.Status.ToString(), tx.ErrorMessage });
        }

    }
}
