using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using DynamicCodeExecution.Enumerations;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Newtonsoft.Json;

namespace DynamicCodeExecution.Controllers
{
    public class AnswerController : ApiController
    {
        /// <summary>
        /// Compiles the program passed in via the body as plain text
        /// Executes the method from the class based on the methodname and classname
        /// Passes the parameters into the executing function
        /// </summary>
        /// <param name="questionId"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IHttpActionResult> CompileAndExecute(int questionId, string className, string methodName)
        {
            try
            {
                var sourcecode = await Request.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(sourcecode) || string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(methodName))
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Required parameter(s) are null"));
                }

                CompilerResults results;

                using (var provider = new CSharpCodeProvider())
                {
                    var compilerParams = new CompilerParameters
                    {
                        GenerateInMemory = true,
                        GenerateExecutable = false
                    };
                    compilerParams.ReferencedAssemblies.Add("System.dll");
                    compilerParams.ReferencedAssemblies.Add("mscorlib.dll");
                    compilerParams.WarningLevel = 3;
                    compilerParams.CompilerOptions = "/target:library /optimize";
                    results = provider.CompileAssemblyFromSource(compilerParams, sourcecode);
                }

                if (results.Errors.HasErrors)
                {
                    var errorSb = new StringBuilder();
                    foreach (CompilerError ce in results.Errors)
                    {
                        errorSb.AppendLine($"{ce.ErrorNumber} : {ce.ErrorText} at ({ce.Line},{ce.Column})");
                    }

                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.ExpectationFailed, errorSb.ToString()));
                }

                var result = results.CompiledAssembly.CreateInstance(className)
                                                    ?.GetType().GetMethod(methodName)
                                                    ?.Invoke(results.CompiledAssembly.CreateInstance(className), GetParams(GetParamArrayByQuestionId(questionId), GetParamTypeByQuestionId(questionId)));
                return Ok(result?.ToString() ?? string.Empty);
            }
            catch (Exception exception)
            {
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exception.Message));
            }
        }

        private static KnownType GetParamTypeByQuestionId(int questionId)
        {
            switch (questionId)
            {
                case 3:
                    return KnownType.IntList;

                case 4:
                    return KnownType.StringList;

                case 5:
                    return KnownType.Double;

                default:
                    return KnownType.Invalid;
            }
        }

        private static string GetParamArrayByQuestionId(int questionId)
        {
            switch (questionId)
            {
                case 1:
                    return JsonConvert.SerializeObject(1);

                case 2:
                    return JsonConvert.SerializeObject("abc");

                case 3:
                    return JsonConvert.SerializeObject(new[] { 1, 2, 3, 4, 5 });

                case 4:
                    return JsonConvert.SerializeObject(new[] { "abc", "xyz", "pqr" });

                case 5:
                    return JsonConvert.SerializeObject(1.345);

                default:
                    return null;
            }
        }

        private static object[] GetParams(string jsonparamArray, KnownType knownType)
        {
            if (string.IsNullOrWhiteSpace(jsonparamArray))
            {
                return null;
            }
            try
            {
                object obj;

                switch (knownType)
                {
                    case KnownType.IntList:
                        obj = JsonConvert.DeserializeObject<List<int>>(jsonparamArray);
                        break;

                    case KnownType.Int:
                        obj = JsonConvert.DeserializeObject<int>(jsonparamArray);
                        break;

                    case KnownType.String:
                        obj = JsonConvert.DeserializeObject<string>(jsonparamArray);
                        break;

                    case KnownType.StringList:
                        obj = JsonConvert.DeserializeObject<List<string>>(jsonparamArray);
                        break;

                    case KnownType.Double:
                        obj = JsonConvert.DeserializeObject<double>(jsonparamArray);
                        break;

                    case KnownType.Float:
                        obj = JsonConvert.DeserializeObject<float>(jsonparamArray);
                        break;

                    default:
                        obj = JsonConvert.DeserializeObject(jsonparamArray);
                        break;
                }

                return obj as object[] ?? new[] { FixConversionIssues(obj) };
            }
            catch
            {
                return null;
            }
        }

        private static object FixConversionIssues(object o)
        {
            var obj = o as long?;

            if (obj.HasValue && obj < int.MaxValue)
            {
                return (int)obj.Value;
            }

            var s = o as string;
            if (s != null)
            {
                return s;
            }

            var l = o as List<int>;
            if (l != null)
            {
                return l;
            }

            return obj;
        }
    }
}
