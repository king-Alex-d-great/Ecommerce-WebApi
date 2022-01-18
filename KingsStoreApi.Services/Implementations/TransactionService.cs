﻿using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers;
using AuthorizeNet.Api.Controllers.Bases;
using KingsStoreApi.Helpers.Implementations;
using KingsStoreApi.Model.DataTransferObjects.TransactionServiceDTO;
using KingsStoreApi.Model.Entities;
using KingsStoreApi.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KingsStoreApi.Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        public IConfiguration Configuration { get; set; }

        public TransactionService(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string PayForProduct(decimal amount, Order order, User user)
        {
            ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = AuthorizeNet.Environment.SANDBOX;

            //connection to the API Name and Key for the sandbox
            //define merchant information
            ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication =
                new merchantAuthenticationType()
                {
                    name = Configuration["AuthorizeNetName"],
                    ItemElementName = ItemChoiceType.transactionKey,
                    Item = Configuration["AuthorizeNetItem"],
                };

            var creditCard = CreateCreditCard();
            var billingAddress = CreateBillingAddress(user, order);
            var paymentType = CreatePaymentType(creditCard);
            var lineItems = CreateLineItem(order);
            var transactionRequest = CreateTransactionRequestType(amount, paymentType, billingAddress, lineItems);

            var request = new createTransactionRequest { transactionRequest = transactionRequest };

            var controller = new createTransactionController(request);
            controller.Execute();

            var response = controller.GetApiResponse();

            var result = ValidateResponse(response);
            //change to a multilevel string check!
            return result;
        }

        public ReturnModel ConfirmOrder (ConfirmTransactionDTO confirmTransactionModel, User user)
        {
            //I should be able to get a cart from a user
            Cart cart = new Cart();

            if (cart.CartItems.Count == 0)
                return RedirectToAction(nameof(Index));

            confirmTransactionModel.Basket = cart;

            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Shipping));

            // add address to database
            await _orderRepo.CreateAddress(confirmTransactionModel.Address);

            // create an new order object and load the order items onto it
            Order datOrder = new Order
            {
                UserID = user.Id,
                AddressID = confirmTransactionModel.Address.ID,
                Address = confirmTransactionModel.Address,
                OrderDate = DateTime.Now.ToString("MMM d, yyyy (ddd) @ HH:mm tt"),
                Shipping = confirmTransactionModel.Shipping,
                DiscountName = confirmTransactionModel.DiscountName,
                DiscountPercent = confirmTransactionModel.DiscountPercent,
                DiscountAmt = confirmTransactionModel.DiscountAmt,
                TotalItemQty = confirmTransactionModel.TotalItemQty,
                Subtotal = confirmTransactionModel.Subtotal,
                Total = confirmTransactionModel.Total,
            };

            // add order to the database table
            // I'm doing this first in hopes that the order generates an ID that
            // I can add to the order items. Here's hoping...
            await _orderRepo.AddOrderAsync(datOrder);

            List<OrderItem> demOrderItems = new List<OrderItem>();
            foreach (var item in cart.BasketItems)
            {
                OrderItem tempOrderItem = new OrderItem
                {
                    ProductID = item.ProductID,
                    OrderID = datOrder.ID,
                    UserID = user.Id,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    ImgUrl = item.ImgUrl,
                    UnitPrice = item.UnitPrice
                };

                // add order item to the database table
                await _orderRepo.AddOrderItemToOrderAsync(tempOrderItem);
                demOrderItems.Add(tempOrderItem);
            }

            // attach orderitems to order
            datOrder.OrderItems = demOrderItems;

            //sends a receipt of the order information
            string htmlMessage = "Thank you for shopping with us!  You ordered: </br>";
            foreach (var item in datOrder.OrderItems)
            {
                htmlMessage += $"Item: {item.ProductName}, Quantity: {item.Quantity}</br>";
            };

            //CHARGE CARD
            Payment payment = new Payment(Configuration);
            payment.RunPayment(confirmTransactionModel.Total, datOrder, user);

            await _emailSender.SendEmailAsync(user.Email, "Order Information",
                        htmlMessage);
            // empty out basket
            await _basketRepo.ClearOutBasket(confirmTransactionModel.Basket.BasketItems);

            return "done";
        }

        private customerAddressType CreateBillingAddress(User user, Order order)
        {
            return new customerAddressType
            {
                firstName = user.FullName,
                email = user.Email,
                country = "Nigeria",
                address = order.Shipping,
                city = "Enugu",
                zip = "100403"
            };
        }

        private lineItemType[] CreateLineItem(Order order)
        {
            var lineItems = new lineItemType[order.OrderItems.Count];
            int count = 0;
            foreach (var item in order.OrderItems)
            {
                //line items to process
                lineItems[count] = new lineItemType
                {
                    itemId = (item.ProductID).ToString(),
                    name = item.ProductName,
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice
                };
                count++;
            }

            return lineItems;
        }

        private creditCardType CreditCardType()
        {
            return new creditCardType
            {
                cardNumber = "4111111111111111",
                expirationDate = "0728",
                cardCode = "123"
            };
        }

        private string ValidateResponse(createTransactionResponse response)
        {
            if (response is null)
                return "invvlid";

            if (response.messages.resultCode != messageTypeEnum.Ok)
                return $"Transaction failed\n{response.transactionResponse.errors[0].errorText ?? response.messages.message[0].code}";

            if (response.transactionResponse.messages is null)
                return "TransactionFailed Error Text: " + response.transactionResponse.errors[0].errorText;
            // We should be getting an OK response type.
            return $"Successfully created transaction with Transaction ID: {response.transactionResponse.transId}\n Response Code: {response.transactionResponse.responseCode}";
        }

        private transactionRequestType CreateTransactionRequestType(decimal amount, paymentType paymentType, customerAddressType billingAddress, lineItemType[] lineItems)
        {
            return new transactionRequestType
            {
                transactionType = transactionTypeEnum.authCaptureTransaction.ToString(),    // charge the card

                amount = amount,
                payment = paymentType,
                billTo = billingAddress,
                lineItems = lineItems
            };
        }

        private creditCardType CreateCreditCard()
        {
            return new creditCardType
            {
                cardNumber = "4111111111111111",
                expirationDate = "0728",
                cardCode = "123"
            };
        }

        private paymentType CreatePaymentType(creditCardType creditCard)
        {
            return new paymentType { Item = creditCard };
        }
    }
}
