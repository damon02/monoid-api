using backend_core;
using BackendApi.Controllers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Xunit;

namespace backend_tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            IApiController controller = new IApiController();

            JObject expectedResult = new JObject();
            expectedResult.Add("success", true);
            expectedResult.Add("message", "Lorum ipsum message..");
            expectedResult.Add("data", JsonConvert.SerializeObject(new { test = "hallo" }));

            // ~ Act
            var response = controller.CreateResponse("Lorum ipsum message..", JsonConvert.SerializeObject(new { test = "hallo" }), true);
            JObject result = JObject.Parse(Convert.ToString(response.Value)); 

            // ~ Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void TestEmailResolver()
        {
            Mailer mailer = new Mailer();
            mailer.SendEmail("Dit is de test", "Monoid: Subject", "Title", new string[] { "reinier@mail.nl" });
        }
    }
}
