﻿using KingsStoreApi.Extensions;
using KingsStoreApi.Model.DataTransferObjects.TransactionServiceDTO;
using KingsStoreApi.Model.Entities;
using KingsStoreApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KingsStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBaseExtension
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService, UserManager<User> userManager): base(userManager)
        {
            this._transactionService = transactionService;
        }
        public async Task<IActionResult> PayForProduct(PayForProductDTO model)
        {
           var user = await GetLoggedInUserAsync();
            var result = _transactionService.PayForProduct(model.Amount, model.OrderId, user);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }

        public IActionResult ConfirmOrder(ConfirmTransactionDTO confirmTransactionModel) 
        {
            var user = await GetLoggedInUserAsync();
            var result = _transactionService.ConfirmOrder(confirmTransactionModel, user);
        }
    }
}
