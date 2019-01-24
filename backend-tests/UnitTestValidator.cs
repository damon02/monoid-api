using backend_core;
using BackendApi.Controllers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Xunit;

namespace backend_tests
{
    public class UnitTestValidator
    {
        private Validator Validator = new Validator();
        [Fact]
        public void TestEmailValidator()
        {
            // Valid email
            string emailAddress = "0904486@hr.nl";

            string uEmailAddress = "0904486";

            bool functionResult = Validator.IsValidEmail(emailAddress);
            bool functionResult2 = Validator.IsValidEmail(uEmailAddress);

            Assert.NotEqual(functionResult, functionResult2);
        }

        [Fact]
        public void TestPasswordFunction()
        {
            // Valid password
            string password = "Geheim123!";
            string uPassword = "geheim";

            List<string> functionResult = Validator.IsValidPassword(password);
            List<string> functionResult2 = Validator.IsValidPassword(uPassword);

            Assert.NotEqual(functionResult, functionResult2);
        }

        [Fact]
        public void TestUsernameFunction()
        {
            // Valid password
            string username = "goedeusername";
            string uUsername = "<script>alert()</script>";

            bool functionResult = Validator.IsValidUserName(username);
            bool functionResult2 = Validator.IsValidUserName(uUsername);

            Assert.NotEqual(functionResult, functionResult2);
        }
    }
}
