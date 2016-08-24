using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;

namespace TestWebApp.Controllers
{
    /// <summary>
    /// This class is used as a placeholder to call webapi
    /// </summary>
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ValuesController : Controller
    {
        /// <summary>
        /// Get some values
        /// </summary>
        /// <returns>A constant string</returns>
        [HttpGet]
        [Produces(typeof(string))]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, Type = typeof(string))]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        /// <summary>
        /// Return a single value
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A static value</returns>
        [HttpGet("{id}")]
        [Produces(typeof(string))]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, Type = typeof(string))]        
        public string Get(int id)
        {
            return "value";
        }
    }
}
