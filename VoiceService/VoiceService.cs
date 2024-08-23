using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Serilog;
using Azure.AI.TextAnalytics;
using Azure;
namespace SpeechTodoApp
{

    public class VoiceService
    {
        static string aiSpeechKey { get; set; }
        static string aiSpeechRegion { get; set; }
        static string aiSpeechLanguage { get; set; }
        static string aiTextAnalysisKey { get; set; }
        static string aiTextAnalysisRegion { get; set; }
        static string aiTextAnalysisLanguage { get; set; }
        static string textAnalysisEndpoint {  get; set; }

        static ILogger logService;
        public VoiceService()
        {
            logService = new LoggerConfiguration()
             .WriteTo.Console()
             .CreateLogger();
            
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            aiSpeechKey = config["Azure:AISpeechKey"];
            aiSpeechRegion = config["Azure:AISpeechRegion"];
            aiSpeechLanguage = "en-US";

            aiTextAnalysisKey = config["Azure:AITextKey"];
            aiTextAnalysisRegion = config["Azure:AITextRegion"];
            aiTextAnalysisLanguage = "en-US";

            textAnalysisEndpoint = "https://languageservicetext.cognitiveservices.azure.com/";

        }

        public async Task<SpeechRecognitionResult> GetSpeechResult()
        {
            var speechConfig = SpeechConfig.FromSubscription(aiSpeechKey,aiSpeechRegion);
            speechConfig.SpeechRecognitionLanguage = aiSpeechLanguage;

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

            logService.Information("Starting voice recognition.");
            var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();

            if (speechRecognitionResult.Reason == ResultReason.RecognizedSpeech)
            {
                logService.Information(speechRecognitionResult.Text);
                return speechRecognitionResult;
             }
            else
            {
                logService.Information("Speech not recognized");
            }
            throw new Exception("Error recognizing speech");
  
        }

        public async Task<string> GetTextAnalysis(string text)
        {
            AzureKeyCredential credential = new AzureKeyCredential(aiTextAnalysisKey);
            Uri endpoint = new Uri(textAnalysisEndpoint);

            var client = new TextAnalyticsClient(endpoint, credential);
            
            var response =  client.RecognizeEntities(text);

            bool validSpeech = false;

            foreach (var entity in response.Value)
            {
                if(entity.Category == "DateTime")
                {
                    //returns the text, in this case will be "March 26."
                    return entity.Text.ToString();
                }
            }
            throw new Exception("Error recognizing entities");
        }


    }
}
