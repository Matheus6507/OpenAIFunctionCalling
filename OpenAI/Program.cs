using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace OpenAI
{
    internal class Program
    {
        private static readonly HttpClient client = new HttpClient();
        public static string API_KEY = "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";
        public static string _baseAddress = "https://api.openai.com/v1/";

        static void Main(string[] args)
        {
            OpenAiFunctionCallingRun();
        }

        static void OpenAiFunctionCallingRun()
        {
            string endpoint = "chat/completions";
            string student1Description = "David Nguyen is a sophomore majoring in computer science at Stanford University. He is Asian American and has a 3.8 GPA. David is known for his programming skills and is an active member of the university's Robotics Club. He hopes to pursue a career in artificial intelligence after graduating.";
            string student2Description = "Ravi Patel is a sophomore majoring in computer science at the University of Michigan. He is South Asian Indian American and has a 3.7 GPA. Ravi is an active member of the university's Chess Club and the South Asian Student Association. He hopes to pursue a career in software engineering after graduating.";

            string[] studentsDescription = new string[] { student1Description, student2Description };

            var studentCustomFunctionsObj = new OpenAiFunctionCalling
            {
                Name = "ExtractStudentInformation",
                Description = "Get the student information from the body of the input text",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new Properties
                    {
                        Name = new PropertyDetail
                        {
                            Type = "string",
                            Description = "Name of the person"
                        },
                        Major = new PropertyDetail
                        {
                            Type = "string",
                            Description = "Major subject."
                        },
                        School = new PropertyDetail
                        {
                            Type = "string",
                            Description = "The university name."
                        },
                        Grades = new PropertyDetail
                        {
                            Type = "integer",
                            Description = "GPA of the student."
                        },
                        Clubs = new PropertyDetail
                        {
                            Type = "string",
                            Description = "School club for extracurricular activities."
                        },
                    }
                }
            };

            var studentCustomFunctions = JsonConvert.SerializeObject(studentCustomFunctionsObj);

            client.BaseAddress = new Uri(_baseAddress);

            foreach (var item in studentsDescription)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);

                var chatCompletionReq = new ChatSession
                {
                    Model = "gpt-3.5-turbo",
                    Messages = new List<Message> {
                            new Message {
                                Role = "user",
                                Content = item
                            }
                        },
                    Functions = new List<OpenAiFunctionCalling> { studentCustomFunctionsObj },
                    FunctionCall = "auto"
                };

                var content = new StringContent(JsonConvert.SerializeObject(chatCompletionReq), Encoding.UTF8, "application/json");

                var response = client.PostAsync(endpoint, content).GetAwaiter().GetResult();
                var responseString = JObject.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

                string functionName = responseString.SelectToken("choices")[0].SelectToken("message").SelectToken("function_call").SelectToken("name").ToString();
                string arguments = responseString.SelectToken("choices")[0].SelectToken("message").SelectToken("function_call").SelectToken("arguments").ToString();
                Console.WriteLine($"Chamando função: {functionName}");
                Console.WriteLine("");

                // Obtenha o tipo e o método
                Type thisType = typeof(Program);
                MethodInfo method = thisType.GetMethod(functionName);

                // Deserializar os parâmetros
                JObject parametrosJObject = JsonConvert.DeserializeObject<JObject>(arguments);
                ParameterInfo[] paramsInfo = method.GetParameters();
                object[] parametros = new object[paramsInfo.Length];

                for (int i = 0; i < paramsInfo.Length; i++)
                {
                    parametros[i] = parametrosJObject[paramsInfo[i].Name].ToObject(paramsInfo[i].ParameterType);
                }

                // Invocar o método
                object result = method.Invoke(null, parametros); // Use null para métodos estáticos, ou uma instância da classe para métodos não estáticos
            }
        }

        
        public static void ExtractStudentInformation(string name, string major, string school, int grades, string clubs)
        {
            Console.WriteLine($"{name} is majoring in {major} at {school}. He has {grades} GPA and he is an active member of the university's {string.Join(",", clubs)}.");
        }
    }

    public class ChatSession
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("messages")]
        public List<Message> Messages { get; set; }

        [JsonProperty("functions")]
        public List<OpenAiFunctionCalling> Functions { get; set; }

        [JsonProperty("function_call")]
        public string FunctionCall { get; set; }
    }

    public class Message
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class OpenAiFunctionCalling
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("parameters")]
        public Parameters Parameters { get; set; }
    }

    public class Parameters
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("properties")]
        public Properties Properties { get; set; }
    }

    public class Properties
    {
        [JsonProperty("name")]
        public PropertyDetail Name { get; set; }

        [JsonProperty("major")]
        public PropertyDetail Major { get; set; }

        [JsonProperty("school")]
        public PropertyDetail School { get; set; }

        [JsonProperty("grades")]
        public PropertyDetail Grades { get; set; }

        [JsonProperty("clubs")]
        public PropertyDetail Clubs { get; set; }
    }

    public class PropertyDetail
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}