﻿using KingsStoreApi.Data.Interfaces;
using KingsStoreApi.Model.Entities;
using KingsStoreApi.Services.Interfaces;

namespace KingsStoreApi.Services.Implementations
{
    public class CartService : ICartService 
    {
        private readonly IUnitOfWork unitOfWork;
        private IRepository<Cart> _repository;

        public CartService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            _repository = unitOfWork.GetRepository<Cart>();
        }
        /*private List<CartItem> _CartContent;
        
        public CartService()
        {
            _CartContent = new List<CartItem>();
        }*/
        public void CreateCart()
        {

        }
        public void AddCartItem(Product product, int quantity)
        {
            /*var cartItem = _repository.GetSingleByCondition(p => p.Product.Id == product.Id).FirstOrDefault();

            if (cartItem is null)
            {
                var newCartItem = new CartItem { Product = product, Quantity = quantity };
                _CartContent.Add(newCartItem);
            }
            else
            {
                cartItem.Quantity++;
            }*/
        }

        public void RemoveCartItem(string cartItemId)
        {
           /* var cartItem = _CartContent.Where(c => c.CartId == cartItemId).FirstOrDefault();

            if (cartItem is not null)
                _CartContent.Remove(cartItem);*/
        }

        public void ClearCart()
        {
            /*_CartContent.Clear();*/
        }
        public decimal GetTotalCartPrice()
        {
           decimal totalPrice = 0;

            /* foreach (var item in _CartContent)
            {
                item.Product.Price += totalPrice;
            }*/

            return totalPrice;
        }       
        
    }
}