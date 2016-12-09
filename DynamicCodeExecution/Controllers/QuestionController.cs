using System.Collections.Generic;
using System.Web.Http;

namespace DynamicCodeExecution.Controllers
{
    public class QuestionController : ApiController
    {
        /// <summary>
        /// Returns the Question detail
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public IHttpActionResult Get(int id)
        {
            return Ok($"Question : {id}");
        }

        /// <summary>
        /// Returns a list of random question Ids
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IHttpActionResult Get()
        {
            return Ok(new List<int> { 1, 2, 3, 4, 5 });
        }
    }
}
