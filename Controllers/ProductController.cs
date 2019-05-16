using Microsoft.AspNetCore.Mvc;
using NG_Core_Auth.Data;
using NG_Core_Auth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace NG_Core_Auth.Controllers
{
    [Route("api/[controller]")]
    public class ProductController:Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("[action]")]
        [Authorize(Policy = "RequiredLoggedIn")]
        public IActionResult GetProducts()
        {

            return Ok(_db.Products.ToList());
        }


        [HttpPost("[action]")]
        [Authorize(Policy = "AdministratorRole")]
        public async Task<IActionResult> AddProduct([FromBody] ProductModel formdata)
        {
            var newProduct = new ProductModel
            {
                Name = formdata.Name,
                Description = formdata.Description,
                ImageUrl = formdata.ImageUrl,
                OutOfStock = formdata.OutOfStock,
                Price = formdata.Price
            };

            await _db.Products.AddAsync(newProduct);

            await _db.SaveChangesAsync();

            return Ok(new JsonResult("The Product was Added Successfully!"));
        }
        
        [HttpPut("[action]/{id}")]
        [Authorize(Policy = "AdministratorRole")]
        public async Task<IActionResult> UpdateProduct([FromRoute] int id,[FromBody] ProductModel formdata)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var findProduct = _db.Products.FirstOrDefault(p => p.ProductId == id);

            if(findProduct==null)
            {
                return NotFound();
            }

            // If the product was found
            findProduct.Name = formdata.Name;
            findProduct.Description = formdata.Description;
            findProduct.ImageUrl = formdata.ImageUrl;
            findProduct.OutOfStock = formdata.OutOfStock;
            findProduct.Price = formdata.Price;

            _db.Entry(findProduct).State=EntityState.Modified;

            await _db.SaveChangesAsync();

            return Ok(new JsonResult("The Product with id " + id + " is updated"));

        }

        [HttpDelete("[action]/{id}")]
        [Authorize(Policy = "AdministratorRole")]
        public async Task<IActionResult> DeleteProduct([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // find the product
            var findProduct = _db.Products.FirstOrDefault(p => p.ProductId == id);

            if (findProduct == null)
            {
                return NotFound();
            }

            _db.Products.Remove(findProduct);

            await _db.SaveChangesAsync();

            return Ok(new JsonResult("The Product with id " + id + " is deleted"));

        }
        
    }
}
